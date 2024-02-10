namespace GameServer.ReverseProxy
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using PlayFab;
    using PlayFab.AuthenticationModels;
    using PlayFab.MultiplayerModels;

    public class ServerEndpointFactory
    {
        private readonly ILogger _logger;
        private readonly PlayFabAuthenticationInstanceAPI _authenticationApi;

        private (long expiresAt, PlayFabMultiplayerInstanceAPI multiplayerApi) _cachedMultiplayerApi;

        public ServerEndpointFactory(ILoggerFactory loggerFactory, PlayFabAuthenticationInstanceAPI authenticationApi)
        {
            _logger = loggerFactory.CreateLogger<ServerEndpointFactory>();
            _authenticationApi = authenticationApi;
        }

        public async Task<PlayFabMultiplayerInstanceAPI> GetMultiplayerAPI()
        {
            if(_cachedMultiplayerApi.multiplayerApi == null ||
                DateTime.UtcNow.ToFileTimeUtc() >= _cachedMultiplayerApi.expiresAt)
            {
                _logger.LogInformation($"Requesting new instance of {nameof(PlayFabMultiplayerInstanceAPI)} - CACHE EXPIRED");
                var entityToken = await _authenticationApi.GetEntityTokenAsync(new GetEntityTokenRequest());
                if(entityToken.Error != null)
                {
                    _logger.LogError(entityToken.Error.ErrorMessage);
                    return null;
                }
                _logger.LogInformation($"Token expires at {entityToken.Result.TokenExpiration}");
                PlayFabMultiplayerInstanceAPI api = new(_authenticationApi.apiSettings,
                        new PlayFabAuthenticationContext()
                        {
                            EntityToken = entityToken.Result.EntityToken
                        });
                _cachedMultiplayerApi = new(entityToken.Result.TokenExpiration.Value.ToFileTimeUtc(), api);
                _logger.LogInformation($"New instance of {nameof(PlayFabMultiplayerAPI)} set, expires at {entityToken.Result.TokenExpiration.Value}");
            }
            return _cachedMultiplayerApi.multiplayerApi;
        }

        public async Task<string> GetServerEndpoint(Guid sessionId)
        {
            PlayFabMultiplayerInstanceAPI api = await GetMultiplayerAPI();
            if (api == null)
                return string.Empty;
            var response = await api.GetMultiplayerServerDetailsAsync(new GetMultiplayerServerDetailsRequest
            {
                SessionId = sessionId.ToString(),
            });

            if (response.Error?.Error == PlayFabErrorCode.MultiplayerServerNotFound)
            {
                _logger.LogError("Server not found: Session ID = {SessionId}", sessionId);

                return null;
            }

            _logger.LogInformation($"FQDN: {response.Result.FQDN}");

            if (response.Error != null)
            {
                _logger.LogError("{Request} failed: {Message}", nameof(api.GetMultiplayerServerDetailsAsync),
                    response.Error.GenerateErrorReport());

                throw new Exception(response.Error.GenerateErrorReport());
            }

            var endpoint = string.Concat("ws://", response.Result.IPV4Address);
            _logger.LogInformation($"end point is ---- {endpoint}");
            var uriBuilder = new UriBuilder(endpoint)
            {
                Port = GetEndpointPortNumber(response.Result.Ports)
            };

            return uriBuilder.ToString();
        }

        private static int GetEndpointPortNumber(IEnumerable<Port> ports)
        {
            // replace this logic with whatever is configured for your build i.e. getting a port by name
            return ports.First().Num;
        }
    }
}
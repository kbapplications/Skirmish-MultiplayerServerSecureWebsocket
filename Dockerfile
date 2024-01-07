#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GameServer.ReverseProxy.csproj", "."]
RUN dotnet restore "./GameServer.ReverseProxy.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "GameServer.ReverseProxy.csproj" -c Release -o /app/build

RUN openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout /app/server.key -out /app/server.crt -subj "/CN=localhost"

FROM build AS publish
RUN dotnet publish "GameServer.ReverseProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameServer.ReverseProxy.dll", "--urls", "https://*:8443"]

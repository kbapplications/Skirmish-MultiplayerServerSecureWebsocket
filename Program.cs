using GameServer.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel(serverOptions =>
        {
            string certPath = "/app/server.crt";
            string keyPath = "/app/server.key";

            // Configure Kestrel to use the specified certificate and private key
            serverOptions.ListenAnyIP(8080);  // Adjust the port as needed
            serverOptions.ListenAnyIP(8443, listenOptions =>
            {
                listenOptions.UseHttps(certPath, keyPath);
            });
        });
        webBuilder.UseStartup<Startup>();
    }).Build().Run();
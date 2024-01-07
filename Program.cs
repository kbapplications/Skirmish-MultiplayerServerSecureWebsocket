using GameServer.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel(serverOptions =>
        {
            // Configure Kestrel to use the specified certificate and private key
            serverOptions.ListenAnyIP(8080);  // Adjust the port as needed
            serverOptions.ListenAnyIP(8443, listenOptions =>
            {
                string privateKeyPath = "/app/server.crt";
                string certificatePath = "/app/server.key";

                // Load private key
                RSA privateKey = LoadPrivateKey(privateKeyPath);

                // Load certificate
                X509Certificate2 certificate = new(certificatePath);

                // Create a new X509Certificate2 object combining the private key and certificate
                X509Certificate2 certificateWithPrivateKey = CreateCertificateWithPrivateKey(certificate, privateKey);
                listenOptions.UseHttps(certificate);
            });
        });
        webBuilder.UseStartup<Startup>();
    }).Build().Run();

static RSA LoadPrivateKey(string privateKeyPath)
{
    // Load the private key from a file (this is just a simple example, you might need to adjust based on your key format)
    string privateKeyContent = File.ReadAllText(privateKeyPath);
    RSA privateKey = RSA.Create();
    privateKey.ImportFromPem(privateKeyContent);
    return privateKey;
}

static X509Certificate2 CreateCertificateWithPrivateKey(X509Certificate2 certificate, RSA privateKey)
{
    // Create a certificate with private key
    X509Certificate2 certificateWithPrivateKey = certificate.CopyWithPrivateKey(privateKey);
    return certificateWithPrivateKey;
}
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Services;

public static class CloudHosting
{
    public static string DatabaseConnection(IConfiguration config)
    {
        var connection = new MySqlConnector.MySqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure the database connection."));
        connection.AllowUserVariables = true; // Reviewed compatibility migrations use server-side conditional DDL.
        return connection.ConnectionString;
    }
    public static ServerVersion DatabaseVersion(IConfiguration config)
    {
        var version = Version.Parse(config["Database:ServerVersion"] ?? "10.4.32");
        return (config["Database:Provider"] ?? "MariaDb").ToLowerInvariant() switch {
            "mariadb" => new MariaDbServerVersion(version),
            "mysql" => new MySqlServerVersion(version),
            _ => throw new InvalidOperationException("Database:Provider must be MariaDb or MySql.")
        };
    }

    public static void Configure(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.Configure<ForwardedHeadersOptions>(o => {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardLimit = 1;
            // Explicit CIDRs only: do not trust arbitrary client-supplied forwarding headers.
            foreach (var cidr in config.GetSection("Hosting:TrustedProxyNetworks").Get<string[]>() ?? [])
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
        });
        if (config.GetValue<bool>("DataProtection:PersistToDatabase")) {
            var certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(config["DataProtection:CertificateBase64"] ?? throw new InvalidOperationException("Configure the Data Protection certificate.")), config["DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
            if (!certificate.HasPrivateKey) throw new InvalidOperationException("Data Protection requires the certificate's private key.");
            builder.Services.AddDataProtection().SetApplicationName("DigitalKrushiOfficer")
                .ProtectKeysWithCertificate(certificate);
            builder.Services.AddSingleton<DatabaseKeyRepository>();
            builder.Services.AddOptions<KeyManagementOptions>().Configure<DatabaseKeyRepository>((options, repository) => options.XmlRepository = repository);
        }
    }
}

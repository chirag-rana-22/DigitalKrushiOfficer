using Digital_Krushi_Officer.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
namespace Digital_Krushi_Officer.Services;
public static class ProductionReadiness
{
    public static async Task ValidateAsync(IConfiguration config, AppDbContext db)
    {
        var connection = new MySqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure the database connection."));
        if (connection.UserID.Equals("root", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(connection.Password))
            throw new InvalidOperationException("Production requires a dedicated database user and password.");
        if (config["AllowedHosts"] is null or "*" or "") throw new InvalidOperationException("Set AllowedHosts to the production hostname.");
        ValidateHosting(config);
        foreach (var admin in await db.Users.Where(u => u.Role == "Admin" && u.IsActive).ToListAsync())
            if (BCrypt.Net.BCrypt.Verify("Admin@123", admin.PasswordHash)) throw new InvalidOperationException("Replace the demo administrator password before production.");
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply reviewed database migrations before deployment.");
    }

    public static void ValidateHosting(IConfiguration config)
    {
        if (!string.Equals(config["Hosting:Platform"], "Render", StringComparison.OrdinalIgnoreCase)) return;
        var connection = new MySqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure the database connection."));
        if (connection.SslMode != MySqlSslMode.VerifyFull)
            throw new InvalidOperationException("Cloud database TLS must verify both certificate and hostname (SslMode=VerifyFull).");
        if (!string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(config["Storage:Supabase:Url"]) || string.IsNullOrWhiteSpace(config["Storage:Supabase:ServiceRoleKey"]))
            throw new InvalidOperationException("Render requires configured persistent photo storage.");
        if (!string.Equals(config["Email:Provider"], "Brevo", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(config["Email:Brevo:ApiKey"]) || string.IsNullOrWhiteSpace(config["Email:FromEmail"]))
            throw new InvalidOperationException("Render Free requires a configured HTTPS email provider.");
        if (!config.GetValue<bool>("DataProtection:PersistToDatabase"))
            throw new InvalidOperationException("Render requires persistent encrypted Data Protection keys.");
        if (!RenderPublicEdge.Enabled(config) && !(config.GetSection("Hosting:TrustedProxyNetworks").Get<string[]>() ?? []).Any())
            throw new InvalidOperationException("Configure the hosting proxy's verified network CIDRs.");
    }
}

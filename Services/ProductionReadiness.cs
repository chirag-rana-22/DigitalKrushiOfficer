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
        foreach (var admin in await db.Users.Where(u => u.Role == "Admin" && u.IsActive).ToListAsync())
            if (BCrypt.Net.BCrypt.Verify("Admin@123", admin.PasswordHash)) throw new InvalidOperationException("Replace the demo administrator password before production.");
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply reviewed database migrations before deployment.");
    }
}

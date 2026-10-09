using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace Digital_Krushi_Officer.Services;
public class OtpService(AppDbContext db, IDataProtectionProvider protection, IEmailService email)
{
    private readonly IDataProtector protector = protection.CreateProtector("DKO.OTP.v1");
    public async Task<bool> SendAsync(TblUser user, string purpose)
    {
        var now = DateTime.UtcNow;
        var challenge = await db.Set<OtpChallenge>().SingleOrDefaultAsync(c => c.Email == user.Email && c.Purpose == purpose);
        if (challenge != null && challenge.SentUtc > now.AddMinutes(-1)) return false;
        if (challenge == null) { challenge = new() { Email = user.Email, Purpose = purpose }; db.Add(challenge); }
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        challenge.ProtectedCode = protector.Protect(code);
        challenge.ExpiresUtc = now.AddMinutes(10); challenge.SentUtc = now; challenge.Attempts = 0; challenge.Consumed = false;
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return false; }
        var error = purpose == "verify" ? await email.SendOtpEmailAsync(user.Email, user.Name, code) : await email.SendPasswordResetEmailAsync(user.Email, user.Name, code);
        return error == null;
    }
    public async Task<bool> VerifyAsync(string address, string purpose, string? code)
    {
        var now = DateTime.UtcNow;
        var candidates = db.Set<OtpChallenge>().Where(c => c.Email == address && c.Purpose == purpose && !c.Consumed && c.ExpiresUtc > now && c.Attempts < 5);
        if (await candidates.ExecuteUpdateAsync(s => s.SetProperty(c => c.Attempts, c => c.Attempts + 1)) != 1) return false;
        var challenge = await db.Set<OtpChallenge>().AsNoTracking().SingleAsync(c => c.Email == address && c.Purpose == purpose);
        string expected;
        try { expected = protector.Unprotect(challenge.ProtectedCode); } catch (CryptographicException) { return false; }
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(code?.Trim() ?? ""))) return false;
        return await db.Set<OtpChallenge>().Where(c => c.Id == challenge.Id && !c.Consumed && c.ProtectedCode == challenge.ProtectedCode)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Consumed, true)) == 1;
    }
}

using System.Net;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Services;

public class CropReminderDelivery(AppDbContext db, IEmailService email, IConfiguration config)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public async Task<(int Sent, int Failed)> SendDueAsync()
    {
        await Gate.WaitAsync();
        try {
            var today = CropPlanning.Today;
            var origin = config["Reminders:PublicUrl"];
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var publicUrl) || publicUrl.Scheme != "https" || publicUrl.Query != "" || publicUrl.UserInfo != "") throw new InvalidOperationException("Configure the HTTPS reminder website URL.");
            var alreadyAttempted = await db.CropDigests.CountAsync(d => d.LocalDate == today);
            var remaining = Math.Max(0, Math.Clamp(config.GetValue<int?>("Reminders:DailyEmailLimit") ?? 100, 1, 100) - alreadyAttempted);
            var farmers = await db.Users.Where(u => u.Role == "Farmer" && u.IsActive && u.IsEmailVerified
                && !db.CropDigests.Any(d => d.FarmerId == u.Id && d.LocalDate == today)
                && db.CropTasks.Any(t => t.FarmerCrop!.FarmerId == u.Id && t.FarmerCrop.EmailRemindersEnabled && t.FarmerCrop.Status == "Growing" && t.Status == "Pending" && t.DueDate <= today))
                .OrderBy(u => u.Id).Take(remaining).ToListAsync();
            int sent = 0, failed = 0;
            foreach (var farmer in farmers) {
                var tasks = await db.CropTasks.AsNoTracking().Include(t => t.FarmerCrop).ThenInclude(c => c!.Crop)
                    .Where(t => t.FarmerCrop!.FarmerId == farmer.Id && t.FarmerCrop.EmailRemindersEnabled && t.FarmerCrop.Status == "Growing" && t.Status == "Pending" && t.DueDate <= today)
                    .OrderBy(t => t.DueDate).ThenBy(t => t.Id).Take(50).ToListAsync();
                if (tasks.Count == 0) continue;
                // Persist the unique claim BEFORE sending. Ambiguous delivery is never retried blindly the same day.
                var digest = new CropDigest { FarmerId = farmer.Id, LocalDate = today };
                db.CropDigests.Add(digest);
                try { await db.SaveChangesAsync(); }
                catch (DbUpdateException) {
                    db.Entry(digest).State = EntityState.Detached;
                    if (await db.CropDigests.AnyAsync(d => d.FarmerId == farmer.Id && d.LocalDate == today)) continue;
                    throw;
                }
                var heading = ReminderLanguage.Heading(farmer.PreferredLanguage);
                var body = ReminderLanguage.Email(farmer, tasks, today, publicUrl);
                var error = await email.SendEmailAsync(farmer.Email, farmer.Name, heading, body);
                digest.Status = error == null ? "Accepted" : "Failed";
                digest.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                if (error == null) sent++; else failed++;
            }
            failed = await db.CropDigests.CountAsync(d => d.LocalDate == today && d.Status == "Failed");
            return (sent, failed);
        } finally { Gate.Release(); }
    }
}

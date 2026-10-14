using System.Security.Cryptography;
using System.Text;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Mvc;
namespace Digital_Krushi_Officer.Controllers;

[ApiController]
public class ReminderJobController(IConfiguration config, CropReminderDelivery delivery) : ControllerBase
{
    [HttpPost("internal/reminders/run"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> Run()
    {
        var expected = config["Reminders:SchedulerToken"];
        var provided = Request.Headers["X-Reminder-Token"].ToString();
        if (!config.GetValue<bool>("Reminders:Enabled") || expected?.Length < 32 || string.IsNullOrEmpty(expected)) return NotFound();
        if (provided.Length > 256 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(provided)))) return Unauthorized();
        var result = await delivery.SendDueAsync();
        return result.Failed > 0 ? StatusCode(503, new { sent = result.Sent, failed = result.Failed }) : Ok(new { sent = result.Sent, failed = result.Failed });
    }
}

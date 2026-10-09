using System.ComponentModel.DataAnnotations;
namespace Digital_Krushi_Officer.Models;
public class OtpChallenge
{
    public int Id { get; set; }
    [MaxLength(100)] public string Email { get; set; } = "";
    [MaxLength(20)] public string Purpose { get; set; } = "";
    public string ProtectedCode { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime SentUtc { get; set; }
    public int Attempts { get; set; }
    public bool Consumed { get; set; }
}

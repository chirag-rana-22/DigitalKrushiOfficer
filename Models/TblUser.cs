using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblUser")]
    public class TblUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? Mobile { get; set; }

        [Required, MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Admin / Farmer / Officer

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Village { get; set; }

        [MaxLength(50)]
        public string PreferredLanguage { get; set; } = "English";

        public bool IsEmailVerified { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? LastLoginDate { get; set; }

        // Navigation
        public TblFarmerDetails? FarmerDetails { get; set; }
        public TblOfficerDetails? OfficerDetails { get; set; }
        public ICollection<TblQuery> Queries { get; set; } = new List<TblQuery>();
        public ICollection<TblFarmerCrop> FarmerCrops { get; set; } = new List<TblFarmerCrop>();
        public ICollection<TblNotificationLog> Notifications { get; set; } = new List<TblNotificationLog>();
    }
}

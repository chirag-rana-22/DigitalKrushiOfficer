using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblNotificationLog")]
    public class TblNotificationLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Email / SMS / System</summary>
        [MaxLength(50)]
        public string NotificationType { get; set; } = "Email";

        [MaxLength(200)]
        public string? Subject { get; set; }

        public string? Content { get; set; }

        /// <summary>Sent / Failed / Pending</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime? SentDate { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public TblUser? User { get; set; }
    }
}

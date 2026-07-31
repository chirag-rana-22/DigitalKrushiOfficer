using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblOfficerAssignment")]
    public class TblOfficerAssignment
    {
        [Key]
        public int Id { get; set; }

        public int OfficerId { get; set; }

        [Required, MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Village { get; set; }

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("OfficerId")]
        public TblUser? Officer { get; set; }
    }
}

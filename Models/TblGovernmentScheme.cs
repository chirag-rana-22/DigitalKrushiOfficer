using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblGovernmentScheme")]
    public class TblGovernmentScheme
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string SchemeName { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Eligibility { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        public DateTime? StartDate { get; set; }
        public DateTime? LastDate { get; set; }

        [MaxLength(300)]
        public string? DocumentUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedBy { get; set; }
    }
}

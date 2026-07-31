using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblFarmerDetails")]
    public class TblFarmerDetails
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? LandArea { get; set; }

        [MaxLength(100)]
        public string? SoilType { get; set; }

        [MaxLength(100)]
        public string? MainCrop { get; set; }

        public int? FarmingExperience { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("UserId")]
        public TblUser? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblFarmerCrop")]
    public class TblFarmerCrop
    {
        [Key]
        public int Id { get; set; }

        public int FarmerId { get; set; }
        public int? CropId { get; set; }

        [MaxLength(100)]
        public string? CustomCropName { get; set; }

        [NotMapped]
        public string DisplayName => CustomCropName ?? Crop?.CropName ?? "Crop";

        [MaxLength(100)] public string? FieldName { get; set; }
        [MaxLength(100)] public string? Variety { get; set; }
        public bool EmailRemindersEnabled { get; set; }
        public DateTime? HarvestedDate { get; set; }
        [ConcurrencyCheck] public int PlanVersion { get; set; }
        public ICollection<CropTask> Tasks { get; set; } = new List<CropTask>();

        [Column(TypeName = "decimal(10,2)")]
        public decimal? LandArea { get; set; }

        public DateTime? SowingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Growing";

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("FarmerId")]
        public TblUser? Farmer { get; set; }

        [ForeignKey("CropId")]
        public TblCrop? Crop { get; set; }
    }
}

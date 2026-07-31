using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblDiseaseDataset")]
    public class TblDiseaseDataset
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? CropName { get; set; }

        [MaxLength(100)]
        public string? DiseaseName { get; set; }

        public string? Symptoms { get; set; }
        public string? Solution { get; set; }

        [MaxLength(200)]
        public string? RecommendedPesticide { get; set; }

        [MaxLength(300)]
        public string? ReferenceImagePath { get; set; }

        [MaxLength(50)]
        public string? SeverityLevel { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}

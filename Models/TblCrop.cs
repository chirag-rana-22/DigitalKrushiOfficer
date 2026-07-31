using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblCrop")]
    public class TblCrop
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string CropName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Season { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        public ICollection<TblFarmerCrop> FarmerCrops { get; set; } = new List<TblFarmerCrop>();
    }
}

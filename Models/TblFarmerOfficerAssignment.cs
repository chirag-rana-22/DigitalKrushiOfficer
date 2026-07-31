using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblFarmerOfficerAssignment")]
    public class TblFarmerOfficerAssignment
    {
        [Key]
        public int Id { get; set; }

        public int FarmerId { get; set; }

        public int OfficerId { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Village { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("FarmerId")]
        public TblUser? Farmer { get; set; }

        [ForeignKey("OfficerId")]
        public TblUser? Officer { get; set; }
    }
}

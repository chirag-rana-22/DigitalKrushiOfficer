using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblQuery")]
    public class TblQuery
    {
        [Key]
        public int Id { get; set; }

        public int FarmerId { get; set; }

        public string? QueryText { get; set; }

        [MaxLength(50)]
        public string QueryLanguage { get; set; } = "Gujarati";

        [MaxLength(50)]
        public string? QueryType { get; set; } // Text / Voice / Image

        [MaxLength(300)]
        public string? ImagePath { get; set; }

        public string? ImagePathsJson { get; set; }

        [MaxLength(300)]
        public string? VoicePath { get; set; }

        public int? ParentQueryId { get; set; }
        public int? FarmerCropId { get; set; }
        [MaxLength(3000)] public string? CropContext { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("FarmerId")]
        public TblUser? Farmer { get; set; }

        public TblAIResponse? AIResponse { get; set; }
        public TblEscalation? Escalation { get; set; }
        public ICollection<TblFeedback> Feedbacks { get; set; } = new List<TblFeedback>();
    }
}

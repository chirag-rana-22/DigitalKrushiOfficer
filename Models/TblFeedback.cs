using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblFeedback")]
    public class TblFeedback
    {
        [Key]
        public int Id { get; set; }

        public int QueryId { get; set; }
        public int? AIResponseId { get; set; }

        public int? Rating { get; set; }

        public string? FeedbackText { get; set; }

        [MaxLength(50)]
        public string? FeedbackType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("QueryId")]
        public TblQuery? Query { get; set; }

        [ForeignKey("AIResponseId")]
        public TblAIResponse? AIResponse { get; set; }
    }
}

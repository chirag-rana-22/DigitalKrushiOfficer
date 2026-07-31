using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    public enum ComplexityLevel
    {
        Simple = 0,
        Medium = 1,
        Complex = 2
    }

    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Modified = 3
    }

    [Table("TblAIResponse")]
    public class TblAIResponse
    {
        [Key]
        public int Id { get; set; }

        public int QueryId { get; set; }

        public string? AIAnswer { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ConfidenceScore { get; set; }

        [MaxLength(50)]
        public string? AIModel { get; set; }

        public int? ResponseTimeMs { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        public bool IsEscalated { get; set; } = false;

        [MaxLength(500)]
        public string? EscalationReason { get; set; }

        // ── New fields for complexity-based review workflow ──
        public ComplexityLevel ComplexityLevel { get; set; } = ComplexityLevel.Simple;

        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

        public int? ApprovedByOfficerId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(1000)]
        public string? OfficerNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("QueryId")]
        public TblQuery? Query { get; set; }

        [ForeignKey("ApprovedByOfficerId")]
        public TblUser? ApprovedByOfficer { get; set; }
    }
}

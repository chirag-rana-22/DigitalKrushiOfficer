using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblEscalation")]
    public class TblEscalation
    {
        [Key]
        public int Id { get; set; }

        public int QueryId { get; set; }
        public int OfficerId { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        public string? OfficerReply { get; set; }

        public DateTime? AssignedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public bool EscalatedByAI { get; set; } = false;
        public bool EscalatedByFarmer { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("QueryId")]
        public TblQuery? Query { get; set; }

        [ForeignKey("OfficerId")]
        public TblUser? Officer { get; set; }
    }
}

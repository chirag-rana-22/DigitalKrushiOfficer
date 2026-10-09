using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblGovernmentScheme")]
    public class TblGovernmentScheme : IValidatableObject
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

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (!string.IsNullOrWhiteSpace(DocumentUrl) && (!Uri.TryCreate(DocumentUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")))
                yield return new ValidationResult("Enter an HTTP or HTTPS document URL.", new[] { nameof(DocumentUrl) });
            if (StartDate != null && LastDate < StartDate) yield return new ValidationResult("Last date must follow the start date.", new[] { nameof(LastDate) });
            if (Status is not ("Active" or "Closed" or "Upcoming" or "Expired")) yield return new ValidationResult("Choose a valid scheme status.", new[] { nameof(Status) });
        }
    }
}

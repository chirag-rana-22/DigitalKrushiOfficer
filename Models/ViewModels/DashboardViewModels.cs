using System.ComponentModel.DataAnnotations;

namespace Digital_Krushi_Officer.Models.ViewModels
{
    public class FarmerDashboardViewModel
    {
        public TblUser? User { get; set; }
        public TblFarmerDetails? FarmerDetails { get; set; }
        public int TotalQueries { get; set; }
        public int PendingQueries { get; set; }
        public int TotalCrops { get; set; }
        public int TotalFeedback { get; set; }
        public List<TblQuery> RecentQueries { get; set; } = new();
        public List<TblFarmerCrop> ActiveCrops { get; set; } = new();
        public WeatherViewModel? Weather { get; set; }
    }

    public class OfficerDashboardViewModel
    {
        public TblUser? User { get; set; }
        public TblOfficerDetails? OfficerDetails { get; set; }
        public int PendingEscalations { get; set; }
        public int InProgressEscalations { get; set; }
        public int SolvedEscalations { get; set; }
        public int TotalEscalations { get; set; }
        public double AvgResolutionHours { get; set; }
        public List<TblEscalation> RecentEscalations { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalFarmers { get; set; }
        public int TotalOfficers { get; set; }
        public int TotalQueries { get; set; }
        public int PendingQueries { get; set; }
        public int PendingEscalations { get; set; }
        public int SolvedEscalations { get; set; }
        public int TotalFeedback { get; set; }
        public double AvgRating { get; set; }
        public int TotalCrops { get; set; }
        public int TotalDiseases { get; set; }
        public int ActiveSchemes { get; set; }
        public List<TblUser> RecentFarmers { get; set; } = new();
        public List<TblQuery> RecentQueries { get; set; } = new();
    }

    public class WeatherViewModel
    {
        public string District { get; set; } = string.Empty;
        public string Village { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public decimal RainChance { get; set; }
        public decimal WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public string FarmingAlert { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public DateTime FetchedAt { get; set; } = DateTime.Now;
    }

    public class ChatViewModel
    {
        public List<ChatMessageViewModel> Messages { get; set; } = new();
        public string? CurrentQueryText { get; set; }
    }

    public class ChatMessageViewModel
    {
        public int QueryId { get; set; }
        public string? QueryText { get; set; }
        public string? AIAnswer { get; set; }
        public string QueryType { get; set; } = "Text";
        public string? ImagePath { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedDate { get; set; }
        public bool IsEscalated { get; set; }
        public string? OfficerReply { get; set; }
        public int? FeedbackRating { get; set; }
        public string ComplexityLevel { get; set; } = "Simple";
        public string ApprovalStatus { get; set; } = "N/A";
    }

    public class ProfileViewModel
    {
        public TblUser? User { get; set; }
        public TblFarmerDetails? FarmerDetails { get; set; }
        public TblOfficerDetails? OfficerDetails { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(15)]
        [Display(Name = "Mobile")]
        public string? Mobile { get; set; }

        [Display(Name = "State")]
        public string? State { get; set; }

        [Display(Name = "District")]
        public string? District { get; set; }

        [Display(Name = "Village")]
        public string? Village { get; set; }

        [Display(Name = "Preferred Language")]
        public string PreferredLanguage { get; set; } = "Gujarati";

        // Farmer specific
        [Display(Name = "Land Area (acres)")]
        public decimal? LandArea { get; set; }

        [Display(Name = "Soil Type")]
        public string? SoilType { get; set; }

        [Display(Name = "Main Crop")]
        public string? MainCrop { get; set; }

        [Display(Name = "Farming Experience (years)")]
        public int? FarmingExperience { get; set; }

        // Officer specific
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        [Display(Name = "Experience Years")]
        public int? ExperienceYears { get; set; }

        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }
    }

    public class FarmerCropViewModel
    {
        public int Id { get; set; }
        public int CropId { get; set; }
        public string? CropName { get; set; }
        public string? Season { get; set; }
        public decimal? LandArea { get; set; }
        public DateTime? SowingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public string Status { get; set; } = "Growing";
    }

    public class DiseaseDetectionViewModel
    {
        public string? DetectedDisease { get; set; }
        public string? CropName { get; set; }
        public string? Symptoms { get; set; }
        public string? Solution { get; set; }
        public string? RecommendedPesticide { get; set; }
        public string? SeverityLevel { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public string? UploadedImagePath { get; set; }
        public string? AIAnalysis { get; set; }
    }

    public class EscalationReplyViewModel
    {
        public int EscalationId { get; set; }
        [Required]
        public string OfficerReply { get; set; } = string.Empty;
        [Required]
        public string Status { get; set; } = "Solved";
    }
}

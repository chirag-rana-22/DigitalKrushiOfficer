using System.ComponentModel.DataAnnotations;
namespace Digital_Krushi_Officer.Models.ViewModels;

public class EditCropModel
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string FieldName { get; set; } = "";
    [MaxLength(100)] public string? Variety { get; set; }
    [Range(typeof(decimal), "0.01", "1000000")] public decimal? LandArea { get; set; }
    [DataType(DataType.Date)] public DateTime? SowingDate { get; set; }
    [DataType(DataType.Date)] public DateTime? ExpectedHarvestDate { get; set; }
    public bool EmailRemindersEnabled { get; set; }
}

public class CropPlanModel
{
    public TblFarmerCrop Crop { get; set; } = null!;
    public List<CropTask> Preview { get; set; } = [];
}

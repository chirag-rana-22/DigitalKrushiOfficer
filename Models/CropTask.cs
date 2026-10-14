using System.ComponentModel.DataAnnotations;
namespace Digital_Krushi_Officer.Models;

public class CropTask
{
    public int Id { get; set; }
    public int FarmerCropId { get; set; }
    public TblFarmerCrop? FarmerCrop { get; set; }
    [Required, MaxLength(160)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Notes { get; set; }
    public DateTime DueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(30)] public string Kind { get; set; } = "Custom";
    public DateTime? CompletedAt { get; set; }
    [ConcurrencyCheck] public int Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CropDigest
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public DateTime LocalDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Sending";
    public int Attempts { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

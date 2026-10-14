using Digital_Krushi_Officer.Models;
namespace Digital_Krushi_Officer.Services;

public static class CropPlanning
{
    public static DateTime Today => DateTime.UtcNow.AddHours(5.5).Date;

    public static List<CropTask> Preview(TblFarmerCrop crop)
    {
        if (crop.SowingDate == null || crop.ExpectedHarvestDate == null) return [];
        var sowing = crop.SowingDate.Value.Date;
        var harvest = crop.ExpectedHarvestDate.Value.Date;
        if (harvest <= sowing || (harvest - sowing).TotalDays > 730) return [];
        var tasks = new List<CropTask>();
        void Add(DateTime due, string title, string kind, string notes) => tasks.Add(new CropTask { FarmerCropId = crop.Id, DueDate = due, Title = title, Kind = kind, Notes = notes });
        Add(sowing.AddDays(-7), "Review field preparation", "Preparation", "Review your field, planting materials and any soil-test advice. Adjust this date to your actual plan.");
        Add(sowing, "Confirm sowing", "Sowing", "Record whether sowing took place. Update your crop dates if planting has changed.");
        for (var day = sowing.AddDays(7); day < harvest; day = day.AddDays(7))
            Add(day, "Weekly crop and soil check", "Inspection", "Check plant growth, leaf changes and soil moisture. Ask AI if you notice a problem. This is not an instruction to irrigate or apply chemicals.");
        if ((harvest - sowing).TotalDays > 7) Add(harvest.AddDays(-7), "Prepare for possible harvest", "HarvestPreparation", "Check crop maturity and prepare labour, equipment and storage if needed. The date is an estimate.");
        Add(harvest, "Check readiness for harvest", "HarvestCheck", "Is the crop ready? Confirm harvest only after checking actual maturity; otherwise reschedule this task and update the expected date.");
        return tasks.OrderBy(t => t.DueDate).ToList();
    }
}

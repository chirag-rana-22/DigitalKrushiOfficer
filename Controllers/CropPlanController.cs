using System.Security.Claims;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Controllers;

[Authorize(Roles = "Farmer")]
public class CropPlanController(AppDbContext db, IConfiguration config) : Controller
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    async Task<bool> Save() {
        try { await db.SaveChangesAsync(); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }
    Task<TblFarmerCrop?> OwnCrop(int id) => db.FarmerCrops.Include(c => c.Crop).Include(c => c.Tasks).FirstOrDefaultAsync(c => c.Id == id && c.FarmerId == UserId);
    public async Task<IActionResult> Index(int id)
    {
        var crop = await OwnCrop(id);
        ViewBag.EmailSchedulerEnabled = config.GetValue<bool>("Reminders:Enabled");
        ViewBag.LatestDigest = await db.CropDigests.AsNoTracking().Where(d => d.FarmerId == UserId).OrderByDescending(d => d.LocalDate).FirstOrDefaultAsync();
        return crop == null ? NotFound() : View(new CropPlanModel { Crop = crop, Preview = CropPlanning.Preview(crop) });
    }
    public async Task<IActionResult> Today()
    {
        var today = CropPlanning.Today;
        return View(await db.CropTasks.Include(t => t.FarmerCrop).ThenInclude(c => c!.Crop)
            .Where(t => t.FarmerCrop!.FarmerId == UserId && t.FarmerCrop.Status == "Growing" && t.Status == "Pending" && t.DueDate <= today)
            .OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToListAsync());
    }
    public async Task<IActionResult> Edit(int id)
    {
        var crop = await OwnCrop(id); if (crop == null) return NotFound();
        ViewBag.EmailSchedulerEnabled = config.GetValue<bool>("Reminders:Enabled");
        ViewBag.CropName = ReminderLanguage.CropName(crop, SiteLanguage.Current(HttpContext));
        return View(new EditCropModel { Id = crop.Id, FieldName = crop.FieldName ?? "", Variety = crop.Variety, LandArea = crop.LandArea, SowingDate = crop.SowingDate, ExpectedHarvestDate = crop.ExpectedHarvestDate, EmailRemindersEnabled = crop.EmailRemindersEnabled });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(EditCropModel model)
    {
        var crop = await OwnCrop(model.Id); if (crop == null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.FieldName)) ModelState.AddModelError(nameof(model.FieldName), "Enter a field name.");
        if (model.SowingDate != null && model.ExpectedHarvestDate != null && (model.ExpectedHarvestDate <= model.SowingDate || (model.ExpectedHarvestDate - model.SowingDate).Value.TotalDays > 730))
            ModelState.AddModelError(nameof(model.ExpectedHarvestDate), "Choose a harvest estimate after sowing and within two years. For perennial crops, plan one harvest cycle at a time.");
        if (!ModelState.IsValid) { ViewBag.EmailSchedulerEnabled = config.GetValue<bool>("Reminders:Enabled"); ViewBag.CropName = ReminderLanguage.CropName(crop, SiteLanguage.Current(HttpContext)); return View(model); }
        var datesChanged = crop.SowingDate != model.SowingDate || crop.ExpectedHarvestDate != model.ExpectedHarvestDate;
        crop.FieldName = model.FieldName.Trim(); crop.Variety = model.Variety?.Trim(); crop.LandArea = model.LandArea;
        crop.SowingDate = model.SowingDate?.Date; crop.ExpectedHarvestDate = model.ExpectedHarvestDate?.Date;
        crop.EmailRemindersEnabled = model.EmailRemindersEnabled; crop.UpdatedDate = DateTime.UtcNow;
        crop.PlanVersion++;
        if (!await Save()) return Conflict("This crop was changed in another request. Refresh and try again.");
        TempData["Success"] = datesChanged ? "Crop saved. Existing task dates are unchanged. Review the proposed calendar below or reschedule individual tasks." : "Crop details and email preference saved.";
        return RedirectToAction(nameof(Index), new { id = crop.Id });
    }
    [HttpPost]
    public async Task<IActionResult> AcceptPlan(int id)
    {
        var crop = await OwnCrop(id); if (crop == null) return NotFound();
        var preview = CropPlanning.Preview(crop);
        if (crop.Status != "Growing" || preview.Count == 0) return BadRequest("Set valid sowing and estimated harvest dates for an active crop first.");
        // Completed/skipped tasks and farmer-created tasks remain in the history.
        foreach (var task in crop.Tasks.Where(t => t.Status == "Pending" && t.Kind != "Custom")) { task.Status = "Cancelled"; task.Version++; }
        foreach (var task in preview.Where(t => t.DueDate >= CropPlanning.Today && !crop.Tasks.Any(old => old.Kind == t.Kind && old.DueDate == t.DueDate && old.Status is "Done" or "Skipped"))) db.CropTasks.Add(task);
        crop.PlanVersion++;
        if (!await Save()) return Conflict("This crop was changed in another request. Refresh and try again."); TempData["Success"] = "Calendar confirmed. Past dates were not added as overdue tasks; review upcoming dates and adjust them as needed.";
        return RedirectToAction(nameof(Index), new { id });
    }
    [HttpPost]
    public async Task<IActionResult> AddTask(int id, string? title, DateTime? dueDate, string? notes)
    {
        var crop = await OwnCrop(id); if (crop == null) return NotFound();
        if (!ModelState.IsValid || crop.Status != "Growing" || string.IsNullOrWhiteSpace(title) || title.Length > 160 || notes?.Length > 1000 || dueDate == null || dueDate.Value.Date < CropPlanning.Today || dueDate.Value.Date > CropPlanning.Today.AddYears(2)) return BadRequest("Enter a title, a date within the next two years and notes under 1000 characters.");
        db.CropTasks.Add(new CropTask { FarmerCropId = id, Title = title.Trim(), DueDate = dueDate.Value.Date, Notes = notes?.Trim() });
        crop.PlanVersion++;
        if (!await Save()) return Conflict("This crop was changed in another request. Refresh and try again."); return RedirectToAction(nameof(Index), new { id });
    }
    [HttpPost]
    public async Task<IActionResult> TaskAction(int id, string action, DateTime? dueDate)
    {
        var task = await db.CropTasks.Include(t => t.FarmerCrop).FirstOrDefaultAsync(t => t.Id == id && t.FarmerCrop!.FarmerId == UserId);
        if (task == null) return NotFound();
        if (task.Status != "Pending" || task.FarmerCrop!.Status != "Growing") return Conflict("This task is no longer active. Refresh the calendar.");
        if (action == "done") { task.Status = "Done"; task.CompletedAt = DateTime.UtcNow; }
        else if (action == "skip") task.Status = "Skipped";
        else if (action == "later") task.DueDate = CropPlanning.Today.AddDays(1);
        else if (action == "reschedule" && ModelState.IsValid && dueDate != null && dueDate.Value.Date >= CropPlanning.Today && dueDate.Value.Date <= CropPlanning.Today.AddYears(2)) task.DueDate = dueDate.Value.Date;
        else return BadRequest("Choose a valid action and date.");
        task.Version++;
        if (!await Save()) return Conflict("This task was changed in another request. Refresh and try again."); return RedirectToAction(nameof(Index), new { id = task.FarmerCropId });
    }
    [HttpPost]
    public async Task<IActionResult> ConfirmHarvest(int id, DateTime? harvestedDate)
    {
        var crop = await OwnCrop(id); if (crop == null) return NotFound();
        if (crop.Status != "Growing") return Conflict("This crop is no longer growing.");
        if (!ModelState.IsValid || harvestedDate == null || harvestedDate.Value.Date > CropPlanning.Today || (crop.SowingDate != null && harvestedDate.Value.Date < crop.SowingDate.Value.Date)) return BadRequest("Enter the actual harvest date, no later than today and no earlier than sowing.");
        crop.Status = "Harvested"; crop.HarvestedDate = harvestedDate.Value.Date; crop.UpdatedDate = DateTime.UtcNow;
        foreach (var task in crop.Tasks.Where(t => t.Status == "Pending")) { task.Status = task.Kind == "HarvestCheck" ? "Done" : "Cancelled"; task.CompletedAt = DateTime.UtcNow; task.Version++; }
        crop.PlanVersion++;
        if (!await Save()) return Conflict("This crop was changed in another request. Refresh and try again."); TempData["Success"] = "Harvest recorded. Remaining reminders for this crop have stopped.";
        return RedirectToAction(nameof(Index), new { id });
    }
}

using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Krushi_Officer.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWeatherService _weather;

        public FarmerController(AppDbContext db, IWeatherService weather)
        {
            _db = db;
            _weather = weather;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var user = await _db.Users.Include(u => u.FarmerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            var totalQueries = await _db.Queries.CountAsync(q => q.FarmerId == UserId);
            var pendingQueries = await _db.Queries.CountAsync(q => q.FarmerId == UserId && (q.Status != "Answered" && q.Status != "Closed" || q.Status == "Escalated"));
            var totalCrops = await _db.FarmerCrops.CountAsync(c => c.FarmerId == UserId);
            var totalFeedback = await _db.Feedbacks.CountAsync(f => f.Query!.FarmerId == UserId);
            var recentQueries = await _db.Queries.Where(q => q.FarmerId == UserId)
                .Include(q => q.AIResponse)
                .Include(q => q.Escalation)
                .OrderByDescending(q => q.CreatedDate).Take(5).ToListAsync();
            var activeCrops = await _db.FarmerCrops.Where(c => c.FarmerId == UserId && c.Status == "Growing")
                .Include(c => c.Crop).Take(4).ToListAsync();

            var district = User.FindFirstValue("District") ?? "";
            var village = User.FindFirstValue("Village") ?? "";
            var weatherData = await _weather.GetWeatherAsync(district, village);

            return View(new FarmerDashboardViewModel
            {
                User = user,
                FarmerDetails = user?.FarmerDetails,
                TotalQueries = totalQueries,
                PendingQueries = pendingQueries,
                TotalCrops = totalCrops,
                TotalFeedback = totalFeedback,
                RecentQueries = recentQueries,
                ActiveCrops = activeCrops,
                Weather = weatherData
            });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.Users.Include(u => u.FarmerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            return View(new ProfileViewModel { User = user, FarmerDetails = user?.FarmerDetails });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _db.Users.Include(u => u.FarmerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) return NotFound();
            var fd = user.FarmerDetails;
            return View(new EditProfileViewModel
            {
                Name = user.Name, Mobile = user.Mobile, State = user.State,
                District = user.District, Village = user.Village,
                PreferredLanguage = user.PreferredLanguage,
                LandArea = fd?.LandArea, SoilType = fd?.SoilType,
                MainCrop = fd?.MainCrop, FarmingExperience = fd?.FarmingExperience
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _db.Users.Include(u => u.FarmerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) return NotFound();

            user.Name = model.Name; user.Mobile = model.Mobile;
            user.State = model.State; user.District = model.District;
            user.Village = model.Village; user.PreferredLanguage = model.PreferredLanguage;
            user.UpdatedDate = DateTime.Now;

            if (user.FarmerDetails == null)
                user.FarmerDetails = new TblFarmerDetails { UserId = UserId };

            user.FarmerDetails.LandArea = model.LandArea;
            user.FarmerDetails.SoilType = model.SoilType;
            user.FarmerDetails.MainCrop = model.MainCrop;
            user.FarmerDetails.FarmingExperience = model.FarmingExperience;
            user.FarmerDetails.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Crops()
        {
            var crops = await _db.FarmerCrops.Where(c => c.FarmerId == UserId)
                .Include(c => c.Crop).OrderByDescending(c => c.CreatedDate).ToListAsync();
            ViewBag.AllCrops = await _db.Crops.Where(c => c.IsActive).OrderBy(c => c.CropName).ToListAsync();
            return View(crops);
        }

        public async Task<IActionResult> Diseases(string? search, string? crop)
        {
            var query = _db.DiseaseDatasets.Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(crop))
                query = query.Where(d => d.CropName == crop);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(d => d.DiseaseName!.Contains(search) || d.CropName!.Contains(search) || d.Symptoms!.Contains(search));

            var diseases = await query.OrderBy(d => d.CropName).ThenBy(d => d.DiseaseName).ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCrop = crop;
            ViewBag.CropNames = await _db.DiseaseDatasets
                .Where(d => d.IsActive && d.CropName != null)
                .Select(d => d.CropName!)
                .Distinct().OrderBy(c => c).ToListAsync();

            return View(diseases);
        }


        [HttpPost]
        public async Task<IActionResult> AddCrop(int cropId, decimal? landArea, DateTime? sowingDate, DateTime? harvestDate, string? customCropName)
        {
            customCropName = customCropName?.Trim();
            var validCrop = cropId == -1
                ? !string.IsNullOrWhiteSpace(customCropName) && customCropName.Length <= 100 && !customCropName.Any(char.IsControl)
                : await _db.Crops.AnyAsync(c => c.Id == cropId && c.IsActive);
            if (!ModelState.IsValid || !validCrop || landArea <= 0 || landArea > 1000000 || (sowingDate != null && harvestDate < sowingDate)) return BadRequest("Choose an active crop or enter a crop name (1–100 characters), positive land area and valid dates.");
            _db.FarmerCrops.Add(new TblFarmerCrop
            {
                FarmerId = UserId, CropId = cropId == -1 ? null : cropId,
                CustomCropName = cropId == -1 ? customCropName : null,
                LandArea = landArea, SowingDate = sowingDate,
                ExpectedHarvestDate = harvestDate, Status = "Growing",
                CreatedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Crop added successfully!";
            return RedirectToAction("Crops");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCropStatus(int id, string status)
        {
            if (status is not ("Growing" or "Harvested" or "Failed")) return BadRequest("Invalid crop status.");
            var crop = await _db.FarmerCrops.FirstOrDefaultAsync(c => c.Id == id && c.FarmerId == UserId);
            if (crop != null) { crop.Status = status; crop.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Crops");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCrop(int id)
        {
            var crop = await _db.FarmerCrops.FirstOrDefaultAsync(c => c.Id == id && c.FarmerId == UserId);
            if (crop != null) { _db.FarmerCrops.Remove(crop); await _db.SaveChangesAsync(); }
            return RedirectToAction("Crops");
        }


        public async Task<IActionResult> QueryHistory(string? search, string? status, int page = 1)
        {
            page = Math.Clamp(page, 1, 10000);
            var source = _db.Queries.AsNoTracking().Where(q => q.FarmerId == UserId);
            if (!string.IsNullOrWhiteSpace(search)) source = source.Where(q => q.QueryText != null && q.QueryText.Contains(search));
            if (!string.IsNullOrWhiteSpace(status)) source = source.Where(q => q.Status == status);
            var queries = await source.Include(q => q.AIResponse).Include(q => q.Escalation).ThenInclude(e => e!.Officer)
                .Include(q => q.Feedbacks).OrderByDescending(q => q.CreatedDate).ThenByDescending(q => q.Id).Skip((page - 1) * 50).Take(51).ToListAsync();
            ViewBag.Page = page; ViewBag.HasNext = queries.Count > 50; ViewBag.Search = search; ViewBag.StatusFilter = status;
            return View(queries.Take(50).ToList());
        }

        public async Task<IActionResult> OfficerReplies()
        {
            var queries = await _db.Queries
                .Where(q => q.FarmerId == UserId &&
                    ((q.Escalation != null && q.Escalation.OfficerReply != null) ||
                     (q.AIResponse != null && (q.AIResponse.ApprovalStatus == ApprovalStatus.Approved || q.AIResponse.ApprovalStatus == ApprovalStatus.Modified))))
                .Include(q => q.AIResponse)
                .Include(q => q.Escalation).ThenInclude(e => e!.Officer)
                .Include(q => q.Feedbacks)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();
            return View(queries);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int queryId, int rating, string? feedbackText, string? feedbackType)
        {
            if (rating < 1 || rating > 5 || feedbackText?.Length > 2000 || feedbackType?.Length > 50) return BadRequest("Enter a rating from 1 to 5 and feedback under 2000 characters.");
            var query = await _db.Queries.FirstOrDefaultAsync(q => q.Id == queryId && q.FarmerId == UserId && q.Status == "Answered");
            if (query == null) return NotFound();

            var existing = await _db.Feedbacks.FirstOrDefaultAsync(f => f.QueryId == queryId);
            if (existing != null) { existing.Rating = rating; existing.FeedbackText = feedbackText; existing.FeedbackType = feedbackType; }
            else
                _db.Feedbacks.Add(new TblFeedback { QueryId = queryId, Rating = rating, FeedbackText = feedbackText, FeedbackType = feedbackType });

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}

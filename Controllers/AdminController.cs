using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly Digital_Krushi_Officer.Services.ImageUploadService _uploads;

        public AdminController(AppDbContext db, IWebHostEnvironment env, Digital_Krushi_Officer.Services.ImageUploadService uploads) { _db = db; _env = env; _uploads = uploads; }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalFarmers = await _db.Users.CountAsync(u => u.Role == "Farmer" && u.IsActive),
                TotalOfficers = await _db.Users.CountAsync(u => u.Role == "Officer" && u.IsActive),
                TotalQueries = await _db.Queries.CountAsync(),
                PendingQueries = await _db.Queries.CountAsync(q => q.Status != "Answered" && q.Status != "Closed"),
                PendingEscalations = await _db.Escalations.CountAsync(e => e.Status == "Pending"),
                SolvedEscalations = await _db.Escalations.CountAsync(e => e.Status == "Solved"),
                TotalFeedback = await _db.Feedbacks.CountAsync(),
                AvgRating = await _db.Feedbacks.AverageAsync(f => (double?)f.Rating) ?? 0,
                TotalCrops = await _db.Crops.CountAsync(c => c.IsActive),
                TotalDiseases = await _db.DiseaseDatasets.CountAsync(d => d.IsActive),
                ActiveSchemes = await _db.GovernmentSchemes.CountAsync(s => s.Status == "Active"),
                RecentFarmers = await _db.Users.Where(u => u.Role == "Farmer" && u.IsActive)
                    .OrderByDescending(u => u.CreatedDate).Take(5).ToListAsync(),
                RecentQueries = await _db.Queries.Include(q => q.Farmer).Include(q => q.AIResponse)
                    .OrderByDescending(q => q.CreatedDate).Take(5).ToListAsync()
            };
            return View(vm);
        }

        // --- Farmers ---
        public async Task<IActionResult> Farmers(string? search)
        {
            var q = _db.Users.Include(u => u.FarmerDetails).Where(u => u.Role == "Farmer");
            if (!string.IsNullOrEmpty(search))
                q = q.Where(u => u.Name.Contains(search) || u.Email.Contains(search) || (u.District != null && u.District.Contains(search)));
            ViewBag.Search = search;
            return View(await q.OrderByDescending(u => u.CreatedDate).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFarmerStatus(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Farmer");
            if (user != null) { user.IsActive = !user.IsActive; user.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Farmers");
        }

        // --- Farmer-Officer Assignment ---
        public async Task<IActionResult> AssignFarmerToOfficer(string? district, string? search)
        {
            // Get all districts from farmers
            var districts = await _db.Users
                .Where(u => u.Role == "Farmer" && !string.IsNullOrEmpty(u.District))
                .Select(u => u.District)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Districts = districts;
            ViewBag.SelectedDistrict = district;

            var vm = new List<dynamic>();

            if (!string.IsNullOrEmpty(district))
            {
                // Get farmers in this district who don't have an officer assigned
                var farmersInDistrict = await _db.Users
                    .Where(u => u.Role == "Farmer" && u.District == district && u.IsActive)
                    .Include(u => u.FarmerDetails)
                    .ToListAsync();

                // Get officers who can cover this district
                var officers = await _db.Users
                    .Where(u => u.Role == "Officer" && u.IsActive)
                    .Include(u => u.OfficerDetails)
                    .ToListAsync();

                // Filter officers by district if needed
                var availableOfficers = officers
                    .Where(o => o.District == district || string.IsNullOrEmpty(o.District))
                    .ToList();

                foreach (var farmer in farmersInDistrict)
                {
                    if (!string.IsNullOrEmpty(search) && !farmer.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var assignment = await _db.FarmerOfficerAssignments
                        .FirstOrDefaultAsync(a => a.FarmerId == farmer.Id && a.IsActive);

                    vm.Add(new
                    {
                        Farmer = farmer,
                        AssignedOfficer = assignment?.Officer,
                        AssignmentId = assignment?.Id,
                        AvailableOfficers = availableOfficers
                    });
                }
            }

            ViewBag.Search = search;
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AssignFarmerToOfficer(int farmerId, int officerId, string? remarks)
        {
            var farmer = await _db.Users.FindAsync(farmerId);
            var officer = await _db.Users.FindAsync(officerId);

            if (farmer?.Role != "Farmer" || officer?.Role != "Officer" || !farmer.IsActive || !officer.IsActive || !officer.IsEmailVerified || remarks?.Length > 500)
                return BadRequest("Invalid farmer or officer");

            // Remove previous assignment if exists
            var oldAssignment = await _db.FarmerOfficerAssignments
                .FirstOrDefaultAsync(a => a.FarmerId == farmerId && a.IsActive);

            if (oldAssignment != null)
            {
                oldAssignment.IsActive = false;
                oldAssignment.UpdatedDate = DateTime.Now;
            }

            // Create new assignment
            var assignment = await _db.FarmerOfficerAssignments.FirstOrDefaultAsync(a => a.FarmerId == farmerId && a.OfficerId == officerId);
            if (assignment != null) { assignment.IsActive = true; assignment.Remarks = remarks; assignment.UpdatedDate = DateTime.Now; }
            else assignment = new TblFarmerOfficerAssignment
            {
                FarmerId = farmerId,
                OfficerId = officerId,
                District = farmer.District,
                Village = farmer.Village,
                Remarks = remarks,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            if (assignment.Id == 0) _db.FarmerOfficerAssignments.Add(assignment);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Farmer {farmer.Name} assigned to Officer {officer.Name}";
            return RedirectToAction("AssignFarmerToOfficer", new { district = farmer.District });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignFarmer(int farmerId)
        {
            var assignment = await _db.FarmerOfficerAssignments
                .FirstOrDefaultAsync(a => a.FarmerId == farmerId && a.IsActive);

            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.UpdatedDate = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Farmer unassigned successfully";
            }

            return RedirectToAction("AssignFarmerToOfficer");
        }

        [HttpGet]
        [Route("Admin/ViewOfficerFarmers/{officerId}")]
        public async Task<IActionResult> ViewOfficerFarmers(int officerId)
        {
            var officer = await _db.Users
                .Include(u => u.OfficerDetails)
                .FirstOrDefaultAsync(u => u.Id == officerId && u.Role == "Officer");

            if (officer == null)
                return NotFound();

            var assignments = await _db.FarmerOfficerAssignments
                .Where(a => a.OfficerId == officerId && a.IsActive)
                .Include(a => a.Farmer)
                .Include(a => a.Farmer!.FarmerDetails)
                .OrderBy(a => a.Farmer!.District)
                .ToListAsync();

            ViewBag.Officer = officer;
            return View(assignments);
        }

        // --- Officers ---
        public async Task<IActionResult> Officers()
        {
            var officers = await _db.Users.Include(u => u.OfficerDetails)
                .Where(u => u.Role == "Officer").OrderByDescending(u => u.CreatedDate).ToListAsync();
            return View(officers);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOfficerStatus(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Officer" && u.IsEmailVerified);
            if (user != null) { user.IsActive = !user.IsActive; user.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Officers");
        }

        // --- Crops ---
        public async Task<IActionResult> Crops()
            => View(await _db.Crops.OrderByDescending(c => c.CreatedDate).ToListAsync());

        [HttpGet]
        public IActionResult AddCrop() => View(new TblCrop());

        [HttpPost]
        public async Task<IActionResult> AddCrop(TblCrop model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedDate = DateTime.Now;
            _db.Crops.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Crop added!";
            return RedirectToAction("Crops");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCropStatus(int id)
        {
            var crop = await _db.Crops.FindAsync(id);
            if (crop != null) { crop.IsActive = !crop.IsActive; crop.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Crops");
        }

        [HttpGet]
        public async Task<IActionResult> EditCrop(int id)
        {
            var crop = await _db.Crops.FindAsync(id);
            if (crop == null) return NotFound();
            return View(crop);
        }

        [HttpPost]
        public async Task<IActionResult> EditCrop(TblCrop model)
        {
            if (!ModelState.IsValid) return View(model);
            var crop = await _db.Crops.FindAsync(model.Id);
            if (crop == null) return NotFound();
            crop.CropName = model.CropName;
            crop.Season = model.Season;
            crop.Description = model.Description;
            crop.IsActive = model.IsActive;
            crop.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Crop updated successfully!";
            return RedirectToAction("Crops");
        }

        // --- Government Schemes ---
        public async Task<IActionResult> Schemes()
            => View(await _db.GovernmentSchemes.OrderByDescending(s => s.CreatedDate).ToListAsync());

        [HttpGet]
        public IActionResult AddScheme() => View(new TblGovernmentScheme());

        [HttpPost]
        public async Task<IActionResult> AddScheme(TblGovernmentScheme model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedDate = DateTime.Now;
            _db.GovernmentSchemes.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Scheme added!";
            return RedirectToAction("Schemes");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSchemeStatus(int id)
        {
            var scheme = await _db.GovernmentSchemes.FindAsync(id);
            if (scheme != null) { scheme.IsActive = !scheme.IsActive; scheme.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Schemes");
        }

        [HttpGet]
        public async Task<IActionResult> EditScheme(int id)
        {
            var scheme = await _db.GovernmentSchemes.FindAsync(id);
            if (scheme == null) return NotFound();
            return View(scheme);
        }

        [HttpPost]
        public async Task<IActionResult> EditScheme(TblGovernmentScheme model)
        {
            if (!ModelState.IsValid) return View(model);
            var scheme = await _db.GovernmentSchemes.FindAsync(model.Id);
            if (scheme == null) return NotFound();
            scheme.SchemeName = model.SchemeName;
            scheme.Description = model.Description;
            scheme.Eligibility = model.Eligibility;
            scheme.Category = model.Category;
            scheme.Status = model.Status;
            scheme.StartDate = model.StartDate;
            scheme.LastDate = model.LastDate;
            scheme.DocumentUrl = model.DocumentUrl;
            scheme.IsActive = model.IsActive;
            scheme.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Scheme updated successfully!";
            return RedirectToAction("Schemes");
        }

        // --- Disease Dataset ---
        public async Task<IActionResult> Diseases()
            => View(await _db.DiseaseDatasets.OrderByDescending(d => d.CreatedDate).ToListAsync());

        [HttpGet]
        public IActionResult AddDisease() => View(new TblDiseaseDataset());

        [HttpPost]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> AddDisease(TblDiseaseDataset model, IFormFile? DiseaseImage)
        {
            if (!ModelState.IsValid) return View(model);

            // Handle image upload
            if (DiseaseImage != null && DiseaseImage.Length > 0)
            {
                try {
                    var saved = await _uploads.SaveAsync(DiseaseImage, true);
                    model.ReferenceImagePath = "/uploads/diseases/" + saved.FileName;
                } catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            model.CreatedDate = DateTime.Now;
            _db.DiseaseDatasets.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Disease record added!";
            return RedirectToAction("Diseases");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDiseaseStatus(int id)
        {
            var d = await _db.DiseaseDatasets.FindAsync(id);
            if (d != null) { d.IsActive = !d.IsActive; d.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); }
            return RedirectToAction("Diseases");
        }

        [HttpGet]
        public async Task<IActionResult> EditDisease(int id)
        {
            var d = await _db.DiseaseDatasets.FindAsync(id);
            if (d == null) return NotFound();
            return View(d);
        }

        [HttpPost]
        public async Task<IActionResult> EditDisease(TblDiseaseDataset model)
        {
            if (!ModelState.IsValid) return View(model);
            var d = await _db.DiseaseDatasets.FindAsync(model.Id);
            if (d == null) return NotFound();
            d.CropName = model.CropName;
            d.DiseaseName = model.DiseaseName;
            d.Symptoms = model.Symptoms;
            d.Solution = model.Solution;
            d.RecommendedPesticide = model.RecommendedPesticide;
            d.SeverityLevel = model.SeverityLevel;
            d.ReferenceImagePath = model.ReferenceImagePath;
            d.IsActive = model.IsActive;
            d.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Disease record updated successfully!";
            return RedirectToAction("Diseases");
        }

        // --- Feedback ---
        public async Task<IActionResult> Feedback()
        {
            var feedback = await _db.Feedbacks
                .Include(f => f.Query).ThenInclude(q => q!.Farmer)
                .OrderByDescending(f => f.CreatedDate).ToListAsync();
            return View(feedback);
        }

        // --- Queries ---

        public async Task<IActionResult> Queries(string? status, string? search, int page = 1)
        {
            page = Math.Clamp(page, 1, 10000);
            var q = _db.Queries.AsNoTracking().Include(q => q.Farmer).Include(q => q.AIResponse).AsQueryable();
            if (!string.IsNullOrEmpty(status)) q = q.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.QueryText != null && x.QueryText.Contains(search));
            var items = await q.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).Skip((page - 1) * 50).Take(51).ToListAsync();
            ViewBag.StatusFilter = status; ViewBag.Search = search; ViewBag.Page = page; ViewBag.HasNext = items.Count > 50;
            return View(items.Take(50).ToList());
        }

        public async Task<IActionResult> ReviewQueue() {
            ViewBag.Officers = await _db.Users.Where(u => u.Role == "Officer" && u.IsActive && u.IsEmailVerified).OrderBy(u => u.Name).ToListAsync();
            return View(await _db.Queries.Include(q => q.Farmer).Include(q => q.Escalation).Where(q => q.Status != "Answered" && q.Status != "Closed").OrderBy(q => q.CreatedDate).ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> AssignQuery(int queryId, int officerId) {
            var query = await _db.Queries.Include(q => q.Escalation).SingleOrDefaultAsync(q => q.Id == queryId && q.Status != "Answered" && q.Status != "Closed");
            if (query == null || !await _db.Users.AnyAsync(u => u.Id == officerId && u.Role == "Officer" && u.IsActive && u.IsEmailVerified)) return BadRequest("Choose an active verified officer and an open query.");
            query.Escalation ??= new TblEscalation { QueryId = queryId };
            query.Escalation.OfficerId = officerId; query.Escalation.AssignedDate = DateTime.Now; query.Escalation.Status = "Pending";
            query.Status = "PendingApproval"; await _db.SaveChangesAsync();
            TempData["Success"] = "Query assigned for review."; return RedirectToAction(nameof(ReviewQueue));
        }

        // --- Analytics ---
        public async Task<IActionResult> Analytics()
        {
            var queryByType = await _db.Queries
                .GroupBy(q => q.QueryType)
                .Select(g => new { Type = g.Key, Count = g.Count() }).ToListAsync();

            var queryByStatus = await _db.Queries
                .GroupBy(q => q.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

            var aiModels = await _db.AIResponses
                .GroupBy(a => a.AIModel)
                .Select(g => new { Model = g.Key, Count = g.Count(), AvgConf = g.Average(a => (double?)a.ConfidenceScore) }).ToListAsync();

            ViewBag.QueryByType = queryByType;
            ViewBag.QueryByStatus = queryByStatus;
            ViewBag.AIModels = aiModels;
            return View();
        }
    }
}

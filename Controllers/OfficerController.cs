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
    [Authorize(Roles = "Officer")]
    public class OfficerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public OfficerController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var user = await _db.Users.Include(u => u.OfficerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            var pending = await _db.Escalations.CountAsync(e => e.OfficerId == UserId && e.Status == "Pending");
            var inProgress = await _db.Escalations.CountAsync(e => e.OfficerId == UserId && e.Status == "InProgress");
            var solved = await _db.Escalations.CountAsync(e => e.OfficerId == UserId && e.Status == "Solved");
            var total = await _db.Escalations.CountAsync(e => e.OfficerId == UserId);

            var recent = await _db.Escalations
                .Where(e => e.OfficerId == UserId)
                .Include(e => e.Query).ThenInclude(q => q!.Farmer)
                .Include(e => e.Query).ThenInclude(q => q!.AIResponse)
                .OrderByDescending(e => e.CreatedDate)
                .Take(10).ToListAsync();

            return View(new OfficerDashboardViewModel
            {
                User = user,
                OfficerDetails = user?.OfficerDetails,
                PendingEscalations = pending,
                InProgressEscalations = inProgress,
                SolvedEscalations = solved,
                TotalEscalations = total,
                RecentEscalations = recent
            });
        }

        public async Task<IActionResult> Escalations(string? status)
        {
            var query = _db.Escalations
                .Where(e => e.OfficerId == UserId)
                .Include(e => e.Query).ThenInclude(q => q!.Farmer)
                .Include(e => e.Query).ThenInclude(q => q!.AIResponse)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            ViewBag.StatusFilter = status;
            return View(await query.OrderByDescending(e => e.CreatedDate).ToListAsync());
        }

        public async Task<IActionResult> EscalationDetail(int id)
        {
            var esc = await _db.Escalations
                .Include(e => e.Query).ThenInclude(q => q!.Farmer).ThenInclude(f => f!.FarmerDetails)
                .Include(e => e.Query).ThenInclude(q => q!.AIResponse)
                .Include(e => e.Query).ThenInclude(q => q!.Feedbacks)
                .FirstOrDefaultAsync(e => e.Id == id && e.OfficerId == UserId);

            if (esc == null) return NotFound();
            return View(esc);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(EscalationReplyViewModel model)
        {
            var esc = await _db.Escalations
                .Include(e => e.Query).ThenInclude(q => q!.Farmer)
                .FirstOrDefaultAsync(e => e.Id == model.EscalationId && e.OfficerId == UserId);

            if (esc == null) return NotFound();

            esc.OfficerReply = model.OfficerReply;
            esc.Status = model.Status;
            esc.ResolvedDate = model.Status == "Solved" ? DateTime.Now : null;
            esc.UpdatedDate = DateTime.Now;

            if (esc.Query != null)
            {
                esc.Query.Status = model.Status == "Solved" ? "Answered" : "Escalated";
                esc.Query.UpdatedDate = DateTime.Now;
            }
            await _db.SaveChangesAsync();

            // Notify farmer
            var farmer = esc.Query?.Farmer;
            var officer = await _db.Users.FindAsync(UserId);
            if (farmer != null && officer != null)
                await _email.SendOfficerReplyNotificationAsync(farmer.Email, farmer.Name, officer.Name, model.OfficerReply);

            TempData["Success"] = "Reply sent to farmer!";
            return RedirectToAction("Escalations");
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _db.Users.Include(u => u.OfficerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            return View(new ProfileViewModel { User = user, OfficerDetails = user?.OfficerDetails });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _db.Users.Include(u => u.OfficerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) return NotFound();
            var od = user.OfficerDetails;
            return View(new EditProfileViewModel
            {
                Name = user.Name,
                Mobile = user.Mobile,
                State = user.State,
                District = user.District,
                Village = user.Village,
                PreferredLanguage = user.PreferredLanguage,
                Department = od?.Department,
                Designation = od?.Designation,
                ExperienceYears = od?.ExperienceYears,
                Specialization = od?.Specialization
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _db.Users.Include(u => u.OfficerDetails).FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) return NotFound();

            user.Name = model.Name; user.Mobile = model.Mobile;
            user.State = model.State; user.District = model.District;
            user.Village = model.Village; user.PreferredLanguage = model.PreferredLanguage;
            user.UpdatedDate = DateTime.Now;

            if (user.OfficerDetails == null)
                user.OfficerDetails = new TblOfficerDetails { UserId = UserId };

            user.OfficerDetails.Department = model.Department;
            user.OfficerDetails.Designation = model.Designation;
            user.OfficerDetails.ExperienceYears = model.ExperienceYears;
            user.OfficerDetails.Specialization = model.Specialization;
            user.OfficerDetails.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Profile");
        }

        // ── AI Response Review Actions ──
        public async Task<IActionResult> PendingReviews()
        {
            ViewBag.ActiveMenu = "PendingReviews";
            var pendingResponses = await _db.AIResponses
                .Where(r => r.ApprovalStatus == ApprovalStatus.Pending &&
                            r.ComplexityLevel == ComplexityLevel.Complex)
                .Include(r => r.Query).ThenInclude(q => q!.Farmer)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(pendingResponses);
        }

        public async Task<IActionResult> ReviewResponse(int id)
        {
            var response = await _db.AIResponses
                .Include(r => r.Query).ThenInclude(q => q!.Farmer)
                .FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);

            if (response == null) return NotFound();
            return View(response);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveResponse(int id, string? officerNotes)
        {
            var response = await _db.AIResponses
                .Include(r => r.Query)
                .FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);

            if (response == null) return NotFound();

            response.ApprovalStatus = ApprovalStatus.Approved;
            response.ApprovedByOfficerId = UserId;
            response.ApprovedDate = DateTime.Now;
            response.OfficerNotes = officerNotes;

            if (response.Query != null)
                response.Query.Status = "Answered";

            await _db.SaveChangesAsync();
            TempData["Success"] = "Response approved! Farmer will see the answer.";
            return RedirectToAction("PendingReviews");
        }

        [HttpPost]
        public async Task<IActionResult> RejectResponse(int id, string? rejectionReason)
        {
            var response = await _db.AIResponses
                .Include(r => r.Query)
                .FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);

            if (response == null) return NotFound();

            response.ApprovalStatus = ApprovalStatus.Rejected;
            response.ApprovedByOfficerId = UserId;
            response.ApprovedDate = DateTime.Now;
            response.OfficerNotes = $"Rejected - {rejectionReason}";

            if (response.Query != null)
                response.Query.Status = "NeedsRevision";

            await _db.SaveChangesAsync();
            TempData["Warning"] = "Response rejected. AI will generate a new answer.";
            return RedirectToAction("PendingReviews");
        }

        [HttpPost]
        public async Task<IActionResult> ModifyAndApprove(int id, string modifiedAnswer, string? officerNotes)
        {
            var response = await _db.AIResponses
                .Include(r => r.Query)
                .FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);

            if (response == null) return NotFound();

            response.AIAnswer = modifiedAnswer;
            response.ApprovalStatus = ApprovalStatus.Modified;
            response.ApprovedByOfficerId = UserId;
            response.ApprovedDate = DateTime.Now;
            response.OfficerNotes = officerNotes;

            if (response.Query != null)
                response.Query.Status = "Answered";

            await _db.SaveChangesAsync();
            TempData["Success"] = "Response modified and approved! Farmer will see the updated answer.";
            return RedirectToAction("PendingReviews");
        }
    }
}

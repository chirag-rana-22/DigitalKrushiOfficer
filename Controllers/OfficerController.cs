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
            await LoadConversationContext(esc.Query!);
            return View(esc);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(EscalationReplyViewModel model)
        {
            var esc = await _db.Escalations
                .Include(e => e.Query).ThenInclude(q => q!.Farmer)
                .Include(e => e.Query).ThenInclude(q => q!.AIResponse)
                .FirstOrDefaultAsync(e => e.Id == model.EscalationId && e.OfficerId == UserId);

            if (esc == null) return NotFound();

            if (esc.Status is not ("Pending" or "InProgress")) {
                TempData["Warning"] = "This query has already been closed. Refresh the list to see its current status.";
                return RedirectToAction(nameof(EscalationDetail), new { id = esc.Id });
            }
            if (model.Status is not ("Solved" or "InProgress" or "Rejected"))
                ModelState.AddModelError(nameof(model.Status), "Choose In Progress, Solved or Rejected.");
            if (string.IsNullOrWhiteSpace(model.OfficerReply))
                ModelState.AddModelError(nameof(model.OfficerReply), "Enter a reply or a reason explaining why you cannot solve this query.");
            if (!ModelState.IsValid) { await LoadConversationContext(esc.Query!); return View("EscalationDetail", esc); }

            esc.OfficerReply = model.OfficerReply.Trim();
            esc.Status = model.Status;
            esc.ResolvedDate = model.Status is "Solved" or "Rejected" ? DateTime.Now : null;
            esc.UpdatedDate = DateTime.Now;

            if (esc.Query != null)
            {
                esc.Query.Status = model.Status == "Solved" ? "Answered" : model.Status == "Rejected" ? "Rejected" : "Escalated";
                if (model.Status == "Rejected" && esc.Query.AIResponse != null)
                    esc.Query.AIResponse.ApprovalStatus = ApprovalStatus.Rejected;
                if (model.Status == "Solved" && esc.Query.AIResponse != null) QueryWorkflow.Publish(esc.Query.AIResponse, UserId, "Officer replied directly", model.OfficerReply);
                esc.Query.UpdatedDate = DateTime.Now;
            }
            await _db.SaveChangesAsync();

            // Notify farmer
            var farmer = esc.Query?.Farmer;
            var officer = await _db.Users.FindAsync(UserId);
            if (farmer != null && officer != null)
                await _email.SendOfficerReplyNotificationAsync(farmer.Email, farmer.Name, officer.Name, model.OfficerReply);

            TempData["Success"] = model.Status == "Rejected" ? "Query rejected. Your reason has been sent to the farmer." : "Reply sent to farmer!";
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

        private async Task LoadConversationContext(TblQuery query) {
            var root = query.ParentQueryId ?? query.Id;
            ViewBag.ConversationContext = await _db.Queries.AsNoTracking()
                .Where(q => q.FarmerId == query.FarmerId && q.Id < query.Id && (q.Id == root || q.ParentQueryId == root))
                .Include(q => q.AIResponse).Include(q => q.Escalation).OrderBy(q => q.Id).ToListAsync();
        }

        // ── AI Response Review Actions ──

        private IQueryable<TblAIResponse> AssignedResponses() => _db.AIResponses
            .Include(r => r.Query).ThenInclude(q => q!.Escalation)
            .Include(r => r.Query).ThenInclude(q => q!.Farmer)
            .Where(r => r.Query != null && r.Query.Escalation != null && r.Query.Escalation.OfficerId == UserId && r.Query.Escalation.Status != "Rejected");
        public async Task<IActionResult> PendingReviews() {
            ViewBag.ActiveMenu = "PendingReviews";
            return View(await AssignedResponses().Where(r => r.ApprovalStatus == ApprovalStatus.Pending || r.ApprovalStatus == ApprovalStatus.Rejected).OrderBy(r => r.CreatedDate).ToListAsync());
        }
        public async Task<IActionResult> ReviewResponse(int id) {
            var response = await AssignedResponses().FirstOrDefaultAsync(r => r.Id == id);
            if (response == null) return NotFound();
            await LoadConversationContext(response.Query!);
            return View(response);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveResponse(int id, string? officerNotes) {
            var response = await AssignedResponses().FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);
            if (response == null) return NotFound();
            if (response.AIModel == "Unavailable" || string.IsNullOrWhiteSpace(response.AIAnswer) || officerNotes?.Length > 1000) return BadRequest("Write a reviewed answer using Modify and approve.");
            QueryWorkflow.Publish(response, UserId, officerNotes); await _db.SaveChangesAsync();
            TempData["Success"] = "Reviewed answer published to the farmer."; return RedirectToAction(nameof(PendingReviews));
        }
        [HttpPost]
        public async Task<IActionResult> RejectResponse(int id, string? rejectionReason) {
            var response = await AssignedResponses().FirstOrDefaultAsync(r => r.Id == id && r.ApprovalStatus == ApprovalStatus.Pending);
            if (response == null) return NotFound();
            if (string.IsNullOrWhiteSpace(rejectionReason) || rejectionReason.Length > 950) return BadRequest("Provide a rejection reason under 950 characters.");
            response.ApprovalStatus = ApprovalStatus.Rejected; response.OfficerNotes = rejectionReason;
            response.ApprovedByOfficerId = UserId; response.ApprovedDate = DateTime.Now; response.Query!.Status = "NeedsRevision";
            if (response.Query.Escalation != null) response.Query.Escalation.Status = "InProgress";
            await _db.SaveChangesAsync(); TempData["Warning"] = "Draft rejected. Edit and approve a corrected answer from your review queue.";
            return RedirectToAction(nameof(PendingReviews));
        }
        [HttpPost]
        public async Task<IActionResult> ModifyAndApprove(int id, string modifiedAnswer, string? officerNotes) {
            var response = await AssignedResponses().FirstOrDefaultAsync(r => r.Id == id && (r.ApprovalStatus == ApprovalStatus.Pending || r.ApprovalStatus == ApprovalStatus.Rejected));
            if (response == null) return NotFound();
            if (string.IsNullOrWhiteSpace(modifiedAnswer) || modifiedAnswer.Length > 16000 || officerNotes?.Length > 1000) return BadRequest("Write an answer under 16000 characters and notes under 1000 characters.");
            QueryWorkflow.Publish(response, UserId, officerNotes, modifiedAnswer); await _db.SaveChangesAsync();
            TempData["Success"] = "Corrected answer published to the farmer."; return RedirectToAction(nameof(PendingReviews));
        }
    }
}

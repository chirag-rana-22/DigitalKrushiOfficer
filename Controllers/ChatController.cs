using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Digital_Krushi_Officer.Controllers;
[Authorize(Roles = "Farmer")]
public class ChatController(AppDbContext db, QueryWorkflow workflow, ImageUploadService uploads) : Controller {
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Language => User.FindFirstValue("Language") ?? "Gujarati";
    public async Task<IActionResult> Index() {
        var queries = await db.Queries.AsNoTracking().Where(q => q.FarmerId == UserId).Include(q => q.AIResponse).Include(q => q.Escalation).ThenInclude(e => e!.Officer).Include(q => q.Feedbacks).OrderByDescending(q => q.CreatedDate).Take(50).ToListAsync();
        return View(new ChatViewModel { Messages = queries.AsEnumerable().Reverse().Select(q => new ChatMessageViewModel {
            QueryId=q.Id, QueryText=q.QueryText, QueryType=q.QueryType ?? "Text", ImagePath=q.ImagePath == null ? null : Url.Action("QueryImage", "Media", new { id=q.Id }),
            AIAnswer=AdvisoryPolicy.IsPublished(q.AIResponse) ? q.AIResponse!.AIAnswer : null, Status=q.Status, CreatedDate=q.CreatedDate,
            OfficerReply=q.Escalation?.OfficerReply, IsEscalated=q.AIResponse?.IsEscalated ?? false, ApprovalStatus=q.AIResponse?.ApprovalStatus.ToString() ?? "Pending", FeedbackRating=q.Feedbacks.FirstOrDefault()?.Rating
        }).ToList() });
    }
    private IActionResult Saved(TblQuery q) => Json(new {
        success=true, queryId=q.Id,
        answer=AdvisoryPolicy.IsPublished(q.AIResponse) ? q.AIResponse!.AIAnswer : null,
        status=q.Status, approvalStatus=q.AIResponse?.ApprovalStatus.ToString(),
        isEscalated=q.AIResponse?.IsEscalated ?? false,
        message=q.Status == "Answered" ? "AI answered your question." : AdvisoryPolicy.PendingMessage(q.Status)
    });
    [HttpPost, EnableRateLimiting("advisory")]
    public async Task<IActionResult> Ask([FromBody] AskRequest? request) {
        if (request == null || string.IsNullOrWhiteSpace(request.QueryText) || request.QueryText.Length > 4000) return BadRequest(new { success=false, message="Enter a question of 1–4000 characters." });
        return Saved(await workflow.SubmitAsync(UserId, request.QueryText.Trim(), "Text", Language));
    }
    [HttpPost, EnableRateLimiting("advisory"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile? image, string? userPrompt, string? cropName) {
        if (image == null || (userPrompt?.Length ?? 0)>4000 || (cropName?.Length ?? 0)>100) return BadRequest(new { message="Choose a photo and a description under 4000 characters." });
        StoredImage stored;
        try { stored=await uploads.SaveAsync(image); } catch (ArgumentException ex) { return BadRequest(new { message=ex.Message }); }
        var text=string.IsNullOrWhiteSpace(userPrompt) ? $"Please review the visible condition of this {cropName ?? "crop"}." : userPrompt.Trim();
        try { return Saved(await workflow.SubmitAsync(UserId,text,"Image",Language,stored.Bytes,"private:"+stored.FileName)); }
        catch { System.IO.File.Delete(Path.Combine(uploads.PrivateRoot,stored.FileName)); throw; }
    }
    [HttpPost, EnableRateLimiting("advisory")]
    public async Task<IActionResult> UploadVoice(string? transcribedText) {
        if (string.IsNullOrWhiteSpace(transcribedText) || transcribedText.Length>4000) return BadRequest(new { message="Review the voice transcript before sending it." });
        return Saved(await workflow.SubmitAsync(UserId,transcribedText.Trim(),"Voice",Language));
    }
    public async Task<IActionResult> DiseaseDetection(string? search,string? crop) {
        var query=db.DiseaseDatasets.Where(d=>d.IsActive);
        if (!string.IsNullOrEmpty(crop)) query=query.Where(d=>d.CropName==crop);
        if (!string.IsNullOrEmpty(search)) query=query.Where(d=>d.DiseaseName!.Contains(search)||d.CropName!.Contains(search)||d.Symptoms!.Contains(search));
        ViewBag.Search=search; ViewBag.SelectedCrop=crop;
        ViewBag.CropNames=await db.DiseaseDatasets.Where(d=>d.IsActive && d.CropName!=null).Select(d=>d.CropName!).Distinct().OrderBy(c=>c).ToListAsync();
        return View(await query.OrderBy(d=>d.CropName).ThenBy(d=>d.DiseaseName).ToListAsync());
    }
}
public class AskRequest { public string QueryText { get; set; }=""; }

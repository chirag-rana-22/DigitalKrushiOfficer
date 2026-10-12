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
public class ChatController(AppDbContext db, QueryWorkflow workflow, ImageUploadService uploads, ILogger<ChatController> logger) : Controller {
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Language => User.FindFirstValue("Language") ?? "Gujarati";
    private Task<bool> OwnsConversation(int? id) => id == null ? Task.FromResult(true) : db.Queries.AnyAsync(q => q.Id == id && q.FarmerId == UserId && q.ParentQueryId == null);
    private async Task<List<ChatHistoryItemViewModel>> History(string? search = null) {
        var roots=db.Queries.AsNoTracking().Where(q=>q.FarmerId==UserId && q.ParentQueryId==null);
        if(!string.IsNullOrWhiteSpace(search)) roots=roots.Where(q=>q.QueryText!=null && q.QueryText.Contains(search));
        return await roots.Select(q=>new ChatHistoryItemViewModel {
            Id=q.Id,Title=q.QueryText ?? "Photo conversation",
            UpdatedAt=db.Queries.Where(x=>x.FarmerId==UserId && (x.Id==q.Id || x.ParentQueryId==q.Id)).Max(x=>x.CreatedDate)
        }).OrderByDescending(q=>q.UpdatedAt).ThenByDescending(q=>q.Id).Take(100).ToListAsync();
    }
    private IQueryable<TblQuery> Conversation(int id) => db.Queries.AsNoTracking().Where(q=>q.FarmerId==UserId && (q.Id==id || q.ParentQueryId==id))
        .Include(q=>q.AIResponse).Include(q=>q.Escalation).Include(q=>q.Feedbacks);
    private ChatMessageViewModel Message(TblQuery q) => new() {
        QueryId=q.Id, QueryText=q.QueryText, QueryType=q.QueryType ?? "Text",
        ImagePath=QueryMedia.Paths(q).Count==0?null:Url.Action("QueryImage","Media",new{id=q.Id}),
        ImageUrls=QueryMedia.Paths(q).Select((_,i)=>Url.Action("QueryImage","Media",new{id=q.Id,index=i})!).ToList(),
        AIAnswer=AdvisoryPolicy.IsPublished(q.AIResponse)?q.AIResponse!.AIAnswer:null,
        PublicSummary=q.AIResponse?.PublicSummary,
        Status=q.Status,CreatedDate=q.CreatedDate,OfficerReply=q.Escalation?.OfficerReply,
        IsEscalated=q.AIResponse?.IsEscalated??false,ApprovalStatus=q.AIResponse?.ApprovalStatus.ToString()??"Pending",
        FeedbackRating=q.Feedbacks.FirstOrDefault()?.Rating
    };
    public async Task<IActionResult> Index(int? conversationId = null, bool newChat = false) {
        if(!await OwnsConversation(conversationId)) return NotFound();
        var history=await History();
        if(newChat) conversationId=null; else conversationId ??= history.FirstOrDefault()?.Id;
        var queries=conversationId==null ? new List<TblQuery>() : await Conversation(conversationId.Value).OrderByDescending(q=>q.Id).Take(51).ToListAsync();
        return View(new ChatViewModel {ConversationId=conversationId,Conversations=history,HasOlderMessages=queries.Count>50,Messages=queries.Take(50).Reverse().Select(Message).ToList()});
    }
    [HttpGet] public async Task<IActionResult> Conversations(string? search=null) => Json(await History(search?.Length>200?search[..200]:search));
    [HttpGet] public async Task<IActionResult> Messages(int conversationId,int beforeId) {
        if(!await OwnsConversation(conversationId)) return NotFound();
        var queries=await Conversation(conversationId).Where(q=>q.Id<beforeId).OrderByDescending(q=>q.Id).Take(51).ToListAsync();
        return Json(new{messages=queries.Take(50).Reverse().Select(Message),hasOlder=queries.Count>50});
    }
    private IActionResult Saved(TblQuery q) => Json(new {
        success=true, queryId=q.Id,conversationId=q.ParentQueryId??q.Id,
        answer=AdvisoryPolicy.IsPublished(q.AIResponse)?q.AIResponse!.AIAnswer:null,
        status=q.Status,approvalStatus=q.AIResponse?.ApprovalStatus.ToString(),isEscalated=q.AIResponse?.IsEscalated??false,
        message=q.Status=="Answered"?"AI answered your question.":AdvisoryPolicy.PendingMessage(q.Status),entry=Message(q)
    });
    [HttpPost, EnableRateLimiting("advisory")]
    public async Task<IActionResult> Ask([FromBody] AskRequest? request) {
        if(request==null || string.IsNullOrWhiteSpace(request.QueryText) || request.QueryText.Length>4000) return BadRequest(new{message="Enter a question of 1–4000 characters."});
        if(!await OwnsConversation(request.ConversationId)) return NotFound();
        return Saved(await workflow.SubmitAsync(UserId,request.QueryText.Trim(),"Text",Language,conversationId:request.ConversationId));
    }
    [HttpPost, EnableRateLimiting("advisory"), RequestSizeLimit(27 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile? image, List<IFormFile>? images, string? userPrompt, string? cropName, int? conversationId) {
        if(!await OwnsConversation(conversationId)) return NotFound();
        var files=images?.ToList()??new List<IFormFile>();if(image!=null) files.Add(image);
        if(files.Count==0 || files.Count>ImageUploadService.MaximumImages || files.Sum(f=>f.Length)>ImageUploadService.MaximumBatchBytes || (userPrompt?.Length??0)>4000 || (cropName?.Length??0)>100)
            return BadRequest(new{message="Choose 1–20 photos, up to 5 MB each and 25 MB total, with a question under 4000 characters."});
        var stored=new List<StoredImage>();var saved=false;
        try {
            foreach(var file in files) stored.Add(await uploads.SaveAsync(file));
            var text=string.IsNullOrWhiteSpace(userPrompt)?$"Please assess these {files.Count} crop/soil photos together.":userPrompt.Trim();
            var q=await workflow.SubmitAsync(UserId,text,"Image",Language,conversationId:conversationId,photos:stored);saved=true;return Saved(q);
        } catch(ArgumentException ex) { return BadRequest(new{message=ex.Message}); }
        catch (HttpRequestException) { return StatusCode(503, new { message = "Photo storage is temporarily unavailable. Please try again." }); }
        catch (TaskCanceledException) { return StatusCode(503, new { message = "Photo upload timed out. Please try again." }); }
        finally { if(!saved) foreach(var photo in stored) {
            try { await uploads.DeleteAsync(photo.FileName); }
            catch (Exception ex) { logger.LogWarning("Could not clean up an unused photo: {FailureType}", ex.GetType().Name); }
        } }
    }
    [HttpPost, EnableRateLimiting("advisory")]
    public async Task<IActionResult> UploadVoice(string? transcribedText,int? conversationId) {
        if(string.IsNullOrWhiteSpace(transcribedText) || transcribedText.Length>4000) return BadRequest(new{message="Review the voice transcript before sending it."});
        if(!await OwnsConversation(conversationId)) return NotFound();
        return Saved(await workflow.SubmitAsync(UserId,transcribedText.Trim(),"Voice",Language,conversationId:conversationId));
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
public class AskRequest {
    public string QueryText { get; set; }="";
    public int? ConversationId { get; set; }
}

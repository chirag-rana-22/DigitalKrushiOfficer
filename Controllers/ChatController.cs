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
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAIService _ai;

        public ChatController(AppDbContext db, IAIService ai)
        {
            _db = db;
            _ai = ai;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string Language => User.FindFirstValue("Language") ?? "Gujarati";

        public async Task<IActionResult> Index()
        {
            var queries = await _db.Queries
                .Where(q => q.FarmerId == UserId)
                .Include(q => q.AIResponse)
                .Include(q => q.Escalation).ThenInclude(e => e!.Officer)
                .Include(q => q.Feedbacks)
                .OrderByDescending(q => q.CreatedDate)
                .Take(50).ToListAsync();

            var messages = queries
                .Select(q => new ChatMessageViewModel
                {
                    QueryId = q.Id,
                    QueryText = q.QueryText,
                    AIAnswer = q.AIResponse?.AIAnswer,
                    QueryType = q.QueryType ?? "Text",
                    ImagePath = q.ImagePath,
                    ConfidenceScore = q.AIResponse?.ConfidenceScore,
                    Status = q.Status,
                    CreatedDate = q.CreatedDate,
                    IsEscalated = q.AIResponse?.IsEscalated ?? false,
                    OfficerReply = q.Escalation?.OfficerReply,
                    FeedbackRating = q.Feedbacks.FirstOrDefault()?.Rating,
                    ComplexityLevel = q.AIResponse?.ComplexityLevel.ToString() ?? "Unknown",
                    ApprovalStatus = q.AIResponse?.ApprovalStatus.ToString() ?? "N/A"
                })
                .Reverse()   // oldest at top → newest at bottom (correct chat order)
                .ToList();

            return View(new ChatViewModel { Messages = messages });
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QueryText))
                return Json(new { success = false, message = "Please enter a question." });

            var query = new TblQuery
            {
                FarmerId = UserId,
                QueryText = request.QueryText,
                QueryType = "Text",
                QueryLanguage = Language,
                Status = "Pending",
                CreatedDate = DateTime.Now
            };
            _db.Queries.Add(query);
            await _db.SaveChangesAsync();

            var (answer, confidence, ms) = await _ai.GetAIResponseAsync(request.QueryText, Language);

            // ── Calculate complexity level ──
            var complexityLevel = ((AIService)_ai).CalculateComplexityLevel(request.QueryText, confidence);

            // ── Determine approval status based on complexity ──
            var approvalStatus = complexityLevel == ComplexityLevel.Complex
                ? ApprovalStatus.Pending
                : ApprovalStatus.Approved;

            var aiResponse = new TblAIResponse
            {
                QueryId = query.Id,
                AIAnswer = answer,
                ConfidenceScore = confidence,
                AIModel = "Gemini-Pro",
                ResponseTimeMs = ms,
                Language = Language,
                ComplexityLevel = complexityLevel,
                ApprovalStatus = approvalStatus,
                CreatedDate = DateTime.Now
            };
            _db.AIResponses.Add(aiResponse);

            // ── If complex, assign to Officer for review ──
            if (complexityLevel == ComplexityLevel.Complex)
            {
                var officer = await _db.Users.FirstOrDefaultAsync(u => u.Role == "Officer" && u.IsActive);
                if (officer != null)
                {
                    var escalation = new TblEscalation
                    {
                        QueryId = query.Id,
                        OfficerId = officer.Id,
                        Status = "Pending",
                        Priority = "High",
                        EscalatedByAI = true,
                        AssignedDate = DateTime.Now,
                        CreatedDate = DateTime.Now
                    };
                    _db.Escalations.Add(escalation);
                    query.Status = "PendingApproval";
                }
            }
            else
            {
                query.Status = "Answered";
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                queryId = query.Id,
                answer = answer,           // Always return the AI answer immediately
                confidence = (double)confidence,
                complexityLevel = complexityLevel.ToString(),
                approvalStatus = approvalStatus.ToString(),
                isEscalated = complexityLevel == ComplexityLevel.Complex,
                status = query.Status,
                message = "Answer ready!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image, string? userPrompt, string? cropName)
        {
            if (image == null || image.Length == 0)
                return Json(new { success = false, message = "No image uploaded." });

            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                return Json(new { success = false, message = "Invalid image format." });

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "crops");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            using (var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                await image.CopyToAsync(fs);
            var imagePath = $"/uploads/crops/{fileName}";

            // Build a rich query text combining the farmer's prompt + image context
            var prompt = !string.IsNullOrWhiteSpace(userPrompt)
                ? userPrompt
                : !string.IsNullOrWhiteSpace(cropName)
                    ? $"Analyze this {cropName} crop image and tell me what disease or problem is visible, and how to treat it."
                    : "Analyze this crop image and tell me what disease or problem is visible, and how to treat it.";

            // Run full Gemini AI on the prompt for a rich, detailed answer
            var (aiAnswer, confidence, ms) = await _ai.GetAIResponseAsync(prompt, Language);

            // Also get image-specific disease dataset match as supplementary info
            var imageAnalysis = await _ai.AnalyzeCropImageAsync(Path.Combine(folder, fileName), cropName ?? "");

            // Merge: if AI gave a good answer use it; otherwise fall back to image analysis
            var finalAnswer = !string.IsNullOrWhiteSpace(aiAnswer) && aiAnswer.Length > 100
                ? aiAnswer
                : imageAnalysis;

            var queryText = !string.IsNullOrWhiteSpace(userPrompt) ? userPrompt
                          : $"📷 Image analysis: {cropName ?? "crop"}";

            var query = new TblQuery
            {
                FarmerId = UserId,
                QueryText = queryText,
                QueryType = "Image",
                QueryLanguage = Language,
                ImagePath = imagePath,
                Status = "Answered",
                CreatedDate = DateTime.Now
            };
            _db.Queries.Add(query);
            await _db.SaveChangesAsync();

            _db.AIResponses.Add(new TblAIResponse
            {
                QueryId = query.Id,
                AIAnswer = finalAnswer,
                ConfidenceScore = confidence,
                AIModel = "Gemini-Vision",
                ResponseTimeMs = ms,
                Language = Language,
                CreatedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, queryId = query.Id, imagePath, analysis = finalAnswer, confidence = (double)confidence });
        }

        [HttpPost]
        public async Task<IActionResult> UploadVoice(IFormFile? audio, string? transcribedText)
        {
            var queryText = transcribedText ?? "Voice query submitted";
            var voicePath = "";
            if (audio != null && audio.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "voice");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}.webm";
                using (var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await audio.CopyToAsync(fs);
                voicePath = $"/uploads/voice/{fileName}";
            }

            var (answer, confidence, ms) = await _ai.GetAIResponseAsync(queryText, Language);
            var query = new TblQuery
            {
                FarmerId = UserId,
                QueryText = queryText,
                QueryType = "Voice",
                QueryLanguage = Language,
                VoicePath = voicePath,
                Status = "Answered",
                CreatedDate = DateTime.Now
            };
            _db.Queries.Add(query);
            await _db.SaveChangesAsync();

            _db.AIResponses.Add(new TblAIResponse
            {
                QueryId = query.Id,
                AIAnswer = answer,
                ConfidenceScore = confidence,
                AIModel = "Gemini-Pro",
                ResponseTimeMs = ms,
                Language = Language,
                CreatedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, queryId = query.Id, transcribedText = queryText, answer });
        }

        [HttpGet]
        public async Task<IActionResult> DiseaseDetection(string? search, string? crop)
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
    }

    public class AskRequest
    {
        public string QueryText { get; set; } = string.Empty;
    }
}

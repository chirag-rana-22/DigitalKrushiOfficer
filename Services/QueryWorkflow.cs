using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;
namespace Digital_Krushi_Officer.Services;
public class QueryWorkflow(AppDbContext db, IAIService ai)
{
    public async Task<TblQuery> SubmitAsync(int farmerId, string text, string kind, string language, byte[]? image = null, string? imagePath = null, int? conversationId = null, IReadOnlyList<StoredImage>? photos = null, int? farmerCropId = null) {
        if (conversationId != null && !await db.Queries.AnyAsync(q => q.Id == conversationId && q.FarmerId == farmerId && q.ParentQueryId == null))
            throw new ArgumentException("Conversation not found.", nameof(conversationId));
        var history = new List<AIConversationTurn>();
        string? cropContext = null;
        if (conversationId != null) {
            var root = await db.Queries.AsNoTracking().SingleAsync(q => q.Id == conversationId && q.FarmerId == farmerId);
            if (farmerCropId != null && farmerCropId != root.FarmerCropId) throw new ArgumentException("Start a new chat to switch crops.");
            farmerCropId = root.FarmerCropId;
            cropContext = root.CropContext;
        }
        if (farmerCropId != null) {
            var crop = await db.FarmerCrops.AsNoTracking().Include(c => c.Crop).FirstOrDefaultAsync(c => c.Id == farmerCropId && c.FarmerId == farmerId);
            if (crop == null && conversationId == null) throw new ArgumentException("Crop not found.");
            if (crop != null) {
                var completed = await db.CropTasks.AsNoTracking().Where(t => t.FarmerCropId == crop.Id && t.Status == "Done").OrderByDescending(t => t.CompletedAt).Take(3).Select(t => new { t.Title, t.CompletedAt }).ToListAsync();
                cropContext = System.Text.Json.JsonSerializer.Serialize(new { Crop = crop.DisplayName, crop.FieldName, crop.Variety, AreaAcres = crop.LandArea, crop.SowingDate, crop.ExpectedHarvestDate, crop.Status, CompletedTasks = completed }, new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            }
        }
        if (conversationId != null) {
            var previous = await db.Queries.AsNoTracking().Where(q => q.FarmerId == farmerId && (q.Id == conversationId || q.ParentQueryId == conversationId))
                .Include(q => q.AIResponse).Include(q => q.Escalation).OrderByDescending(q => q.Id).Take(6).ToListAsync();
            history = previous.AsEnumerable().Reverse().Select(q => new AIConversationTurn(q.QueryText ?? "", AdvisoryPolicy.IsPublished(q.AIResponse) ? q.AIResponse!.AIAnswer ?? "" : q.Escalation?.OfficerReply ?? AdvisoryPolicy.PendingMessage(q.Status), q.Status == "AwaitingDetails")).ToList();
        }
        if (photos?.Count > 0) { image=photos[0].Bytes; imagePath="private:"+photos[0].FileName; }
        var result = await ai.GenerateAsync(text, language, image, additionalImages: photos?.Skip(1).Select(p => p.Bytes).ToList(), history: history, cropContext: cropContext);
        var needsOfficer = !result.RetryableFailure && (!result.Available || result.RequiresOfficerReview || result.Complexity == ComplexityLevel.Complex);
        var query = new TblQuery { FarmerId = farmerId, QueryText = text, QueryType = kind, QueryLanguage = language, ImagePath = imagePath, ImagePathsJson = photos?.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(photos.Select(p => "private:"+p.FileName)) : null, ParentQueryId = conversationId, Status = result.RetryableFailure ? "RetryNeeded" : needsOfficer ? "PendingApproval" : result.NeedsClarification ? "AwaitingDetails" : "Answered" };
        query.FarmerCropId = farmerCropId; query.CropContext = cropContext;
        query.AIResponse = new TblAIResponse { AIAnswer = result.Answer, PublicSummary = needsOfficer ? result.PublicSummary : null, AIModel = result.Model, ResponseTimeMs = result.ResponseMs,
            ConfidenceScore = null, Language = language, ComplexityLevel = result.Complexity,
            ApprovalStatus = needsOfficer ? ApprovalStatus.Pending : ApprovalStatus.AIAnswered, IsEscalated = needsOfficer,
            EscalationReason = needsOfficer ? (result.ReviewReason ?? "AI could not reliably solve this question; officer assistance is needed.") : null };
        if (needsOfficer) {
            var officerId = await FindOfficerAsync(farmerId);
            if (officerId != null) query.Escalation = new TblEscalation { OfficerId = officerId.Value, Status = "Pending", Priority = result.Complexity == ComplexityLevel.Complex ? "High" : "Medium", EscalatedByAI = true, AssignedDate = DateTime.Now };
            else query.Status = "Unassigned";
        }
        db.Queries.Add(query); await db.SaveChangesAsync(); return query;
    }

    private async Task<int?> FindOfficerAsync(int farmerId) {
        var assigned = await db.FarmerOfficerAssignments.Where(a => a.FarmerId == farmerId && a.IsActive && a.Officer != null && a.Officer.Role == "Officer" && a.Officer.IsActive && a.Officer.IsEmailVerified)
            .OrderByDescending(a => a.CreatedDate).ThenBy(a => a.Id).Select(a => (int?)a.OfficerId).FirstOrDefaultAsync();
        if (assigned != null) return assigned;
        var district = await db.Users.Where(u => u.Id == farmerId).Select(u => u.District).SingleAsync();
        // Prefer local expertise, then the smallest open queue. Each assignment belongs to this query.
        return await db.Users.Where(u => u.Role == "Officer" && u.IsActive && u.IsEmailVerified)
            .OrderByDescending(u => district != null && district != "" && u.District == district)
            .ThenBy(u => db.Escalations.Count(e => e.OfficerId == u.Id && e.Status != "Solved" && e.Status != "Closed" && e.Status != "Rejected"))
            .ThenBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync();
    }

    public static void Publish(TblAIResponse response, int officerId, string? notes, string? answer = null) {
        if (answer != null) response.AIAnswer = answer.Trim();
        response.ApprovalStatus = answer == null ? ApprovalStatus.Approved : ApprovalStatus.Modified;
        response.ApprovedByOfficerId = officerId; response.ApprovedDate = DateTime.Now; response.OfficerNotes = notes;
        if (response.Query != null) {
            response.Query.Status = "Answered";
            if (response.Query.Escalation != null) { response.Query.Escalation.Status = "Solved"; response.Query.Escalation.ResolvedDate = DateTime.Now; }
        }
    }
}

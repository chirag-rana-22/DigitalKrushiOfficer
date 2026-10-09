using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;
namespace Digital_Krushi_Officer.Services;
public class QueryWorkflow(AppDbContext db, IAIService ai)
{
    public async Task<TblQuery> SubmitAsync(int farmerId, string text, string kind, string language, byte[]? image = null, string? imagePath = null) {
        var result = await ai.GenerateAsync(text, language, image);
        var needsOfficer = !result.Available || result.RequiresOfficerReview || result.Complexity == ComplexityLevel.Complex;
        var query = new TblQuery { FarmerId = farmerId, QueryText = text, QueryType = kind, QueryLanguage = language, ImagePath = imagePath, Status = needsOfficer ? "PendingApproval" : "Answered" };
        query.AIResponse = new TblAIResponse { AIAnswer = result.Answer, AIModel = result.Model, ResponseTimeMs = result.ResponseMs,
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
            .ThenBy(u => db.Escalations.Count(e => e.OfficerId == u.Id && e.Status != "Solved" && e.Status != "Closed"))
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

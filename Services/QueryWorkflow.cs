using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;
namespace Digital_Krushi_Officer.Services;
public class QueryWorkflow(AppDbContext db, IAIService ai)
{
    public async Task<TblQuery> SubmitAsync(int farmerId, string text, string kind, string language, byte[]? image = null, string? imagePath = null) {
        var result = await ai.GenerateAsync(text, language, image);
        var query = new TblQuery { FarmerId = farmerId, QueryText = text, QueryType = kind, QueryLanguage = language, ImagePath = imagePath, Status = "PendingApproval" };
        query.AIResponse = new TblAIResponse { AIAnswer = result.Answer, AIModel = result.Model, ResponseTimeMs = result.ResponseMs,
            ConfidenceScore = null, Language = language, ComplexityLevel = ComplexityLevel.Complex, ApprovalStatus = ApprovalStatus.Pending, IsEscalated = true,
            EscalationReason = result.Available ? "Agricultural advice requires officer review." : "AI unavailable; officer response required." };
        var officerId = await db.FarmerOfficerAssignments.Where(a => a.FarmerId == farmerId && a.IsActive && a.Officer != null && a.Officer.IsActive && a.Officer.IsEmailVerified)
            .OrderByDescending(a => a.CreatedDate).Select(a => (int?)a.OfficerId).FirstOrDefaultAsync();
        if (officerId != null) query.Escalation = new TblEscalation { OfficerId = officerId.Value, Status = "Pending", Priority = "Medium", EscalatedByAI = true, AssignedDate = DateTime.Now };
        else query.Status = "Unassigned";
        db.Queries.Add(query); await db.SaveChangesAsync(); return query;
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

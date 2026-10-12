using Digital_Krushi_Officer.Models;

namespace Digital_Krushi_Officer.Services;

public static class AdvisoryPolicy
{
    public static bool IsPublished(TblAIResponse? response) => response is not null &&
        response.ApprovalStatus is ApprovalStatus.Approved or ApprovalStatus.Modified or ApprovalStatus.AIAnswered;

    public static string PendingMessage(string status) => status == "Rejected"
        ? "The officer could not solve this query and has closed it as rejected. Read their reason below; you can send a follow-up with more details."
        : status == "Unassigned"
        ? "AI could not solve this reliably. No active verified officer is available right now; your question is saved in the assignment queue."
        : status == "NeedsRevision"
            ? "The draft needs revision. Your officer is preparing a corrected answer."
            : "AI could not solve this reliably, so your question was automatically sent to a Krushi Officer. Their answer will appear here.";
}

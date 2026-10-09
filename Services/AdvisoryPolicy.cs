using Digital_Krushi_Officer.Models;

namespace Digital_Krushi_Officer.Services;

public static class AdvisoryPolicy
{
    public static bool IsPublished(TblAIResponse? response) => response is not null &&
        response.ApprovalStatus is ApprovalStatus.Approved or ApprovalStatus.Modified;

    public static string PendingMessage(string status) => status == "Unassigned"
        ? "Your question is saved. An administrator will assign an officer to review it."
        : status == "NeedsRevision"
            ? "The draft needs revision. Your officer is preparing a corrected answer."
            : "Your question is saved for officer review. The reviewed answer will appear here.";
}

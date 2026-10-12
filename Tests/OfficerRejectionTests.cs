using System.Net;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
public class OfficerRejectionTests {
    [Fact] public async Task RejectClosesAssignedQueryAndShowsReasonWithoutPublishingDraft() {
        using var app=new TestApp();await app.Seed();using var farmer=await app.Login();await app.Ask(farmer);using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var esc=await db.Escalations.Include(e=>e.Query).ThenInclude(q=>q!.AIResponse).SingleAsync();using var officer=await app.Login("officer@test.local");
        var values=new Dictionary<string,string>{{"EscalationId",esc.Id.ToString()},{"Status","Rejected"},{"OfficerReply","This requires a laboratory inspection unavailable here."}};
        using var other=await app.Login("other@test.local");Assert.Equal(HttpStatusCode.NotFound,(await app.Post(other,"/Officer/Reply",values)).StatusCode);
        var result=await app.Post(officer,"/Officer/Reply",values);Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);await db.Entry(esc).ReloadAsync();await db.Entry(esc.Query!).ReloadAsync();await db.Entry(esc.Query!.AIResponse!).ReloadAsync();Assert.Equal("Rejected",esc.Status);Assert.Equal("Rejected",esc.Query.Status);Assert.Equal(ApprovalStatus.Rejected,esc.Query.AIResponse!.ApprovalStatus);Assert.NotNull(esc.ResolvedDate);
        var chat=await farmer.GetStringAsync("/Chat");Assert.Contains("closed it as rejected",chat);Assert.Contains(values["OfficerReply"],chat);Assert.DoesNotContain("draft advice",chat);Assert.Contains(values["OfficerReply"],await farmer.GetStringAsync("/Farmer/OfficerReplies"));Assert.Contains("status-rejected\">Rejected",await farmer.GetStringAsync("/Farmer/OfficerReplies"));Assert.DoesNotContain("draft advice",await officer.GetStringAsync("/Officer/PendingReviews"));Assert.Contains("Rejected",await officer.GetStringAsync("/Officer/Escalations?status=Rejected"));
        Assert.Equal(HttpStatusCode.NotFound,(await app.Post(officer,"/Officer/ModifyAndApprove",new(){{"id",esc.Query.AIResponse.Id.ToString()},{"modifiedAnswer","Stale correction"}})).StatusCode);
        values["Status"]="Solved";await app.Post(officer,"/Officer/Reply",values);await db.Entry(esc).ReloadAsync();Assert.Equal("Rejected",esc.Status);
    }
    [Theory] [InlineData("Rejected","")] [InlineData("Invalid","Keep this draft")]
    public async Task InvalidReplyReturnsFormAndLeavesQueryPending(string status,string reply) {
        using var app=new TestApp();await app.Seed();using var farmer=await app.Login();await app.Ask(farmer);using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var esc=await db.Escalations.SingleAsync();using var officer=await app.Login("officer@test.local");
        var r=await app.Post(officer,"/Officer/Reply",new(){{"EscalationId",esc.Id.ToString()},{"Status",status},{"OfficerReply",reply}});Assert.Equal(HttpStatusCode.OK,r.StatusCode);var html=await r.Content.ReadAsStringAsync();Assert.Contains("validation-summary-errors",html);Assert.Contains("Your Expert Reply",html);if(reply.Length>0)Assert.Contains(reply,html);await db.Entry(esc).ReloadAsync();Assert.Equal("Pending",esc.Status);Assert.Null(esc.OfficerReply);
    }
    [Theory] [InlineData("Solved","Answered")] [InlineData("InProgress","Escalated")]
    public async Task ExistingReplyStatusesStillWork(string status,string queryStatus) {
        using var app=new TestApp();await app.Seed();using var farmer=await app.Login();await app.Ask(farmer);using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var esc=await db.Escalations.Include(e=>e.Query).SingleAsync();using var officer=await app.Login("officer@test.local");Assert.Equal(HttpStatusCode.Redirect,(await app.Post(officer,"/Officer/Reply",new(){{"EscalationId",esc.Id.ToString()},{"Status",status},{"OfficerReply","Arrange a field inspection."}})).StatusCode);await db.Entry(esc).ReloadAsync();await db.Entry(esc.Query!).ReloadAsync();Assert.Equal(status,esc.Status);Assert.Equal(queryStatus,esc.Query!.Status);
    }
}

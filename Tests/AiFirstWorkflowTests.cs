using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AiFirstWorkflowTests {
    [Theory] [InlineData("Text")] [InlineData("Voice")] [InlineData("Image")]
    public async Task RoutineAnswerIsImmediateWithoutAnyOfficer(string kind) {
        using var app=new TestApp();await app.Seed();app.Ai.Result=new("Routine AI advice <script>unsafe()</script>",true,"Fake",5,false,null,ComplexityLevel.Simple);
        using var farmer=await app.Login();
        using(var s=app.Services.CreateScope())await s.ServiceProvider.GetRequiredService<AppDbContext>().Users.Where(u=>u.Role=="Officer").ExecuteUpdateAsync(x=>x.SetProperty(u=>u.IsActive,false));
        int id;
        using(var s=app.Services.CreateScope()) {
            var db=s.ServiceProvider.GetRequiredService<AppDbContext>();var workflow=s.ServiceProvider.GetRequiredService<QueryWorkflow>();
            var q=await workflow.SubmitAsync(10,"Explain composting",kind,"English",kind=="Image"?new byte[]{1,2}:null);
            id=q.Id;Assert.Equal("Answered",q.Status);Assert.Null(q.Escalation);Assert.False(q.AIResponse!.IsEscalated);Assert.Equal(ApprovalStatus.AIAnswered,q.AIResponse.ApprovalStatus);Assert.Null(q.AIResponse.ApprovedByOfficerId);Assert.Empty(await db.Escalations.ToListAsync());
        }
        var html=await farmer.GetStringAsync("/Chat");Assert.Contains("Routine AI advice",html);Assert.DoesNotContain("<script>unsafe",html);Assert.Contains("AI answer",html);
        Assert.DoesNotContain("Routine AI advice",await farmer.GetStringAsync("/Farmer/OfficerReplies"));
        Assert.Equal(HttpStatusCode.OK,(await app.Post(farmer,"/Farmer/SubmitFeedback",new(){{"queryId",id.ToString()},{"rating","5"}})).StatusCode);
    }
    [Fact] public async Task AskReturnsAnswerAndDoesNotCreateOfficerTask() {
        using var app=new TestApp();await app.Seed();app.Ai.Result=new("Water based on soil moisture.",true,"Fake",5,false);
        using var farmer=await app.Login();var r=await app.Ask(farmer);using var json=JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal("Water based on soil moisture.",json.RootElement.GetProperty("answer").GetString());Assert.False(json.RootElement.GetProperty("isEscalated").GetBoolean());Assert.Equal("AIAnswered",json.RootElement.GetProperty("approvalStatus").GetString());
        using var officer=await app.Login("officer@test.local");Assert.DoesNotContain("Water based on soil moisture.",await officer.GetStringAsync("/Officer/PendingReviews"));
    }
    [Theory] [InlineData(false,false,ComplexityLevel.Simple)] [InlineData(true,true,ComplexityLevel.Simple)] [InlineData(true,false,ComplexityLevel.Complex)]
    public async Task FailureUncertaintyAndComplexityRequireReview(bool available,bool flag,ComplexityLevel complexity) {
        using var app=new TestApp();await app.Seed();app.Ai.Result=new("Private uncertain assessment",available,available?"Fake":"Unavailable",5,flag,"Needs investigation",complexity);
        using var farmer=await app.Login();var response=await app.Ask(farmer);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Equal(JsonValueKind.Null,json.RootElement.GetProperty("answer").ValueKind);Assert.True(json.RootElement.GetProperty("isEscalated").GetBoolean());
        using var s=app.Services.CreateScope();var q=await s.ServiceProvider.GetRequiredService<AppDbContext>().Queries.Include(x=>x.Escalation).SingleAsync();Assert.Equal(11,q.Escalation!.OfficerId);Assert.Equal("PendingApproval",q.Status);
    }
    [Fact] public async Task UnassignedFarmerAutomaticallyGetsLocalVerifiedOfficer() {
        using var app=new TestApp();await app.Seed();
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();(await db.Users.FindAsync(13))!.District="Junagadh";(await db.Users.FindAsync(12))!.District="Junagadh";await db.SaveChangesAsync();}
        using var farmer=await app.Login("otherfarmer@test.local");await app.Ask(farmer);
        using var scope=app.Services.CreateScope();var q=await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.Include(x=>x.Escalation).SingleAsync();Assert.Equal(12,q.Escalation!.OfficerId);
    }
    [Fact] public async Task AutomaticFallbackBalancesQueuesAndSkipsUnavailableOfficers() {
        using var app=new TestApp();await app.Seed();using var assigned=await app.Login();await app.Ask(assigned);
        using var farmer=await app.Login("otherfarmer@test.local");await app.Ask(farmer);
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Equal(12,(await db.Queries.Include(q=>q.Escalation).SingleAsync(q=>q.FarmerId==13)).Escalation!.OfficerId);(await db.Users.FindAsync(11))!.IsActive=false;(await db.Users.FindAsync(12))!.IsEmailVerified=false;await db.SaveChangesAsync();}
        await app.Ask(farmer);
        using var scope=app.Services.CreateScope();var last=await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.Include(q=>q.Escalation).OrderByDescending(q=>q.Id).FirstAsync();Assert.Equal("Unassigned",last.Status);Assert.Null(last.Escalation);
    }
    [Fact] public async Task InactiveExistingAssignmentFallsBackAutomatically() {
        using var app=new TestApp();await app.Seed();using(var s=app.Services.CreateScope())await s.ServiceProvider.GetRequiredService<AppDbContext>().Users.Where(u=>u.Id==11).ExecuteUpdateAsync(x=>x.SetProperty(u=>u.IsActive,false));
        using var farmer=await app.Login();await app.Ask(farmer);using var scope=app.Services.CreateScope();Assert.Equal(12,(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Escalations.SingleAsync()).OfficerId);
    }
    [Theory]
    [InlineData(false,false,"Simple","NotProvided",false,false)]
    [InlineData(false,false,"Simple","Clear",true,false)]
    [InlineData(false,true,"Simple","Clear",true,true)]
    [InlineData(false,false,"Simple","Unclear",true,true)]
    [InlineData(false,false,"Simple","NotCrop",true,true)]
    [InlineData(false,false,"Simple","NotProvided",true,true)]
    [InlineData(false,false,"Complex","NotProvided",false,true)]
    [InlineData(true,false,"Medium","NotProvided",false,true)]
    public async Task StructuredProviderAssessmentControlsRouting(bool flag,bool uncertain,string complexity,string imageAssessment,bool image,bool expectedReview) {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();
        var assessment=JsonSerializer.Serialize(new {answer="Observed crop conditions.",requiresOfficer=flag,uncertain,reason="",complexity,imageAssessment});
        var body=JsonSerializer.Serialize(new{candidates=new[]{new{finishReason="STOP",content=new{parts=new[]{new{text=assessment}}}}}});
        var handler=new ProviderHandler(body);var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AISettings:GeminiApiKey","test"}}).Build();
        var ai=new AIService(config,NullLogger<AIService>.Instance,new ClientFactory(handler),scope.ServiceProvider.GetRequiredService<AppDbContext>());
        var result=await ai.GenerateAsync("test","English",image?new byte[]{1}:null);Assert.True(result.Available);Assert.Equal(expectedReview,result.RequiresOfficerReview);
        using var request=JsonDocument.Parse(handler.RequestBody!);Assert.Equal("application/json",request.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
    }
    [Theory] [InlineData("{}","STOP")] [InlineData("plain unstructured answer","STOP")] [InlineData("{}","MAX_TOKENS")]
    public async Task MalformedOrTruncatedProviderOutputIsNotPublished(string assessment,string finishReason) {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();
        var body=JsonSerializer.Serialize(new{candidates=new[]{new{finishReason,content=new{parts=new[]{new{text=assessment}}}}}});
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AISettings:GeminiApiKey","test"}}).Build();var ai=new AIService(config,NullLogger<AIService>.Instance,new ClientFactory(new ProviderHandler(body)),scope.ServiceProvider.GetRequiredService<AppDbContext>());
        var result=await ai.GenerateAsync("test","English");Assert.False(result.Available);Assert.False(result.RequiresOfficerReview);Assert.True(result.RetryableFailure);
    }
}

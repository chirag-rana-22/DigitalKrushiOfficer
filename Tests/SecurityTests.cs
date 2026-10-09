using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class TestApp : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public readonly MailFake Mail = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        connection.Open(); builder.UseEnvironment("Testing");
        builder.ConfigureServices(s => {
            s.RemoveAll<DbContextOptions<AppDbContext>>(); s.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            s.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
            s.RemoveAll<IAIService>(); s.AddSingleton<IAIService>(new AiFake());
            s.RemoveAll<IEmailService>(); s.AddSingleton<IEmailService>(Mail);
            s.RemoveAll<IWeatherService>(); s.AddSingleton<IWeatherService>(new WeatherFake());
        });
    }
    public async Task Seed() {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var password=BCrypt.Net.BCrypt.HashPassword("TestPassword!12",4);
        db.Users.AddRange(new TblUser {Id=10,Name="Farmer",Email="farmer@test.local",PasswordHash=password,Role="Farmer",IsActive=true,IsEmailVerified=true},
            new TblUser {Id=11,Name="Assigned Officer",Email="officer@test.local",PasswordHash=password,Role="Officer",IsActive=true,IsEmailVerified=true},
            new TblUser {Id=12,Name="Other Officer",Email="other@test.local",PasswordHash=password,Role="Officer",IsActive=true,IsEmailVerified=true},
            new TblUser {Id=13,Name="Other Farmer",Email="otherfarmer@test.local",PasswordHash=password,Role="Farmer",IsActive=true,IsEmailVerified=true});
        db.FarmerOfficerAssignments.Add(new TblFarmerOfficerAssignment {FarmerId=10,OfficerId=11,IsActive=true}); await db.SaveChangesAsync();
    }
    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions {AllowAutoRedirect=false});
    public async Task<string> Token(HttpClient client,string path="/Account/Login") {
        var html=await client.GetStringAsync(path);
        return WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    public async Task<HttpResponseMessage> Post(HttpClient client,string path,Dictionary<string,string> values,string tokenPath="/Account/Login") {
        values["__RequestVerificationToken"]=await Token(client,tokenPath);
        return await client.PostAsync(path,new FormUrlEncodedContent(values));
    }
    public async Task<HttpClient> Login(string email="farmer@test.local") {
        var client=Client(); var response=await Post(client,"/Account/Login",new(){{"Email",email},{"Password",email.StartsWith("admin")?"Admin@123":"TestPassword!12"}});
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode); return client;
    }
    public async Task<HttpResponseMessage> Ask(HttpClient client) {
        var request=new HttpRequestMessage(HttpMethod.Post,"/Chat/Ask") {Content=JsonContent.Create(new {queryText="My crop leaves are yellow. Please review."})};
        request.Headers.Add("RequestVerificationToken",await Token(client,"/Chat")); return await client.SendAsync(request);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if(disposing) connection.Dispose(); }
}
public class AiFake : IAIService {
    public Task<AIResult> GenerateAsync(string q,string l,byte[]? image=null,CancellationToken ct=default) => Task.FromResult(new AIResult("<script>alert('unsafe')</script> draft advice",true,"Fake",5));
}
public class WeatherFake : IWeatherService { public Task<WeatherViewModel?> GetWeatherAsync(string d,string v="") => Task.FromResult<WeatherViewModel?>(null); }
public class MailFake : IEmailService {
    public Dictionary<string,string> Codes = new();
    public Task<string?> SendEmailAsync(string a,string n,string s,string b)=>Task.FromResult<string?>(null);
    public Task<string?> SendOtpEmailAsync(string a,string n,string code){ Codes[a]=code; return Task.FromResult<string?>(null); }
    public Task<string?> SendPasswordResetEmailAsync(string a,string n,string code){ Codes[a]=code; return Task.FromResult<string?>(null); }
    public Task SendWelcomeEmailAsync(string a,string n,string r)=>Task.CompletedTask;
    public Task SendEscalationNotificationAsync(string a,string n,string f,string q)=>Task.CompletedTask;
    public Task SendOfficerReplyNotificationAsync(string a,string n,string o,string r)=>Task.CompletedTask;
}
public class SecurityTests
{
    [Fact] public async Task ForgedPostIsRejected() {
        using var app=new TestApp(); await app.Seed(); using var c=app.Client();
        var r=await c.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","admin@digitalkrushi.com"},{"Password","Admin@123"}}));
        Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);
    }
    [Theory] [InlineData("Admin")] [InlineData("SuperAdmin")]
    public async Task PublicRegistrationCannotElevateRole(string role) {
        using var app=new TestApp(); await app.Seed(); using var c=app.Client();
        var r=await app.Post(c,"/Account/Register",new(){{"Name","Attacker"},{"Email","attempt@test.local"},{"Mobile","9999999999"},{"Password","TestPassword!12"},{"ConfirmPassword","TestPassword!12"},{"Role",role},{"PreferredLanguage","Gujarati"}});
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        using var scope=app.Services.CreateScope(); Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AnyAsync(u=>u.Email=="attempt@test.local"));
    }
    [Fact] public async Task NewOfficersNeedVerificationAndAdminApproval() {
        using var app=new TestApp(); await app.Seed(); using var c=app.Client();
        var r=await app.Post(c,"/Account/Register",new(){{"Name","New Officer"},{"Email","new@test.local"},{"Mobile","9999999999"},{"Password","TestPassword!12"},{"ConfirmPassword","TestPassword!12"},{"Role","Officer"},{"PreferredLanguage","Gujarati"}});
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.True(app.Mail.Codes.ContainsKey("new@test.local"));
        using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); var user=await db.Users.SingleAsync(u=>u.Email=="new@test.local");
        Assert.False(user.IsActive); Assert.False(user.IsEmailVerified);
        r=await app.Post(c,"/Account/VerifyOtp",new(){{"Email",user.Email},{"OTPCode",app.Mail.Codes[user.Email]}});
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); await db.Entry(user).ReloadAsync(); Assert.True(user.IsEmailVerified); Assert.False(user.IsActive);
    }
    [Fact] public async Task DraftStaysPrivateAndAssignedOfficerOwnsReview() {
        using var app=new TestApp(); await app.Seed(); using var farmer=await app.Login();
        var response=await app.Ask(farmer); var json=await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.OK,response.StatusCode); Assert.DoesNotContain("draft advice",json);
        Assert.DoesNotContain("draft advice",await farmer.GetStringAsync("/Chat")); Assert.DoesNotContain("draft advice",await farmer.GetStringAsync("/Farmer/QueryHistory"));
        int id;
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var query=await db.Queries.Include(q=>q.AIResponse).Include(q=>q.Escalation).SingleAsync();Assert.Equal(11,query.Escalation!.OfficerId);id=query.AIResponse!.Id;Assert.Null(query.AIResponse.ConfidenceScore);}
        using var other=await app.Login("other@test.local"); Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/Officer/ReviewResponse/"+id)).StatusCode);
        var denied=await app.Post(other,"/Officer/ApproveResponse",new(){{"id",id.ToString()}}); Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);
        using var officer=await app.Login("officer@test.local"); var approved=await app.Post(officer,"/Officer/ApproveResponse",new(){{"id",id.ToString()}}); Assert.Equal(HttpStatusCode.Redirect,approved.StatusCode);
        var page=await farmer.GetStringAsync("/Chat"); Assert.Contains("draft advice",page); Assert.DoesNotContain("<script>alert",page); Assert.Contains("&lt;script&gt;",page);
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Equal("Solved",(await db.Escalations.SingleAsync()).Status);Assert.Equal("Answered",(await db.Queries.SingleAsync()).Status);}
    }
    [Fact] public async Task UnassignedQueryCanBeRoutedByAdmin() {
        using var app=new TestApp(); await app.Seed(); using var farmer=await app.Login("otherfarmer@test.local"); await app.Ask(farmer);
        int id; using(var scope=app.Services.CreateScope()){var q=await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.SingleAsync();Assert.Equal("Unassigned",q.Status);id=q.Id;}
        using var admin=await app.Login("admin@digitalkrushi.com"); var r=await app.Post(admin,"/Admin/AssignQuery",new(){{"queryId",id.ToString()},{"officerId","11"}}); Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        using var officer=await app.Login("officer@test.local"); Assert.Contains("draft advice",await officer.GetStringAsync("/Officer/PendingReviews"));
    }
    [Fact] public async Task RejectedDraftCanBeCorrectedAndPublished() {
        using var app=new TestApp(); await app.Seed(); using var farmer=await app.Login(); await app.Ask(farmer);
        int id; using(var scope=app.Services.CreateScope()){id=(await scope.ServiceProvider.GetRequiredService<AppDbContext>().AIResponses.SingleAsync()).Id;}
        using var officer=await app.Login("officer@test.local");
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(officer,"/Officer/RejectResponse",new(){{"id",id.ToString()},{"rejectionReason","Needs local inspection"}})).StatusCode);
        Assert.DoesNotContain("draft advice",await farmer.GetStringAsync("/Chat"));
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(officer,"/Officer/ModifyAndApprove",new(){{"id",id.ToString()},{"modifiedAnswer","Arrange an in-person crop inspection."}})).StatusCode);
        Assert.Contains("Arrange an in-person crop inspection.",await farmer.GetStringAsync("/Chat"));
    }
    [Fact] public async Task DisabledAccountLosesExistingSession() {
        using var app=new TestApp(); await app.Seed(); using var c=await app.Login();
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();(await db.Users.FindAsync(10))!.IsActive=false;await db.SaveChangesAsync();}
        Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync("/Chat")).StatusCode);
    }
    [Fact] public async Task InvalidFeedbackAndVoiceAreRejected() {
        using var app=new TestApp(); await app.Seed(); using var c=await app.Login();
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(c,"/Farmer/SubmitFeedback",new(){{"queryId","1"},{"rating","99"}})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(c,"/Chat/UploadVoice",new(){{"transcribedText",""}})).StatusCode);
    }
    [Fact] public async Task OtpAttemptLimitAndSingleUseAreEnforced() {
        using var app=new TestApp(); await app.Seed();
        using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); var otp=scope.ServiceProvider.GetRequiredService<OtpService>(); var user=(await db.Users.FindAsync(10))!;
        Assert.True(await otp.SendAsync(user,"reset"));var code=app.Mail.Codes[user.Email];
        for(var i=0;i<5;i++)Assert.False(await otp.VerifyAsync(user.Email,"reset","000000"));
        Assert.False(await otp.VerifyAsync(user.Email,"reset",code));
        Assert.True(await otp.SendAsync(user,"verify"));code=app.Mail.Codes[user.Email];
        Assert.True(await otp.VerifyAsync(user.Email,"verify",code));Assert.False(await otp.VerifyAsync(user.Email,"verify",code));
    }
    [Theory] [InlineData("/Admin")] [InlineData("/Officer")] [InlineData("/Farmer")] [InlineData("/Chat")] [InlineData("/Media/QueryImage/1")]
    public async Task PrivateRoutesRequireLogin(string route){using var app=new TestApp();await app.Seed();using var c=app.Client();Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync(route)).StatusCode);}
    [Fact] public async Task WeatherFailureIsVisibleNotMocked(){using var app=new TestApp();await app.Seed();using var c=await app.Login();var page=await c.GetStringAsync("/Weather");Assert.Contains("Forecast unavailable",page);Assert.DoesNotContain("32°C",page);}
}

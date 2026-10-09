using System.Net;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
public class FinalRegressionTests {
    [Fact] public async Task PasswordChangeInvalidatesExistingCookies() {
        using var app=new TestApp();await app.Seed();using var first=await app.Login();using var second=await app.Login();
        var response=await app.Post(first,"/Account/ChangePassword",new(){{"CurrentPassword","TestPassword!12"},{"NewPassword","NewSecurePassword!42"},{"ConfirmPassword","NewSecurePassword!42"}},"/Account/ChangePassword");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal(HttpStatusCode.Redirect,(await second.GetAsync("/Chat")).StatusCode);
        using var again=app.Client();var login=await app.Post(again,"/Account/Login",new(){{"Email","farmer@test.local"},{"Password","NewSecurePassword!42"}});Assert.Equal(HttpStatusCode.Redirect,login.StatusCode);
    }
    [Fact] public async Task OtpExpiresAndResendHasCooldown() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var otp=scope.ServiceProvider.GetRequiredService<OtpService>();var user=(await db.Users.FindAsync(10))!;
        Assert.True(await otp.SendAsync(user,"reset"));Assert.False(await otp.SendAsync(user,"reset"));var code=app.Mail.Codes[user.Email];
        await db.Set<Digital_Krushi_Officer.Models.OtpChallenge>().ExecuteUpdateAsync(s=>s.SetProperty(c=>c.ExpiresUtc,DateTime.UtcNow.AddMinutes(-1)));
        Assert.False(await otp.VerifyAsync(user.Email,"reset",code));
    }
    [Fact] public async Task AdminCannotToggleAnAdministratorThroughFarmerEndpoint() {
        using var app=new TestApp();await app.Seed();using var admin=await app.Login("admin@digitalkrushi.com");await app.Post(admin,"/Admin/ToggleFarmerStatus",new(){{"id","1"}});
        using var scope=app.Services.CreateScope();Assert.True((await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.FindAsync(1))!.IsActive);
    }
    [Theory] [InlineData("/Farmer/Crops")] [InlineData("/Farmer/Profile")] [InlineData("/Farmer/EditProfile")] [InlineData("/Farmer/OfficerReplies")] [InlineData("/Chat/DiseaseDetection")] [InlineData("/Scheme")]
    public async Task FarmerScreensRender(string route){using var app=new TestApp();await app.Seed();using var c=await app.Login();Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(route)).StatusCode);}
    [Theory] [InlineData("/Admin/ReviewQueue")] [InlineData("/Admin/Officers")] [InlineData("/Admin/AssignFarmerToOfficer")] [InlineData("/Admin/Analytics")]
    public async Task AdminScreensRender(string route){using var app=new TestApp();await app.Seed();using var c=await app.Login("admin@digitalkrushi.com");Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(route)).StatusCode);}
}

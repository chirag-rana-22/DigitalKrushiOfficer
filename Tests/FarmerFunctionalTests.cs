using System.Net;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class FarmerFunctionalTests {
    private static async Task<int> Crop(TestApp app) {
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crop=new TblCrop {CropName="QA Cotton",Season="Kharif"};db.Crops.Add(crop);await db.SaveChangesAsync();return crop.Id;
    }
    [Fact] public async Task CropLifecycleAndOwnership() {
        using var app=new TestApp();await app.Seed();using var c=await app.Login();var cropId=await Crop(app);
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(c,"/Farmer/AddCrop",new(){{"cropId",cropId.ToString()},{"landArea","2.5"},{"sowingDate","2026-06-01"},{"harvestDate","2026-10-01"}})).StatusCode);
        int id;using(var s=app.Services.CreateScope()){var crop=await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerCrops.SingleAsync();id=crop.Id;Assert.Equal(2.5m,crop.LandArea);Assert.Equal("Growing",crop.Status);}
        using var other=await app.Login("otherfarmer@test.local");
        await app.Post(other,"/Farmer/UpdateCropStatus",new(){{"id",id.ToString()},{"status","Harvested"}});
        await app.Post(other,"/Farmer/DeleteCrop",new(){{"id",id.ToString()}});
        using(var s=app.Services.CreateScope())Assert.Equal("Growing",(await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerCrops.SingleAsync()).Status);
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(c,"/Farmer/UpdateCropStatus",new(){{"id",id.ToString()},{"status","Invalid"}})).StatusCode);
        await app.Post(c,"/Farmer/UpdateCropStatus",new(){{"id",id.ToString()},{"status","Harvested"}});
        using(var s=app.Services.CreateScope())Assert.Equal("Harvested",(await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerCrops.SingleAsync()).Status);
        await app.Post(c,"/Farmer/DeleteCrop",new(){{"id",id.ToString()}});
        using(var s=app.Services.CreateScope())Assert.Empty(await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerCrops.ToListAsync());
    }
    [Theory] [InlineData("-1","2026-06-01","2026-10-01")] [InlineData("abc","2026-06-01","2026-10-01")] [InlineData("2","bad-date","2026-10-01")] [InlineData("2","2026-10-01","2026-06-01")]
    public async Task InvalidCropInputsNeverCreateRecords(string area,string sowing,string harvest){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();var id=await Crop(app);
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(c,"/Farmer/AddCrop",new(){{"cropId",id.ToString()},{"landArea",area},{"sowingDate",sowing},{"harvestDate",harvest}})).StatusCode);
        using var s=app.Services.CreateScope();Assert.Empty(await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerCrops.ToListAsync());
    }
    [Fact] public async Task ProfileSavesAndInvalidValuesHaveVisibleErrors(){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();
        var valid=new Dictionary<string,string>{{"Name","Updated Farmer"},{"Mobile","9999999999"},{"State","Gujarat"},{"District","Ahmedabad"},{"Village","Ahmedabad"},{"PreferredLanguage","Hindi"},{"LandArea","2.5"},{"SoilType","Black"},{"MainCrop","Cotton"},{"FarmingExperience","5"}};
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(c,"/Farmer/EditProfile",valid)).StatusCode);
        Assert.Contains("Updated Farmer",await c.GetStringAsync("/Farmer/Profile"));
        valid["LandArea"]="-1";var invalid=await app.Post(c,"/Farmer/EditProfile",valid);var html=await invalid.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK,invalid.StatusCode);Assert.Contains("validation-summary-errors",html);Assert.Contains("must be between",html);
        using var s=app.Services.CreateScope();Assert.Equal(2.5m,(await s.ServiceProvider.GetRequiredService<AppDbContext>().FarmerDetails.SingleAsync()).LandArea);
    }
    [Fact] public async Task FeedbackPersistsUpdatesAndRejectsOtherFarmers(){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();await app.Ask(c);int qid,rid;
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();qid=(await db.Queries.SingleAsync()).Id;rid=(await db.AIResponses.SingleAsync()).Id;}
        using var officer=await app.Login("officer@test.local");await app.Post(officer,"/Officer/ModifyAndApprove",new(){{"id",rid.ToString()},{"modifiedAnswer","QA officer reviewed answer"}});
        Assert.Contains("QA officer reviewed answer",await c.GetStringAsync("/Farmer/OfficerReplies"));
        using var other=await app.Login("otherfarmer@test.local");Assert.Equal(HttpStatusCode.NotFound,(await app.Post(other,"/Farmer/SubmitFeedback",new(){{"queryId",qid.ToString()},{"rating","5"}})).StatusCode);
        foreach(var rating in new[]{"5","3"})Assert.Equal(HttpStatusCode.OK,(await app.Post(c,"/Farmer/SubmitFeedback",new(){{"queryId",qid.ToString()},{"rating",rating}})).StatusCode);
        using var scope=app.Services.CreateScope();Assert.Equal(3,(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Feedbacks.SingleAsync()).Rating);
    }
    [Fact] public async Task QueryHistoryFiltersPaginatesAndSeparatesOwners(){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();for(int i=0;i<52;i++)db.Queries.Add(new TblQuery {FarmerId=10,QueryText="QAQuestion"+i.ToString("D3"),Status="PendingApproval",CreatedDate=DateTime.Now.AddMinutes(i)});db.Queries.Add(new TblQuery{FarmerId=13,QueryText="OtherOwnerSecret",Status="Unassigned"});await db.SaveChangesAsync();}
        var first=await c.GetStringAsync("/Farmer/QueryHistory");Assert.Contains("QAQuestion051",first);Assert.DoesNotContain("QAQuestion000",first);Assert.DoesNotContain("OtherOwnerSecret",first);
        var second=await c.GetStringAsync("/Farmer/QueryHistory?page=2");Assert.Contains("QAQuestion000",second);Assert.DoesNotContain("QAQuestion051",second);
        var search=await c.GetStringAsync("/Farmer/QueryHistory?search=QAQuestion010");Assert.Contains("QAQuestion010",search);Assert.DoesNotContain("QAQuestion011",search);
        Assert.DoesNotContain("QAQuestion051",await c.GetStringAsync("/Farmer/QueryHistory?status=Answered"));
    }
    [Fact] public async Task SchemeFiltersAndOptionalCategoryRender(){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();int expired;
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();var old=new TblGovernmentScheme{SchemeName="QAExpired",LastDate=DateTime.Today.AddDays(-1)};db.GovernmentSchemes.AddRange(old,new TblGovernmentScheme{SchemeName="QANoCategory"},new TblGovernmentScheme{SchemeName="QALoan",Category="Loan"});await db.SaveChangesAsync();expired=old.Id;}
        var r=await c.GetAsync("/Scheme");Assert.Equal(HttpStatusCode.OK,r.StatusCode);var html=await r.Content.ReadAsStringAsync();Assert.Contains("QANoCategory",html);Assert.DoesNotContain("QAExpired",html);
        var filtered=await c.GetStringAsync("/Scheme?category=Loan");Assert.Contains("QALoan",filtered);Assert.DoesNotContain("QANoCategory",filtered);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/Scheme/Detail/"+expired)).StatusCode);
        using var detailsScope=app.Services.CreateScope();var liveId=await detailsScope.ServiceProvider.GetRequiredService<AppDbContext>().GovernmentSchemes.Where(x=>x.SchemeName=="QANoCategory").Select(x=>x.Id).SingleAsync();Assert.Contains("QANoCategory",await c.GetStringAsync("/Scheme/Detail/"+liveId));
    }
    [Theory] [InlineData("/Farmer/Diseases")] [InlineData("/Chat/DiseaseDetection")]
    public async Task DiseaseSearchAndCropFilter(string route){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();
        using(var s=app.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AppDbContext>();db.DiseaseDatasets.AddRange(new TblDiseaseDataset{CropName="QA Cotton",DiseaseName="QAWilt",Symptoms="QA yellow leaves"},new TblDiseaseDataset{CropName="QA Rice",DiseaseName="QABlast"},new TblDiseaseDataset{CropName="QA Cotton",DiseaseName="QAHidden",IsActive=false});await db.SaveChangesAsync();}
        var html=await c.GetStringAsync(route+"?search=yellow&crop=QA%20Cotton");Assert.Contains("QAWilt",html);Assert.DoesNotContain("QABlast",html);Assert.DoesNotContain("QAHidden",html);
    }
    [Fact] public async Task LogoutEndsAccessAndFarmerCannotAccessStaffPages(){
        using var app=new TestApp();await app.Seed();using var c=await app.Login();
        foreach(var route in new[]{"/Admin","/Officer/PendingReviews"}) {var r=await c.GetAsync(route);Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Contains("AccessDenied",r.Headers.Location!.ToString());}
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(c,"/Account/Logout",new())).StatusCode);Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync("/Farmer")).StatusCode);
    }
}

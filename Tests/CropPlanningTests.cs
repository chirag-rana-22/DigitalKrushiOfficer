using System.Net;
using System.Net.Http.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class CropPlanningTests
{
    static async Task<int> SeedCrop(TestApp app, int farmer = 10, bool email = false) {
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crop = new TblFarmerCrop { FarmerId = farmer, CustomCropName = "Tomato", FieldName = "North field", Variety = "Local", LandArea = 2, SowingDate = CropPlanning.Today, ExpectedHarvestDate = CropPlanning.Today.AddDays(90), EmailRemindersEnabled = email };
        db.FarmerCrops.Add(crop); await db.SaveChangesAsync(); return crop.Id;
    }
    [Fact] public async Task CalendarConfirmationIsPrivateAndPreservesCompletedWork() {
        using var app = new TestApp(); await app.Seed(); var id = await SeedCrop(app); using var c = await app.Login(); using var other = await app.Login("otherfarmer@test.local");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/CropPlan/Index/"+id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await app.Post(other,"/CropPlan/AcceptPlan",new(){{"id",id.ToString()}})).StatusCode);
        Assert.Contains("Confirm these reminder dates",await c.GetStringAsync("/CropPlan/Index/"+id));
        await app.Post(c,"/CropPlan/AcceptPlan",new(){{"id",id.ToString()}});
        int first, count;
        using(var scope=app.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); var tasks=await db.CropTasks.Where(t=>t.Status=="Pending").ToListAsync(); count=tasks.Count; first=tasks.First(t=>t.Kind=="Sowing").Id; Assert.Contains(tasks,t=>t.Kind=="HarvestCheck"); Assert.All(tasks,t=>Assert.True(t.DueDate>=CropPlanning.Today)); }
        await app.Post(c,"/CropPlan/TaskAction",new(){{"id",first.ToString()},{"action","done"}});
        await app.Post(c,"/CropPlan/AcceptPlan",new(){{"id",id.ToString()}});
        using(var scope=app.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); Assert.Equal("Done",(await db.CropTasks.FindAsync(first))!.Status); Assert.Equal(count-1,await db.CropTasks.CountAsync(t=>t.Status=="Pending")); }
    }
    [Fact] public async Task TasksCanBeRescheduledAndHarvestStopsReminders() {
        using var app=new TestApp();await app.Seed();var cropId=await SeedCrop(app);using var c=await app.Login();using var other=await app.Login("otherfarmer@test.local");
        await app.Post(c,"/CropPlan/AddTask",new(){{"id",cropId.ToString()},{"title","Check moisture"},{"dueDate",CropPlanning.Today.ToString("yyyy-MM-dd")}});
        int taskId;using(var scope=app.Services.CreateScope())taskId=await scope.ServiceProvider.GetRequiredService<AppDbContext>().CropTasks.Select(t=>t.Id).SingleAsync();
        Assert.Equal(HttpStatusCode.NotFound,(await app.Post(other,"/CropPlan/TaskAction",new(){{"id",taskId.ToString()},{"action","done"}})).StatusCode);
        Assert.Contains("Check moisture",await c.GetStringAsync("/CropPlan/Today"));
        await app.Post(c,"/CropPlan/TaskAction",new(){{"id",taskId.ToString()},{"action","later"}});
        Assert.DoesNotContain("Check moisture",await c.GetStringAsync("/CropPlan/Today"));
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(c,"/CropPlan/TaskAction",new(){{"id",taskId.ToString()},{"action","reschedule"},{"dueDate","invalid"}})).StatusCode);
        await app.Post(c,"/CropPlan/ConfirmHarvest",new(){{"id",cropId.ToString()},{"harvestedDate",CropPlanning.Today.ToString("yyyy-MM-dd")}});
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Equal("Harvested",(await db.FarmerCrops.FindAsync(cropId))!.Status);Assert.Equal("Cancelled",(await db.CropTasks.FindAsync(taskId))!.Status);}
        Assert.Equal(HttpStatusCode.Conflict,(await app.Post(c,"/CropPlan/TaskAction",new(){{"id",taskId.ToString()},{"action","done"}})).StatusCode);
    }
    [Fact] public async Task EditingDatesDoesNotSilentlyMoveTasksAndRejectsInvalidPlan() {
        using var app=new TestApp();await app.Seed();var id=await SeedCrop(app);using var c=await app.Login();
        await app.Post(c,"/CropPlan/AcceptPlan",new(){{"id",id.ToString()}});
        var values=new Dictionary<string,string>{{"Id",id.ToString()},{"FieldName","Field 2"},{"Variety","New variety"},{"SowingDate",CropPlanning.Today.ToString("yyyy-MM-dd")},{"ExpectedHarvestDate",CropPlanning.Today.AddDays(120).ToString("yyyy-MM-dd")},{"EmailRemindersEnabled","true"}};
        Assert.Equal(HttpStatusCode.Redirect,(await app.Post(c,"/CropPlan/Edit",values)).StatusCode);
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.True((await db.FarmerCrops.FindAsync(id))!.EmailRemindersEnabled);Assert.True(await db.CropTasks.AnyAsync(t=>t.Kind=="HarvestCheck"&&t.DueDate==CropPlanning.Today.AddDays(90)));}
        values["ExpectedHarvestDate"]=CropPlanning.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var invalid=await app.Post(c,"/CropPlan/Edit",values);Assert.Equal(HttpStatusCode.OK,invalid.StatusCode);Assert.Contains("validation-summary-errors",await invalid.Content.ReadAsStringAsync());
    }
    [Fact] public async Task SelectedCropContextFollowsConversationAndCannotBeForged() {
        using var app=new TestApp();await app.Seed();var id=await SeedCrop(app);var otherId=await SeedCrop(app,13);using var c=await app.Login();
        app.Ai.Result=new("Helpful answer",true,"Fake",1,false);
        c.DefaultRequestHeaders.Add("RequestVerificationToken",await app.Token(c));
        var denied=await c.PostAsJsonAsync("/Chat/Ask",new{queryText="Help",farmerCropId=otherId});Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);
        var ok=await c.PostAsJsonAsync("/Chat/Ask",new{queryText="How should I check it?",farmerCropId=id});Assert.Equal(HttpStatusCode.OK,ok.StatusCode);Assert.Contains("North field",app.Ai.LastCropContext);Assert.Contains("Local",app.Ai.LastCropContext);
        int root;using(var scope=app.Services.CreateScope()){var query=await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.SingleAsync();root=query.Id;Assert.Equal(id,query.FarmerCropId);Assert.Equal("How should I check it?",query.QueryText);}
        var followup=await c.PostAsJsonAsync("/Chat/Ask",new{queryText="And tomorrow?",conversationId=root});Assert.Equal(HttpStatusCode.OK,followup.StatusCode);Assert.Contains("North field",app.Ai.LastCropContext);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync("/Chat/Ask",new{queryText="Switch",conversationId=root,farmerCropId=otherId})).StatusCode);
    }
    [Fact] public async Task EmailDigestRequiresOptInAndSendsAtMostOnceDaily() {
        using var app=new TestApp();await app.Seed();var id=await SeedCrop(app,email:true);var disabled=await SeedCrop(app,email:false);
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CropTasks.AddRange(new CropTask{FarmerCropId=id,Title="<script>bad</script>",DueDate=CropPlanning.Today},new CropTask{FarmerCropId=disabled,Title="Private disabled task",DueDate=CropPlanning.Today});await db.SaveChangesAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Reminders:PublicUrl","https://dko.test"}}).Build();
        var service=new CropReminderDelivery(db,app.Mail,config);
        Assert.Equal((1,0),await service.SendDueAsync());Assert.Equal((0,0),await service.SendDueAsync());Assert.Single(app.Mail.Sent);Assert.Contains("&lt;script&gt;",app.Mail.Sent[0].Body);Assert.DoesNotContain("Private disabled task",app.Mail.Sent[0].Body);Assert.Equal("Accepted",(await db.CropDigests.SingleAsync()).Status);
    }
    [Fact] public async Task FailedDeliveryIsRecordedAndNotRepeatedBlindly() {
        using var app=new TestApp();await app.Seed();var id=await SeedCrop(app,email:true);app.Mail.DeliveryError="Provider timeout";
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.CropTasks.Add(new CropTask{FarmerCropId=id,Title="Check leaves",DueDate=CropPlanning.Today});await db.SaveChangesAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Reminders:PublicUrl","https://dko.test"}}).Build();var service=new CropReminderDelivery(db,app.Mail,config);
        Assert.Equal((0,1),await service.SendDueAsync());Assert.Equal((0,1),await service.SendDueAsync());Assert.Single(app.Mail.Sent);
    }
    [Fact] public async Task SchedulerIsDisabledWithoutPrivateConfiguration() {
        using var app=new TestApp();await app.Seed();using var c=app.Client();Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync("/internal/reminders/run",null)).StatusCode);
    }
    [Fact] public async Task SchedulerRequiresExactTokenAndRespectsDailyLimit() {
        using var app=new TestApp();await app.Seed();var first=await SeedCrop(app,email:true);var second=await SeedCrop(app,13,email:true);
        using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.CropTasks.AddRange(new CropTask{FarmerCropId=first,Title="First",DueDate=CropPlanning.Today},new CropTask{FarmerCropId=second,Title="Second",DueDate=CropPlanning.Today});await db.SaveChangesAsync();}
        var config=app.Services.GetRequiredService<IConfiguration>();config["Reminders:Enabled"]="true";config["Reminders:SchedulerToken"]=new string('a',64);config["Reminders:PublicUrl"]="https://dko.test";config["Reminders:DailyEmailLimit"]="1";
        using var c=app.Client();Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsync("/internal/reminders/run",null)).StatusCode);
        c.DefaultRequestHeaders.Add("X-Reminder-Token",new string('a',64));Assert.Equal(HttpStatusCode.OK,(await c.PostAsync("/internal/reminders/run",null)).StatusCode);Assert.Single(app.Mail.Sent);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsync("/internal/reminders/run",null)).StatusCode);Assert.Single(app.Mail.Sent);
    }
    [Fact] public async Task ConcurrentCropChangesAreDetectedByDatabase() {
        using var app=new TestApp();await app.Seed();var id=await SeedCrop(app);
        using var a=app.Services.CreateScope();using var b=app.Services.CreateScope();var first=a.ServiceProvider.GetRequiredService<AppDbContext>();var second=b.ServiceProvider.GetRequiredService<AppDbContext>();
        var left=(await first.FarmerCrops.FindAsync(id))!;var right=(await second.FarmerCrops.FindAsync(id))!;left.PlanVersion++;right.PlanVersion++;
        await first.SaveChangesAsync();await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>second.SaveChangesAsync());
    }
}

using System.Net;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class LocalizationTests
{
    [Fact] public async Task NewVisitorDefaultsToEnglishAndLanguageChangeRequiresCsrf() {
        using var app = new TestApp(); await app.Seed(); using var client = app.Client();
        var page = await client.GetStringAsync("/Account/Login");
        Assert.Contains("<html lang=\"en\"", page);
        Assert.Contains("value=\"Marathi\"", page);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Language/Change", new FormUrlEncodedContent(new Dictionary<string,string>{{"language","Hindi"}}))).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync("/Language/Change?language=Hindi")).StatusCode);
    }
    [Theory] [InlineData("Hindi","hi","मेरी फसलें")] [InlineData("Gujarati","gu","મારા પાક")] [InlineData("Marathi","mr","माझी पिके")] [InlineData("English","en","My Crops")]
    public async Task ChoicePersistsAcrossPagesAccountsAndNewAiMessages(string language,string code,string crops) {
        using var app = new TestApp(); await app.Seed(); using var client = await app.Login();
        var response = await app.Post(client, "/Language/Change", new(){{"language",language},{"returnUrl","/Chat/Index?conversationId=115"}});
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode); Assert.Equal("/Chat/Index?conversationId=115",response.Headers.Location!.OriginalString);
        foreach(var path in new[]{"/Farmer/Index","/Farmer/Crops","/Farmer/EditProfile","/CropPlan/Today","/Chat/Index","/Weather/Index","/Scheme/Index"}) {
            var page=await client.GetAsync(path); var html=WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK,page.StatusCode); Assert.Contains("<html lang=\""+code+"\"",html); Assert.Contains(crops,html); Assert.Equal(code,page.Content.Headers.ContentLanguage.Single());
        }
        await app.Ask(client); Assert.Equal(language, app.Ai.LastLanguage);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(language,(await db.Users.FindAsync(10))!.PreferredLanguage); Assert.Equal("English",(await db.Users.FindAsync(13))!.PreferredLanguage);
        Assert.Equal(language,(await db.Queries.SingleAsync()).QueryLanguage);
        using var nextDevice=await app.Login(); Assert.Contains("<html lang=\""+code+"\"",await nextDevice.GetStringAsync("/Chat"));
    }
    [Fact] public async Task AnonymousChoiceWorksOnRegisterAndUnsafeRedirectAndUnknownLanguageAreRejected() {
        using var app=new TestApp(); await app.Seed(); using var client=app.Client();
        var response=await app.Post(client,"/Language/Change",new(){{"language","Marathi"},{"returnUrl","https://external.test/"}});
        Assert.Equal("/",response.Headers.Location!.OriginalString);
        var page=WebUtility.HtmlDecode(await client.GetStringAsync("/Account/Register")); Assert.Contains("<html lang=\"mr\"",page); Assert.Contains("value=\"Marathi\" selected=\"selected\"",page);
        Assert.Equal(HttpStatusCode.BadRequest,(await app.Post(client,"/Language/Change",new(){{"language","<script>"}})).StatusCode);
    }
    [Fact] public async Task ChangingUiDoesNotRewriteExistingQuestionsAnswersOrCropNames() {
        using var app=new TestApp(); await app.Seed(); using var client=await app.Login();
        app.Ai.Result=new("Original answer in English",true,"Fake",1,false); await app.Ask(client);
        using(var s=app.Services.CreateScope()) {var db=s.ServiceProvider.GetRequiredService<AppDbContext>();db.FarmerCrops.Add(new(){FarmerId=10,CustomCropName="My custom crop <script>",FieldName="મારું ખેતર",Status="Growing"});await db.SaveChangesAsync();}
        await app.Post(client,"/Language/Change",new(){{"language","Marathi"}});
        Assert.Contains("Original answer in English",await client.GetStringAsync("/Chat"));
        var crops=await client.GetStringAsync("/Farmer/Crops");Assert.Contains("My custom crop &lt;script&gt;",crops);Assert.DoesNotContain("My custom crop <script>",crops);
    }
    [Fact] public void MarathiEmailAndGeneratedTasksAreLocalizedAndCustomTextIsPreserved() {
        var crop=new TblFarmerCrop {Crop=new(){CropName="Bajra"},FieldName="North Field",SowingDate=new(2026,10,14),ExpectedHarvestDate=new(2026,11,14)};
        var tasks=CropPlanning.Preview(crop);foreach(var task in tasks)task.FarmerCrop=crop;
        var body=WebUtility.HtmlDecode(ReminderLanguage.Email(new(){Name="Farmer",PreferredLanguage="Marathi"},tasks,new(2026,10,14),new("https://dko.test")));
        Assert.Contains("lang='mr'",body);Assert.Contains("आजची शेतीची कामे",body);Assert.Contains("बाजरी",body);Assert.Contains("North Field",body);Assert.DoesNotContain("Weekly crop and soil check",body);Assert.DoesNotContain("Check actual field conditions",body);
        Assert.Equal("English",SiteLanguage.Normalize(null)); Assert.Equal("English",SiteLanguage.Normalize("not-supported"));
    }
    [Fact] public void ValidationAndCatalogHaveLocalStringsWithoutTranslatingArbitraryData() {
        Assert.Equal("खेत का नाम भरें।",TranslationCatalog.Translate("Enter a field name.","Hindi"));
        Assert.Contains("आवश्यक",LocalizedResultFilter.Validation("The Full Name field is required.","Marathi"));
        Assert.Equal("Farmer custom note",TranslationCatalog.Translate("Farmer custom note","Gujarati"));
        var catalog=new TranslationCatalog();foreach(var language in new[]{"Hindi","Gujarati","Marathi"}) {var strings=catalog.For(language);Assert.True(strings.Count>700);Assert.All(strings.Values,value=>Assert.False(string.IsNullOrWhiteSpace(value)));}
    }
    [Fact] public async Task MarathiValidationMessagesAreRenderedForClientAndServer() {
        using var app=new TestApp();await app.Seed();using var client=await app.Login();
        await app.Post(client,"/Language/Change",new(){{"language","Marathi"}});
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/Farmer/EditProfile"));
        var required=System.Text.RegularExpressions.Regex.Match(html,"data-val-required=\"([^\"]+)\"").Groups[1].Value;
        Assert.Contains("आवश्यक",required);
        var response=await app.Post(client,"/Farmer/EditProfile",new(){{"Name",""},{"PreferredLanguage","Marathi"}});
        Assert.Contains("आवश्यक",WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }
}

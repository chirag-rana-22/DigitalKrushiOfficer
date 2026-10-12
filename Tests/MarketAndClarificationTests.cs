using System.Net;
using System.Text.Json;
using Digital_Krushi_Officer.Services;
using Digital_Krushi_Officer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
public class MarketAndClarificationTests {
    private static IConfiguration Config(bool key=true)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"MarketPrices:ApiKey",key?"test-key":""},{"AISettings:GeminiApiKey","test-key"}}).Build();
    private static string Report(string date="01/01/2020",string commodity="Tomato",string min="1000")=>JsonSerializer.Serialize(new{records=new[]{new{commodity,state="Gujarat",district="Rajkot",market="Rajkot",variety="Local",arrival_date=date,min_price=min,max_price="2000",modal_price="1500"}}});
    [Fact] public async Task PricesUseVerifiedNumbersDatesAndUnits() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new MarketPriceService(Config(),new ClientFactory(new ProviderHandler(Report())),cache);
        var answer=await service.AnswerAsync(new("Tomato","Gujarat","Rajkot",""),"English");Assert.Contains("Modal ₹1500",answer);Assert.Contains("01/01/2020",answer);Assert.Contains("NOT today's",answer);Assert.Contains("quintal",answer);Assert.Contains(MarketPriceService.Source,answer);
    }
    [Theory] [InlineData("Onion","1000")] [InlineData("Tomato","-1")] [InlineData("Tomato","3000")]
    public async Task WrongCommodityOrInvalidPricesNeverBecomeAnswers(string commodity,string min) {
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new MarketPriceService(Config(),new ClientFactory(new ProviderHandler(Report(commodity:commodity,min:min))),cache);var answer=await service.AnswerAsync(new("Tomato","Gujarat","Rajkot",""),"English");Assert.Contains("No verified report",answer);Assert.DoesNotContain("₹1500",answer);
    }
    [Fact] public async Task MissingLocationAndMissingKeyNeverInventPrice() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var handler=new ProviderHandler(Report());var service=new MarketPriceService(Config(false),new ClientFactory(handler),cache);Assert.Contains("mandi or district",await service.AnswerAsync(new("Tomato","","",""),"English"));Assert.Contains("not active",await service.AnswerAsync(new("Tomato","Gujarat","Rajkot",""),"English"));Assert.Null(handler.RequestBody);
    }
    [Theory] [InlineData(401)] [InlineData(429)] [InlineData(500)]
    public async Task ProviderFailureGivesHonestAvailabilityMessage(int status) {
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new MarketPriceService(Config(),new ClientFactory(new ProviderHandler("{}",(HttpStatusCode)status)),cache);Assert.Contains("temporarily unavailable",await service.AnswerAsync(new("Tomato","Gujarat","Rajkot",""),"English"));
    }
    private static string Assessment(bool clarify=true,bool officer=false,bool market=false)=>JsonSerializer.Serialize(new{candidates=new[]{new{finishReason="STOP",content=new{parts=new[]{new{text=JsonSerializer.Serialize(new{answer="Private diagnosis or question",requiresOfficer=officer,uncertain=true,reason="Need more evidence",complexity="Simple",imageAssessment="Unclear",needsClarification=clarify,publicSummary="Leaf discoloration is visible. A field inspection can help clarify the cause.",marketPrice=market,marketPriceKind="Wholesale",commodity="Tomato",state="Gujarat",district="Rajkot",market=""})}}}}}});
    [Fact] public async Task ClarificationDoesNotBecomeEscalationMerelyFromTurnCount() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();var ai=new AIService(Config(),NullLogger<AIService>.Instance,new ClientFactory(new ProviderHandler(Assessment())),scope.ServiceProvider.GetRequiredService<AppDbContext>());
        var first=await ai.GenerateAsync("What is wrong?","English",new byte[]{1});Assert.True(first.NeedsClarification);Assert.False(first.RequiresOfficerReview);
        var repeated=await ai.GenerateAsync("Still unclear","English",new byte[]{1},history:new[]{new AIConversationTurn("q","a",true),new AIConversationTurn("q2","a2",true)});Assert.False(repeated.RequiresOfficerReview);Assert.True(repeated.NeedsClarification);
    }
    [Fact] public async Task SpecialistFlagIsNotOverriddenByClarification() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();var ai=new AIService(Config(),NullLogger<AIService>.Instance,new ClientFactory(new ProviderHandler(Assessment(officer:true))),scope.ServiceProvider.GetRequiredService<AppDbContext>());Assert.True((await ai.GenerateAsync("Complex case","English")).RequiresOfficerReview);
    }
    [Fact] public async Task PriceIntentUsesServiceInsteadOfModelNumbersOrOfficer() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();using var cache=new MemoryCache(new MemoryCacheOptions());var market=new MarketPriceService(Config(),new ClientFactory(new ProviderHandler(Report())),cache);var ai=new AIService(Config(),NullLogger<AIService>.Instance,new ClientFactory(new ProviderHandler(Assessment(market:true))),scope.ServiceProvider.GetRequiredService<AppDbContext>(),market);var answer=await ai.GenerateAsync("Tomato price in Rajkot Gujarat?","English");Assert.Contains("Modal ₹1500",answer.Answer);Assert.DoesNotContain("Private diagnosis",answer.Answer);Assert.False(answer.RequiresOfficerReview);
    }
    [Fact] public async Task ClarificationDoesNotAssignOfficerAndEscalationShowsOnlyPublicSummary() {
        using var app=new TestApp();await app.Seed();using var c=await app.Login();app.Ai.Result=new("Please send a clearer leaf photo.",true,"Fake",1,false,null,NeedsClarification:true);var first=await app.Ask(c);var html=await c.GetStringAsync("/Chat");Assert.Contains("Please send a clearer",html);using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Empty(db.Escalations);Assert.Equal("AwaitingDetails",db.Queries.Single().Status);
        app.Ai.Result=new("PRIVATE draft",true,"Fake",1,true,"Inspection needed",PublicSummary:"Visible yellowing needs a field inspection.");await app.Ask(c);html=await c.GetStringAsync("/Chat");Assert.Contains("Visible yellowing",html);Assert.DoesNotContain("PRIVATE draft",html);
    }

    [Fact] public async Task AssignedOfficerCanReadEarlierPhotosButNotFutureOrUnrelatedPhotos() {
        using var app=new TestApp();await app.Seed();using var farmer=await app.Login();using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var upload=scope.ServiceProvider.GetRequiredService<ImageUploadService>();Directory.CreateDirectory(upload.PrivateRoot);var file=Guid.NewGuid()+".jpg";await File.WriteAllBytesAsync(Path.Combine(upload.PrivateRoot,file),new byte[]{1,2,3});
        try {
            var root=new Digital_Krushi_Officer.Models.TblQuery{FarmerId=10,QueryText="Earlier image",ImagePath="private:"+file};db.Queries.Add(root);await db.SaveChangesAsync();
            var assigned=new Digital_Krushi_Officer.Models.TblQuery{FarmerId=10,ParentQueryId=root.Id,QueryText="Expert help",Escalation=new Digital_Krushi_Officer.Models.TblEscalation{OfficerId=11}};db.Queries.Add(assigned);await db.SaveChangesAsync();
            var future=new Digital_Krushi_Officer.Models.TblQuery{FarmerId=10,ParentQueryId=root.Id,QueryText="Later image",ImagePath="private:"+file};var unrelated=new Digital_Krushi_Officer.Models.TblQuery{FarmerId=10,QueryText="Other chat",ImagePath="private:"+file};db.Queries.AddRange(future,unrelated);await db.SaveChangesAsync();
            using var officer=await app.Login("officer@test.local");Assert.Equal(HttpStatusCode.OK,(await officer.GetAsync("/Media/QueryImage/"+root.Id)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await officer.GetAsync("/Media/QueryImage/"+future.Id)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await officer.GetAsync("/Media/QueryImage/"+unrelated.Id)).StatusCode);Assert.Contains("Earlier image",await officer.GetStringAsync("/Officer/EscalationDetail/"+assigned.Escalation.Id));
            using var other=await app.Login("other@test.local");Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/Media/QueryImage/"+root.Id)).StatusCode);
        } finally {File.Delete(Path.Combine(upload.PrivateRoot,file));}
    }
}

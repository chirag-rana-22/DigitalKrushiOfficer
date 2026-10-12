using System.Net;
using System.Text.Json;
using Digital_Krushi_Officer.Services;
using Digital_Krushi_Officer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;
public class ConversationQualityTests {
    [Theory] [InlineData("tpdays onoine price","Onion")] [InlineData("aje onion no bhav su chhe","Onion")] [InlineData("today tomato price in junagadh","Tomato")]
    public void ActualTranscriptPriceQuestionsAreRecognized(string input,string crop) {Assert.Equal(crop,MarketConversation.Resolve(input,null)!.Commodity);}
    [Fact] public void LocationAndCropChangesReuseUserContext() {
        var history=new[]{new AIConversationTurn("todays onion price","Which mandi?"),new AIConversationTurn("junagadh","Unavailable")};
        var r=MarketConversation.Resolve("today tomato price",history)!;Assert.Equal("Tomato",r.Commodity);Assert.Equal("Junagadh",r.District);Assert.Equal("Gujarat",r.State);
        r=MarketConversation.Resolve("junagadh",history.Take(1).ToList())!;Assert.Equal("Onion",r.Commodity);Assert.Equal("Gujarat",r.State);
        Assert.Null(MarketConversation.Resolve("junagadh",null));
        var changed=history.Append(new AIConversationTurn("today tomato price","Unavailable")).ToArray();
        Assert.Equal("Junagadh",MarketConversation.Resolve("abe mara question no answer ap",changed)!.District);
    }
    [Theory] [InlineData("tomato disease and price")] [InlineData("onion price in Mumbai")] [InlineData("onion MSP")] [InlineData("tomato price tomorrow")]
    public void UnsupportedOrMixedRequestUsesSemanticRoute(string text) {Assert.Null(MarketConversation.Resolve(text,null));}
    [Fact] public async Task MissingDetailsOnlyAskWhatIsMissing() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var cfg=new ConfigurationBuilder().Build();var service=new MarketPriceService(cfg,new ClientFactory(new ProviderHandler("{}")),cache);
        var answer=await service.AnswerAsync(new("Onion","","",""),"English");Assert.StartsWith("Onion",answer);Assert.Contains("mandi or district",answer);Assert.DoesNotContain("Which crop",answer);
        answer=await service.AnswerAsync(new("Tomato","","Junagadh",""),"English");Assert.Contains("Tomato, Junagadh, Gujarat",answer);Assert.Contains("not active",answer);Assert.DoesNotContain("Please tell",answer);
    }
    [Fact] public async Task PriceRouteStillWorksWhenGeminiHasNoKey() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();using var cache=new MemoryCache(new MemoryCacheOptions());var cfg=new ConfigurationBuilder().Build();var factory=new ClientFactory(new ProviderHandler("{}",HttpStatusCode.TooManyRequests));var market=new MarketPriceService(cfg,factory,cache);var ai=new AIService(cfg,NullLogger<AIService>.Instance,factory,scope.ServiceProvider.GetRequiredService<AppDbContext>(),market);
        var r=await ai.GenerateAsync("tpdays onoine price","English");Assert.True(r.Available);Assert.False(r.RequiresOfficerReview);Assert.Contains("Onion",r.Answer);
        r=await ai.GenerateAsync("junagadh","English",history:new[]{new AIConversationTurn("tpdays onoine price",r.Answer)});Assert.Contains("Junagadh",r.Answer);Assert.False(r.RequiresOfficerReview);
    }
    [Fact] public async Task TechnicalFailureIsSavedForRetryWithoutOfficerTicket() {
        using var app=new TestApp();await app.Seed();app.Ai.Result=new("Try again shortly.",false,"Unavailable",1,false,RetryableFailure:true);using var farmer=await app.Login();await app.Ask(farmer);using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Empty(await db.Escalations.ToListAsync());Assert.Equal("RetryNeeded",(await db.Queries.SingleAsync()).Status);var html=await farmer.GetStringAsync("/Chat");Assert.Contains("Try again shortly",html);Assert.Contains("Temporarily unavailable",html);
    }

    [Fact] public void FrustrationContinuesPriceQuestionButNewTopicDoesNot() {
        var history=new[]{new AIConversationTurn("today onion price in junagadh","not active"),new AIConversationTurn("fuck you","Please behave")};
        Assert.Equal("Onion",MarketConversation.Resolve("abe mara question no answer ap",history)!.Commodity);
        Assert.Null(MarketConversation.Resolve("please answer what is compost",history));
        Assert.Null(MarketConversation.Resolve("onion and tomato price",history));
        Assert.Null(MarketConversation.Resolve("onion price 2020",history));
    }

    [Fact] public async Task FailedPriceProviderDoesNotMakeEveryFollowupWaitAgain() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"MarketPrices:ApiKey","test-key"}}).Build();var handler=new ProviderHandler("{}",HttpStatusCode.ServiceUnavailable);var service=new MarketPriceService(cfg,new ClientFactory(handler),cache);
        Assert.Contains("Onion",await service.AnswerAsync(new("Onion","Gujarat","Junagadh",""),"English"));Assert.Contains("Tomato",await service.AnswerAsync(new("Tomato","Gujarat","Junagadh",""),"English"));Assert.Equal(1,handler.RequestCount);
    }
}

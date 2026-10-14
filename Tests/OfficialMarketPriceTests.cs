using System.Net;
using System.Text;
using System.Text.Json;
using Digital_Krushi_Officer.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

public class OfficialMarketPriceTests {
    private const string Filters="""
    {"data":{"cmdt_data":[{"cmdt_id":23,"cmdt_name":"Onion"}],"state_data":[{"state_id":11,"state_name":"Gujarat"}],"district_data":[{"id":159,"state_id":11,"district_name":"Surat"}],"market_data":[{"id":100002,"mkt_name":"All Markets","state_id":null,"district_id":null},{"id":64,"mkt_name":"Surat APMC","state_id":11,"district_id":159},{"id":785,"mkt_name":"Bardoli APMC","state_id":11,"district_id":159}]}}
    """;
    private static string Report(string variety="Onion - Other",string unit="Rs./Quintal",string? date=null,object? amount=null,string market="Surat APMC") => JsonSerializer.Serialize(new {
        success=true,title=$"Commodity Prices During Last Week Market: {market}, State/UT: Gujarat",
        data=new[]{new Dictionary<string,object>{{"variety",variety},{"unitOfPrice",unit},{date??DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)).AddDays(-2).ToString("yyyy-MM-dd"),amount??3100m},{"2019-12-31","NR"}}}
    });
    private sealed class Handler(string report) : HttpMessageHandler {
        public List<Uri> Requests=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(request.RequestUri!.AbsolutePath.EndsWith("filters")?Filters:report,Encoding.UTF8,"application/json")});
        }
    }
    [Fact] public async Task PublicSourceWorksWithoutKeyAndPreservesDateUnitAndMarket() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var handler=new Handler(Report());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"MarketPrices:OfficialSourceEnabled","true"}}).Build();
        var service=new MarketPriceService(config,new ClientFactory(handler),cache);
        var answer=await service.AnswerAsync(new("Onion","Gujarat","Surat",""),"English");
        Assert.Contains("₹3100",answer);Assert.Contains("Surat APMC",answer);Assert.Contains("NOT today's",answer);Assert.Contains("Reported price",answer);
        Assert.DoesNotContain("Modal ₹",answer);Assert.Contains(OfficialMarketPriceService.Source,answer);
        Assert.Equal(2,handler.Requests.Count);Assert.All(handler.Requests,u=>Assert.Equal("api.agmarknet.gov.in",u.Host));
        Assert.Contains("marketId=64",handler.Requests.Last().Query);
        await service.AnswerAsync(new("Onion","Gujarat","Surat",""),"Gujarati");Assert.Equal(2,handler.Requests.Count);
    }
    [Theory]
    [InlineData("Tomato - Other","Rs./Quintal","2020-01-01","3100","Surat APMC")]
    [InlineData("Onion - Other","Rs./Kg","2020-01-01","3100","Surat APMC")]
    [InlineData("Onion - Other","Rs./Quintal","2099-01-01","3100","Surat APMC")]
    [InlineData("Onion - Other","Rs./Quintal","2020-01-01","NR","Surat APMC")]
    [InlineData("Onion - Other","Rs./Quintal","2020-01-01","-1","Surat APMC")]
    [InlineData("Onion - Other","Rs./Quintal","2020-01-01","3100","Rajkot APMC")]
    public async Task InvalidOrUnrelatedReportCannotBecomeAPrice(string variety,string unit,string date,string amount,string market) {
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new OfficialMarketPriceService(new ClientFactory(new Handler(Report(variety,unit,date,amount,market))),cache);
        Assert.Null(await service.AnswerAsync(new("Onion","Gujarat","Surat",""),"English"));
    }
    [Fact] public async Task NamedMarketNeverSilentlyBecomesAnotherMarket() {
        using var cache=new MemoryCache(new MemoryCacheOptions());var handler=new Handler(Report());var service=new OfficialMarketPriceService(new ClientFactory(handler),cache);
        Assert.Null(await service.AnswerAsync(new("Onion","Gujarat","Surat","Unknown"),"English"));Assert.Single(handler.Requests);
        Assert.Null(await service.AnswerAsync(new("Onion","Gujarat","Rajkot","Surat"),"English"));Assert.Single(handler.Requests);
    }
    [Theory] [InlineData("Gujarati","નોંધાયેલ ભાવ")] [InlineData("Marathi","नोंदवलेला भाव")] [InlineData("Hindi","दर्ज भाव")]
    public async Task ReportLabelsFollowFarmerLanguage(string language,string label) {
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new OfficialMarketPriceService(new ClientFactory(new Handler(Report())),cache);
        var answer=await service.AnswerAsync(new("Onion","Gujarat","Surat",""),language);
        Assert.Contains(label,answer);Assert.Contains("₹3100",answer);
    }
    [Fact] public async Task PublicSourceFailureWithoutKeyIsUnavailableRatherThanNotConfigured() {
        using var cache=new MemoryCache(new MemoryCacheOptions());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"MarketPrices:OfficialSourceEnabled","true"}}).Build();
        var handler=new ProviderHandler("{}",HttpStatusCode.ServiceUnavailable);
        var service=new MarketPriceService(config,new ClientFactory(handler),cache);
        var answer=await service.AnswerAsync(new("Onion","Gujarat","Surat",""),"English");
        Assert.Contains("temporarily unavailable",answer);Assert.DoesNotContain("not active",answer);Assert.Contains(OfficialMarketPriceService.Source,answer);
    }
}

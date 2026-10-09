using System.Net;
using System.Text;
using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Xunit;
public class ProviderHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler {
    public string? RequestBody;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){RequestBody=request.Content==null?null:await request.Content.ReadAsStringAsync(ct);return new(status){Content=new StringContent(body)};}
}
public class ClientFactory(HttpMessageHandler handler):IHttpClientFactory {public HttpClient CreateClient(string name)=>new(handler,false);}
public class ProviderAndUploadTests {
    [Fact] public async Task ImageBytesActuallyReachGeminiRequest() {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();
        var handler=new ProviderHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Observed leaf discoloration\"}]}}]}");
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AISettings:GeminiApiKey","test-key"}}).Build();
        var service=new AIService(config,NullLogger<AIService>.Instance,new ClientFactory(handler),scope.ServiceProvider.GetRequiredService<AppDbContext>());
        var bytes=new byte[]{1,2,3,4};var result=await service.GenerateAsync("Inspect the photo","Hindi",bytes);
        Assert.True(result.Available);using var json=JsonDocument.Parse(handler.RequestBody!);
        var image=json.RootElement.GetProperty("contents")[0].GetProperty("parts")[1].GetProperty("inline_data");
        Assert.Equal("image/jpeg",image.GetProperty("mime_type").GetString());Assert.Equal(bytes,Convert.FromBase64String(image.GetProperty("data").GetString()!));
    }
    [Theory] [InlineData(429)] [InlineData(500)] [InlineData(401)]
    public async Task AiProviderFailureDoesNotInventAdvice(int status) {
        using var app=new TestApp();await app.Seed();using var scope=app.Services.CreateScope();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AISettings:GeminiApiKey","test-key"}}).Build();
        var service=new AIService(config,NullLogger<AIService>.Instance,new ClientFactory(new ProviderHandler("{}",(HttpStatusCode)status)),scope.ServiceProvider.GetRequiredService<AppDbContext>());
        var result=await service.GenerateAsync("weather next week","Gujarati");Assert.False(result.Available);Assert.Equal("Unavailable",result.Model);Assert.DoesNotContain("clear",result.Answer);
    }
    [Fact] public async Task WeatherUsesForecastProbabilityAndConvertsWindUnits() {
        var handler=new ProviderHandler("{\"list\":[{\"dt\":1791446400,\"main\":{\"temp\":30,\"humidity\":70},\"wind\":{\"speed\":10},\"pop\":0.65,\"weather\":[{\"description\":\"cloudy\"}]}]}");
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AISettings:OpenWeatherApiKey","test-key"}}).Build();
        using var cache=new MemoryCache(new MemoryCacheOptions());var service=new WeatherService(config,NullLogger<WeatherService>.Instance,new ClientFactory(handler),cache);
        var result=await service.GetWeatherAsync("Ahmedabad");Assert.NotNull(result);Assert.Equal(65,result.RainChance);Assert.Equal(36,result.WindSpeed);
    }
    [Fact] public async Task InvalidImageIsRejectedAndPrivateImagesRequireOwnership() {
        using var app=new TestApp();await app.Seed();using var farmer=await app.Login();
        var bad=new MultipartFormDataContent();bad.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("not an image")),"image","fake.jpg");
        var badRequest=new HttpRequestMessage(HttpMethod.Post,"/Chat/UploadImage"){Content=bad};badRequest.Headers.Add("RequestVerificationToken",await app.Token(farmer,"/Chat"));
        Assert.Equal(HttpStatusCode.BadRequest,(await farmer.SendAsync(badRequest)).StatusCode);
        using var bitmap=new SKBitmap(64,64);bitmap.Erase(SKColors.Green);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,90);
        var upload=new MultipartFormDataContent();upload.Add(new ByteArrayContent(data.ToArray()),"image","leaf.png");upload.Add(new StringContent("Review this photo"),"userPrompt");
        var request=new HttpRequestMessage(HttpMethod.Post,"/Chat/UploadImage"){Content=upload};request.Headers.Add("RequestVerificationToken",await app.Token(farmer,"/Chat"));
        var response=await farmer.SendAsync(request);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var query=await db.Queries.SingleAsync();
        try {
            var media=await farmer.GetAsync("/Media/QueryImage/"+query.Id);Assert.Equal(HttpStatusCode.OK,media.StatusCode);Assert.Equal("image/jpeg",media.Content.Headers.ContentType!.MediaType);
            using var other=await app.Login("otherfarmer@test.local");Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/Media/QueryImage/"+query.Id)).StatusCode);
            using var officer=await app.Login("officer@test.local");Assert.Equal(HttpStatusCode.OK,(await officer.GetAsync("/Media/QueryImage/"+query.Id)).StatusCode);
            using var outsider=await app.Login("other@test.local");Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync("/Media/QueryImage/"+query.Id)).StatusCode);
        } finally {var uploads=scope.ServiceProvider.GetRequiredService<ImageUploadService>();File.Delete(Path.Combine(uploads.PrivateRoot,query.ImagePath![8..]));}
    }
    [Fact] public async Task AccountRateLimitReturns429() {
        using var app=new TestApp();await app.Seed();using var c=app.Client();HttpResponseMessage? last=null;
        for(var i=0;i<22;i++)last=await c.GetAsync("/Account/Login");Assert.Equal(HttpStatusCode.TooManyRequests,last!.StatusCode);
    }
}

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

public class CloudDeploymentTests
{
    static IConfiguration Config(Dictionary<string, string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    static IConfiguration StorageConfig() => Config(new() {
        ["Storage:Provider"] = "Supabase", ["Storage:Supabase:Url"] = "https://test.supabase.co",
        ["Storage:Supabase:Bucket"] = "dko-media", ["Storage:Supabase:ServiceRoleKey"] = "server-test-key"
    });

    [Fact] public async Task CloudPhotosUseAuthenticatedBucketAndDeleteExactObject()
    {
        using var app = new TestApp();
        var handler = new CloudHandler();
        var storage = new PhotoStorage(StorageConfig(), app.Services.GetRequiredService<IWebHostEnvironment>(), new CloudClients(handler));
        await storage.PutAsync("leaf.jpg", [1,2,3]);
        Assert.Equal(new byte[] {1,2,3}, await storage.GetAsync("leaf.jpg"));
        await storage.DeleteAsync("leaf.jpg");
        Assert.Equal("/storage/v1/object/dko-media/queries/leaf.jpg", handler.Requests[0].Path);
        Assert.Equal("/storage/v1/object/authenticated/dko-media/queries/leaf.jpg", handler.Requests[1].Path);
        Assert.All(handler.Requests, r => Assert.Equal("Bearer server-test-key", r.Auth));
        using var body = JsonDocument.Parse(handler.Requests[2].Body);
        Assert.Equal("queries/leaf.jpg", body.RootElement.GetProperty("prefixes")[0].GetString());
    }

    [Theory] [InlineData("../secret")] [InlineData("x/y.jpg")] [InlineData("x\\y.jpg")] [InlineData("a?b.jpg")]
    public void StorageRejectsPathsOutsidePhotoNamespace(string path) => Assert.Throws<ArgumentException>(() => PhotoStorage.ValidateName(path));

    [Fact] public async Task CloudFailureDoesNotSilentlySaveToEphemeralDisk()
    {
        using var app = new TestApp();
        var handler = new CloudHandler { Status = HttpStatusCode.ServiceUnavailable };
        var storage = new PhotoStorage(StorageConfig(), app.Services.GetRequiredService<IWebHostEnvironment>(), new CloudClients(handler));
        await Assert.ThrowsAsync<HttpRequestException>(() => storage.PutAsync("leaf.jpg", [1,2,3]));
    }

    [Fact] public async Task MissingPhotoIsNotFound()
    {
        using var app = new TestApp();
        var storage = new PhotoStorage(StorageConfig(), app.Services.GetRequiredService<IWebHostEnvironment>(), new CloudClients(new CloudHandler { Status = HttpStatusCode.NotFound }));
        Assert.Null(await storage.GetAsync("missing.jpg"));
    }

    [Theory]
    [InlineData("{\"statusCode\":\"404\",\"code\":\"NoSuchKey\"}", true)]
    [InlineData("{\"code\":\"AccessDenied\"}", false)]
    [InlineData("invalid response", false)]
    public async Task StorageWrappedMissingObjectDoesNotHideOtherErrors(string body, bool missing)
    {
        using var app = new TestApp();
        var handler = new CloudHandler { Status = HttpStatusCode.BadRequest, ResponseBody = body };
        var storage = new PhotoStorage(StorageConfig(), app.Services.GetRequiredService<IWebHostEnvironment>(), new CloudClients(handler));
        if (missing) Assert.Null(await storage.GetAsync("missing.jpg"));
        else await Assert.ThrowsAsync<HttpRequestException>(() => storage.GetAsync("missing.jpg"));
    }

    [Fact] public async Task EmailApiUsesServerKeyAndPreservesTemplate()
    {
        var handler = new CloudHandler();
        var service = new EmailService(Config(new() { ["Email:Provider"]="Brevo", ["Email:Brevo:ApiKey"]="email-test-key", ["Email:FromEmail"]="sender@example.com" }), NullLogger<EmailService>.Instance, new CloudClients(handler));
        Assert.Null(await service.SendEmailAsync("recipient@example.com", "Farmer", "Verification", "<p>Test OTP</p>"));
        Assert.Equal("/v3/smtp/email", handler.Requests.Single().Path);
        Assert.Equal("email-test-key", handler.ApiKey);
        using var body = JsonDocument.Parse(handler.Requests.Single().Body);
        Assert.Equal("<p>Test OTP</p>", body.RootElement.GetProperty("htmlContent").GetString());
    }

    [Fact] public async Task EmailApiFailureIsReportedWithoutProviderBody()
    {
        var handler = new CloudHandler { Status=HttpStatusCode.TooManyRequests };
        var service = new EmailService(Config(new() { ["Email:Provider"]="Brevo", ["Email:Brevo:ApiKey"]="email-test-key", ["Email:FromEmail"]="sender@example.com" }), NullLogger<EmailService>.Instance, new CloudClients(handler));
        Assert.Equal("Email delivery is temporarily unavailable. Please try again later.", await service.SendEmailAsync("a@example.com", "A", "Test", "Test"));
    }

    [Fact] public void DatabaseEngineIsExplicitAndCloudRequiresVerifiedTls()
    {
        Assert.IsType<MySqlServerVersion>(CloudHosting.DatabaseVersion(Config(new() { ["Database:Provider"]="MySql", ["Database:ServerVersion"]="8.0.36" })));
        Assert.IsType<MariaDbServerVersion>(CloudHosting.DatabaseVersion(Config(new())));
        Assert.Throws<InvalidOperationException>(() => ProductionReadiness.ValidateHosting(Config(new() { ["Hosting:Platform"]="Render", ["ConnectionStrings:DefaultConnection"]="Server=test;User=app;Password=test;SslMode=Required" })));
    }

    [Fact] public async Task EncryptedDatabaseKeysSurviveNewApplicationInstance()
    {
        using var app = new TestApp(); await app.Seed();
        var repository = new DatabaseKeyRepository(app.Services.GetRequiredService<IServiceScopeFactory>());
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=DKO Test Keys", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        ServiceProvider Instance() {
            var services = new ServiceCollection(); services.AddLogging();
            services.AddDataProtection().SetApplicationName("DKOCloudTest").ProtectKeysWithCertificate(certificate)
                .AddKeyManagementOptions(o => o.XmlRepository = repository);
            return services.BuildServiceProvider();
        }
        using var first = Instance();
        var encrypted = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("login").Protect("existing session");
        using var scope = app.Services.CreateScope();
        var records = await scope.ServiceProvider.GetRequiredService<AppDbContext>().ProtectionKeys.ToListAsync();
        Assert.NotEmpty(records);
        Assert.Contains("encryptedSecret", records[0].Xml);
        using var second = Instance();
        Assert.Equal("existing session", second.GetRequiredService<IDataProtectionProvider>().CreateProtector("login").Unprotect(encrypted));
    }

    [Fact] public async Task UnauthorizedFarmerCannotTriggerCloudPhotoDownload()
    {
        using var app = new CloudStorageApp(); await app.Seed();
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = new Digital_Krushi_Officer.Models.TblQuery { FarmerId=10, QueryText="Photo", ImagePath="private:leaf.jpg", Status="Answered" };
        db.Queries.Add(query); await db.SaveChangesAsync();
        using var other = await app.Login("otherfarmer@test.local");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/Media/QueryImage/"+query.Id)).StatusCode);
        Assert.Equal(0, app.Storage.Downloads);
        using var owner = await app.Login();
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/Media/QueryImage/"+query.Id)).StatusCode);
        Assert.Equal(1, app.Storage.Downloads);
    }

    [Fact] public async Task FailedCloudBatchCleansUploadedFilesWithoutCreatingQuery()
    {
        using var app = new CloudStorageApp(); await app.Seed(); app.Storage.FailSecondUpload=true;
        using var client = await app.Login();
        using var bitmap = new SKBitmap(64,64); bitmap.Erase(SKColors.Green);
        using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png,90);
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(encoded.ToArray()), "images", "one.png");
        content.Add(new ByteArrayContent(encoded.ToArray()), "images", "two.png");
        using var request = new HttpRequestMessage(HttpMethod.Post,"/Chat/UploadImage") { Content=content };
        request.Headers.Add("RequestVerificationToken", await app.Token(client,"/Chat"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.SendAsync(request)).StatusCode);
        Assert.Single(app.Storage.Deleted);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.ToListAsync());
    }
}

public class CloudStorageApp : TestApp
{
    public readonly TestCloudStorage Storage = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s => { s.RemoveAll<IPhotoStorage>(); s.AddSingleton<IPhotoStorage>(Storage); });
    }
}
public class TestCloudStorage : IPhotoStorage
{
    public bool FailSecondUpload; public int Uploads; public int Downloads;
    public List<string> Deleted = [];
    public Task PutAsync(string name, byte[] bytes, bool isPublic=false)
    {
        Uploads++;
        if (FailSecondUpload && Uploads==2) throw new HttpRequestException("Simulated provider failure");
        return Task.CompletedTask;
    }
    public Task<byte[]?> GetAsync(string name, bool isPublic=false) { Downloads++; return Task.FromResult<byte[]?>([1,2,3]); }
    public Task DeleteAsync(string name, bool isPublic=false) { Deleted.Add(name); return Task.CompletedTask; }
}

public class CloudClients(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, false);
}
public class CloudHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string? ApiKey { get; set; }
    public string? ResponseBody { get; set; }
    public List<(string Path, string? Auth, string Body)> Requests { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Headers.TryGetValues("api-key", out var keys)) ApiKey = keys.Single();
        Requests.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct)));
        return new HttpResponseMessage(Status) { Content = ResponseBody == null ? new ByteArrayContent([1,2,3]) : new StringContent(ResponseBody) };
    }
}

using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--migrate-only" && a != "--copy-media-to-cloud").ToArray());
builder.Configuration.AddJsonFile("appsettings.Local.json", true, false).AddEnvironmentVariables();
CloudHosting.Configure(builder);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TranslationCatalog>();
builder.Services.AddScoped<UiText>();
builder.Services.AddScoped<LocalizedResultFilter>();
builder.Services.AddControllersWithViews(o => { o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.AddService<LocalizedResultFilter>(); });
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(CloudHosting.DatabaseConnection(builder.Configuration), CloudHosting.DatabaseVersion(builder.Configuration), b => b.EnableRetryOnFailure()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Home/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(60); o.SlidingExpiration = true;
    o.Cookie.Name = "DKO.Auth"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Events.OnValidatePrincipal = async context => {
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = int.TryParse(id, out var userId) ? await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId) : null;
        if (user == null || !user.IsActive || !user.IsEmailVerified || context.Principal?.FindFirstValue("SecurityStamp") != AccountSecurity.Stamp(user)) {
            context.RejectPrincipal(); await context.HttpContext.SignOutAsync();
        } else {
            var changed = context.Principal?.FindFirstValue("Language") != user.PreferredLanguage;
            context.ReplacePrincipal(AccountSecurity.Principal(user)); context.ShouldRenew = changed;
        }
    };
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(30); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("providers", c => c.Timeout = TimeSpan.FromSeconds(40));
builder.Services.AddHttpClient("storage", c => c.Timeout = TimeSpan.FromSeconds(30)).RemoveAllLoggers();
builder.Services.AddHttpClient("email-api", c => c.Timeout = TimeSpan.FromSeconds(15)).RemoveAllLoggers();
builder.Services.AddScoped<IPhotoStorage, PhotoStorage>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<CropReminderDelivery>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddHttpClient("market-prices", c => c.Timeout = TimeSpan.FromSeconds(12)).RemoveAllLoggers();
builder.Services.AddScoped<IMarketPriceService, MarketPriceService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<OtpService>(); builder.Services.AddScoped<QueryWorkflow>(); builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("account", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("advisory", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = async (c, ct) => { c.HttpContext.Response.Headers.RetryAfter = "60"; await c.HttpContext.Response.WriteAsync("Too many requests. Please wait a minute and try again.", ct); };
});
builder.Services.AddHealthChecks();
var app = builder.Build();
if (args.Contains("--copy-media-to-cloud")) {
    await MediaMigration.CopyAsync(app.Services);
    return;
}
if (args.Contains("--migrate-only")) {
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    return;
}
if (RenderPublicEdge.Enabled(app.Configuration)) app.Use(next => context => RenderPublicEdge.InvokeAsync(context, next));
else app.UseForwardedHeaders();
if (!app.Environment.IsEnvironment("Testing")) {
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:ApplyMigrations")) await db.Database.MigrateAsync();
    if (app.Environment.IsProduction()) await ProductionReadiness.ValidateAsync(app.Configuration, db);
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff"; context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (context.Request.Path.StartsWithSegments("/uploads/crops") || context.Request.Path.StartsWithSegments("/uploads/voice")) { context.Response.StatusCode = 404; return; }
    await next();
});
app.UseStaticFiles(); app.UseRouting(); app.UseSession(); app.UseAuthentication(); app.Use(SiteLanguage.InvokeAsync); app.UseAuthorization(); app.UseRateLimiter();
app.MapHealthChecks("/health/live");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
public partial class Program { }

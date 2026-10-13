using System.Net;

namespace Digital_Krushi_Officer.Services;

/// <summary>Only for Render Free public web services, which cannot receive private inbound traffic.</summary>
public static class RenderPublicEdge
{
    public static bool Enabled(IConfiguration config) => config.GetValue<bool>("Hosting:RenderFreePublicEdge");

    public static void Configure(IConfiguration config)
    {
        if (!Enabled(config)) return;
        var host = config["RENDER_EXTERNAL_HOSTNAME"];
        if (config["RENDER"] != "true" || config["RENDER_SERVICE_TYPE"] != "web"
            || !string.Equals(config["Hosting:Platform"], "Render", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(host) || Uri.CheckHostName(host) != UriHostNameType.Dns
            || !host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("RenderFreePublicEdge requires a Render public web service and its assigned hostname.");
        if (config["AllowedHosts"] is null or "" or "*") config["AllowedHosts"] = host;
    }

    public static async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Render probes can bypass Cloudflare. This endpoint exposes no application or database data.
        if (context.Request.Path == "/health/live" && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("Healthy");
            return;
        }
        // Render documents that Cloudflare overwrites this header. Never accept the caller's X-Forwarded-For chain.
        var header = context.Request.Headers["CF-Connecting-IP"];
        if (header.Count != 1 || !IPAddress.TryParse(header[0], out var clientIp))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        context.Connection.RemoteIpAddress = clientIp;
        // Render redirects HTTP to HTTPS at its public edge; do not trust a caller-provided proto header.
        context.Request.Scheme = "https";
        await next(context);
    }
}

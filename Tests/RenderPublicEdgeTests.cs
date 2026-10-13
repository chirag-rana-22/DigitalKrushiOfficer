using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

public class RenderPublicEdgeTests
{
    static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Hosting:RenderFreePublicEdge"] = "true", ["Hosting:Platform"] = "Render",
        ["RENDER"] = "true", ["RENDER_SERVICE_TYPE"] = "web", ["RENDER_EXTERNAL_HOSTNAME"] = "dko-test.onrender.com"
    }).Build();

    [Fact] public void UsesAssignedHostnameAndRejectsNonRenderDeployment()
    {
        var config = Config();
        RenderPublicEdge.Configure(config);
        Assert.Equal("dko-test.onrender.com", config["AllowedHosts"]);
        config["RENDER_SERVICE_TYPE"] = "pserv";
        Assert.Throws<InvalidOperationException>(() => RenderPublicEdge.Configure(config));
        config["RENDER_SERVICE_TYPE"] = "web";
        config["RENDER_EXTERNAL_HOSTNAME"] = "dko.onrender.com.attacker.test";
        Assert.Throws<InvalidOperationException>(() => RenderPublicEdge.Configure(config));
    }

    [Fact] public async Task IgnoresSpoofedForwardingHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/Login";
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.17";
        context.Request.Headers["X-Forwarded-For"] = "192.0.2.3";
        context.Request.Headers["X-Forwarded-Proto"] = "http";
        var invoked = false;
        await RenderPublicEdge.InvokeAsync(context, _ => { invoked = true; return Task.CompletedTask; });
        Assert.True(invoked);
        Assert.Equal("203.0.113.17", context.Connection.RemoteIpAddress?.ToString());
        Assert.True(context.Request.IsHttps);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad")]
    [InlineData("203.0.113.17, 192.0.2.3")]
    public async Task RejectsMissingOrAmbiguousEdgeIdentity(string header)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/Login";
        context.Request.Headers["CF-Connecting-IP"] = header;
        await RenderPublicEdge.InvokeAsync(context, _ => throw new Exception("Must not reach application"));
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact] public async Task DirectHealthProbeDoesNotReachApplication()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        await RenderPublicEdge.InvokeAsync(context, _ => throw new Exception("Must not reach application"));
        Assert.Equal(200, context.Response.StatusCode);
    }
}

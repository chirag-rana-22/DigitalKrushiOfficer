using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;
public class ConversationTests {
    private static async Task<JsonElement> Send(TestApp app,HttpClient c,string text,int? conversation=null) {
        var r=new HttpRequestMessage(HttpMethod.Post,"/Chat/Ask") { Content=JsonContent.Create(new{queryText=text,conversationId=conversation}) };
        r.Headers.Add("RequestVerificationToken",await app.Token(c,"/Chat"));var response=await c.SendAsync(r);response.EnsureSuccessStatusCode();return (await response.Content.ReadFromJsonAsync<JsonElement>());
    }
    [Fact] public async Task ConversationKeepsContextAndNewChatIsSeparate() {
        using var app=new TestApp();await app.Seed();app.Ai.Result=new("Cotton advice",true,"Fake",1,false,null,Digital_Krushi_Officer.Models.ComplexityLevel.Simple);using var c=await app.Login();
        var first=await Send(app,c,"I grow cotton");var root=first.GetProperty("conversationId").GetInt32();
        var next=await Send(app,c,"What crop?",root);Assert.Equal(root,next.GetProperty("conversationId").GetInt32());Assert.Single(app.Ai.LastHistory);Assert.Equal("I grow cotton",app.Ai.LastHistory[0].Question);Assert.Equal("Cotton advice",app.Ai.LastHistory[0].Answer);
        Assert.Single((await c.GetFromJsonAsync<JsonElement>("/Chat/Conversations")).EnumerateArray());
        Assert.DoesNotContain("data-query-id=",await c.GetStringAsync("/Chat?newChat=true"));
        await Send(app,c,"A separate soil question");Assert.Empty(app.Ai.LastHistory);Assert.Equal(2,(await c.GetFromJsonAsync<JsonElement>("/Chat/Conversations")).GetArrayLength());
        Assert.Single((await c.GetFromJsonAsync<JsonElement>("/Chat/Conversations?search=cotton")).EnumerateArray());
        using var other=await app.Login("otherfarmer@test.local");Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/Chat?conversationId="+root)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/Chat/Messages?conversationId={root}&beforeId=999")).StatusCode);
        var forged=new HttpRequestMessage(HttpMethod.Post,"/Chat/Ask"){Content=JsonContent.Create(new{queryText="Read their chat",conversationId=root})};forged.Headers.Add("RequestVerificationToken",await app.Token(other,"/Chat"));Assert.Equal(HttpStatusCode.NotFound,(await other.SendAsync(forged)).StatusCode);
    }
    [Fact] public async Task MultiplePhotosReachAiAndEveryPhotoIsProtected() {
        using var app=new TestApp();await app.Seed();using var c=await app.Login();using var bitmap=new SKBitmap(2000,1000);bitmap.Erase(SKColors.Green);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,90);
        using var form=new MultipartFormDataContent();for(int i=0;i<3;i++)form.Add(new ByteArrayContent(data.ToArray()),"images",$"leaf{i}.png");form.Add(new StringContent("Compare crop and soil"),"userPrompt");
        var request=new HttpRequestMessage(HttpMethod.Post,"/Chat/UploadImage"){Content=form};request.Headers.Add("RequestVerificationToken",await app.Token(c,"/Chat"));var response=await c.SendAsync(request);response.EnsureSuccessStatusCode();Assert.Equal(3,app.Ai.LastImageCount);
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var q=await db.Queries.SingleAsync();var paths=QueryMedia.Paths(q);Assert.Equal(3,paths.Count);
        try { using var other=await app.Login("otherfarmer@test.local");using var officer=await app.Login("officer@test.local");for(int i=0;i<3;i++) {var url=$"/Media/QueryImage/{q.Id}?index={i}";var media=await c.GetAsync(url);Assert.Equal(HttpStatusCode.OK,media.StatusCode);using var decoded=SKBitmap.Decode(await media.Content.ReadAsByteArrayAsync());Assert.Equal(1600,decoded.Width);Assert.Equal(800,decoded.Height);Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(url)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await officer.GetAsync(url)).StatusCode);}
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/Media/QueryImage/{q.Id}?index=3")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/Media/QueryImage/{q.Id}?index=-1")).StatusCode);
        } finally {var uploads=scope.ServiceProvider.GetRequiredService<ImageUploadService>();foreach(var path in paths)File.Delete(Path.Combine(uploads.PrivateRoot,path[8..]));}
    }
    [Fact] public async Task TooManyPhotosAreRejectedBeforeAiOrDatabase() {
        using var app=new TestApp();await app.Seed();using var c=await app.Login();using var form=new MultipartFormDataContent();for(int i=0;i<21;i++)form.Add(new ByteArrayContent(new byte[]{1,2,3}),"images",$"leaf{i}.png");var request=new HttpRequestMessage(HttpMethod.Post,"/Chat/UploadImage"){Content=form};request.Headers.Add("RequestVerificationToken",await app.Token(c,"/Chat"));Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(request)).StatusCode);using var scope=app.Services.CreateScope();Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Queries.AnyAsync());Assert.Equal(0,app.Ai.LastImageCount);
    }

    [Fact] public async Task LongConversationPagesOlderTurnsAndBoundsAiContext() {
        using var app=new TestApp();await app.Seed();using var c=await app.Login();using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var root=new Digital_Krushi_Officer.Models.TblQuery {FarmerId=10,QueryText="Root question"};db.Queries.Add(root);await db.SaveChangesAsync();
        for(var i=0;i<54;i++)db.Queries.Add(new Digital_Krushi_Officer.Models.TblQuery {FarmerId=10,ParentQueryId=root.Id,QueryText="Follow up "+i});await db.SaveChangesAsync();
        var html=await c.GetStringAsync("/Chat?conversationId="+root.Id);Assert.Equal(50,System.Text.RegularExpressions.Regex.Matches(html,"class=\"thread-turn\"").Count);
        var before=await db.Queries.OrderBy(q=>q.Id).Skip(5).Select(q=>q.Id).FirstAsync();var older=await c.GetFromJsonAsync<JsonElement>($"/Chat/Messages?conversationId={root.Id}&beforeId={before}");Assert.Equal(5,older.GetProperty("messages").GetArrayLength());Assert.False(older.GetProperty("hasOlder").GetBoolean());
        await Send(app,c,"Newest follow up",root.Id);Assert.Equal(6,app.Ai.LastHistory.Count);Assert.Equal("Follow up 48",app.Ai.LastHistory[0].Question);Assert.Equal("Follow up 53",app.Ai.LastHistory[^1].Question);
    }
}

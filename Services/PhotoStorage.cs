using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Digital_Krushi_Officer.Services;

public interface IPhotoStorage
{
    Task PutAsync(string name, byte[] bytes, bool isPublic = false);
    Task<byte[]?> GetAsync(string name, bool isPublic = false);
    Task DeleteAsync(string name, bool isPublic = false);
}

public sealed class PhotoStorage(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory clients) : IPhotoStorage
{
    public bool IsCloud => string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase);

    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 150 || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_' && c != '.') || name.Contains(".."))
            throw new ArgumentException("Invalid photo name.");
    }

    private string LocalPath(string name, bool isPublic)
    {
        ValidateName(name);
        return Path.Combine(isPublic ? Path.Combine(env.WebRootPath, "uploads", "diseases") : Path.Combine(env.ContentRootPath, "App_Data", "uploads"), name);
    }

    private HttpRequestMessage Request(HttpMethod method, string name, bool isPublic)
    {
        ValidateName(name);
        var root = config["Storage:Supabase:Url"]?.TrimEnd('/');
        if (!Uri.TryCreate(root, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath != "/" || uri.Query != "" || uri.UserInfo != "")
            throw new InvalidOperationException("Configure an HTTPS Supabase project URL.");
        var bucket = config["Storage:Supabase:Bucket"] ?? "dko-media";
        ValidateName(bucket);
        var key = config["Storage:Supabase:ServiceRoleKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Configure the server-only Supabase service_role key.");
        var access = method == HttpMethod.Get ? "authenticated/" : "";
        var request = new HttpRequestMessage(method, $"{root}/storage/v1/object/{access}{Uri.EscapeDataString(bucket)}/{(isPublic ? "diseases" : "queries")}/{Uri.EscapeDataString(name)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("apikey", key);
        return request;
    }

    public async Task PutAsync(string name, byte[] bytes, bool isPublic = false)
    {
        if (!IsCloud) {
            var path = LocalPath(name, isPublic); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes); return;
        }
        using var request = Request(HttpMethod.Post, name, isPublic);
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentType(name));
        using var response = await clients.CreateClient("storage").SendAsync(request);
        EnsureSuccess(response);
    }

    public async Task<byte[]?> GetAsync(string name, bool isPublic = false)
    {
        if (!IsCloud) { var path = LocalPath(name, isPublic); return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null; }
        using var request = Request(HttpMethod.Get, name, isPublic);
        using var response = await clients.CreateClient("storage").SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        // Storage can wrap an object-not-found error in HTTP 400. Do not
        // treat other 400 responses (including authorization errors) as absent.
        if (response.StatusCode == HttpStatusCode.BadRequest) {
            try {
                await response.Content.LoadIntoBufferAsync(8192);
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (error.RootElement.TryGetProperty("code", out var code) &&
                    code.ValueKind == JsonValueKind.String && code.GetString() == "NoSuchKey") return null;
            } catch (JsonException) { }
        }
        EnsureSuccess(response);
        // Only normalized photos are stored here; bound reads even if bucket content was changed externally.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var bytes = new MemoryStream();
        var buffer = new byte[81920]; int read;
        while ((read = await stream.ReadAsync(buffer, deadline.Token)) > 0) {
            if (bytes.Length + read > ImageUploadService.MaximumBytes) throw new IOException("Stored photo exceeds the size limit.");
            bytes.Write(buffer, 0, read);
        }
        return bytes.ToArray();
    }

    public async Task DeleteAsync(string name, bool isPublic = false)
    {
        if (!IsCloud) { File.Delete(LocalPath(name, isPublic)); return; }
        // Supabase deletes objects through the bucket endpoint with a prefixes array.
        using var single = Request(HttpMethod.Delete, name, isPublic);
        var bucket = Uri.EscapeDataString(config["Storage:Supabase:Bucket"] ?? "dko-media");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{config["Storage:Supabase:Url"]!.TrimEnd('/')}/storage/v1/object/{bucket}");
        request.Headers.Authorization = single.Headers.Authorization;
        request.Headers.Add("apikey", config["Storage:Supabase:ServiceRoleKey"]);
        request.Content = JsonContent.Create(new { prefixes = new[] { $"{(isPublic ? "diseases" : "queries")}/{name}" } });
        using var response = await clients.CreateClient("storage").SendAsync(request);
        EnsureSuccess(response);
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Photo storage is temporarily unavailable.", null, response.StatusCode);
    }

    public static string ContentType(string name) => Path.GetExtension(name).ToLowerInvariant() switch {
        ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg"
    };
}

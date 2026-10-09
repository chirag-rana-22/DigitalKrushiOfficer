using SkiaSharp;
namespace Digital_Krushi_Officer.Services;
public record StoredImage(string FileName, byte[] Bytes);
public class ImageUploadService(IWebHostEnvironment env) {
    public const long MaximumBytes = 5 * 1024 * 1024;
    public string PrivateRoot => Path.Combine(env.ContentRootPath, "App_Data", "uploads");
    public async Task<StoredImage> SaveAsync(IFormFile image, bool isPublic = false) {
        if (image.Length == 0 || image.Length > MaximumBytes) throw new ArgumentException("Choose an image under 5 MB.");
        using var stream = new MemoryStream(); await image.CopyToAsync(stream); stream.Position = 0;
        using var codec = SKCodec.Create(stream);
        if (codec == null || codec.Info.Width < 32 || codec.Info.Height < 32 || (long)codec.Info.Width * codec.Info.Height > 20_000_000)
            throw new ArgumentException("Choose a valid photo between 32 pixels and 20 megapixels.");
        using var bitmap = SKBitmap.Decode(codec); if (bitmap == null) throw new ArgumentException("This image could not be decoded.");
        using var normalized = SKImage.FromBitmap(bitmap); using var encoded = normalized.Encode(SKEncodedImageFormat.Jpeg, 85);
        var bytes = encoded.ToArray(); var name = Guid.NewGuid().ToString("N") + ".jpg";
        var root = isPublic ? Path.Combine(env.WebRootPath, "uploads", "diseases") : PrivateRoot;
        Directory.CreateDirectory(root); await File.WriteAllBytesAsync(Path.Combine(root, name), bytes);
        return new(name, bytes);
    }
}

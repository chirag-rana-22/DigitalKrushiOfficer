using SkiaSharp;
namespace Digital_Krushi_Officer.Services;
public record StoredImage(string FileName, byte[] Bytes);
public class ImageUploadService(IWebHostEnvironment env, IPhotoStorage storage) {
    public const long MaximumBytes = 5 * 1024 * 1024;
    public const int MaximumImages = 20;
    public const long MaximumBatchBytes = 25 * 1024 * 1024;
    public Task DeleteAsync(string name, bool isPublic = false) => storage.DeleteAsync(name, isPublic);
    public string PrivateRoot => Path.Combine(env.ContentRootPath, "App_Data", "uploads");
    public async Task<StoredImage> SaveAsync(IFormFile image, bool isPublic = false) {
        if (image.Length == 0 || image.Length > MaximumBytes) throw new ArgumentException("Choose an image under 5 MB.");
        using var stream = new MemoryStream(); await image.CopyToAsync(stream); stream.Position = 0;
        using var codec = SKCodec.Create(stream);
        if (codec == null || codec.Info.Width < 32 || codec.Info.Height < 32 || (long)codec.Info.Width * codec.Info.Height > 20_000_000)
            throw new ArgumentException("Choose a valid photo between 32 pixels and 20 megapixels.");
        using var bitmap = SKBitmap.Decode(codec); if (bitmap == null) throw new ArgumentException("This image could not be decoded.");
        var scale=Math.Min(1d, 1600d/Math.Max(bitmap.Width,bitmap.Height));
        using var resized = scale < 1 ? bitmap.Resize(new SKImageInfo((int)(bitmap.Width*scale),(int)(bitmap.Height*scale)),new SKSamplingOptions(SKFilterMode.Linear)) : null;
        using var normalized = SKImage.FromBitmap(resized ?? bitmap); using var encoded = normalized.Encode(SKEncodedImageFormat.Jpeg, 85);
        var bytes = encoded.ToArray(); var name = Guid.NewGuid().ToString("N") + ".jpg";
        await storage.PutAsync(name, bytes, isPublic);
        return new(name, bytes);
    }
}

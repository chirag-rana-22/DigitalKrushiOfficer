using Digital_Krushi_Officer.Data;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Services;

public static class MediaMigration
{
    // Run on the source computer with its local DB and cloud Storage settings. Does not change rows or delete source files.
    public static async Task CopyAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Media copy requires Storage:Provider=Supabase.");
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IPhotoStorage>();
        var files = new Dictionary<(string Name, bool Public), string>();
        foreach (var query in await db.Queries.AsNoTracking().ToListAsync()) {
            foreach (var path in QueryMedia.Paths(query)) {
                var name = Path.GetFileName(path.StartsWith("private:") ? path[8..] : path);
                PhotoStorage.ValidateName(name);
                if (!path.StartsWith("private:") && !path.StartsWith("/uploads/crops/")) throw new InvalidOperationException("Unsupported legacy query photo path. Review before migration.");
                var source = path.StartsWith("private:") ? Path.Combine(env.ContentRootPath, "App_Data", "uploads", name) : Path.Combine(env.WebRootPath, "uploads", "crops", name);
                if (files.TryGetValue((name, false), out var existing) && existing != source) throw new InvalidOperationException("Duplicate media filename requires review before migration.");
                files[(name, false)] = source;
            }
        }
        foreach (var path in await db.DiseaseDatasets.AsNoTracking().Where(d => d.ReferenceImagePath != null).Select(d => d.ReferenceImagePath!).ToListAsync()) {
            if (!path.StartsWith("/uploads/diseases/")) continue;
            var name = Path.GetFileName(path); PhotoStorage.ValidateName(name);
            files[(name, true)] = Path.Combine(env.WebRootPath, "uploads", "diseases", name);
        }
        // Preflight the entire source set before any remote write.
        foreach (var source in files.Values) {
            var info = new FileInfo(source);
            if (!info.Exists || info.Length == 0 || info.Length > ImageUploadService.MaximumBytes)
                throw new InvalidOperationException($"Missing or oversized source photo: {info.Name}. Nothing has been copied in this run.");
        }
        var copied = 0; var skipped = 0;
        foreach (var entry in files) {
            var bytes = await File.ReadAllBytesAsync(entry.Value);
            var remote = await storage.GetAsync(entry.Key.Name, entry.Key.Public);
            if (remote != null) {
                if (!bytes.AsSpan().SequenceEqual(remote)) throw new InvalidOperationException("Cloud photo has different bytes; it will not be overwritten.");
                skipped++; continue;
            }
            await storage.PutAsync(entry.Key.Name, bytes, entry.Key.Public);
            var verified = await storage.GetAsync(entry.Key.Name, entry.Key.Public);
            if (verified == null || !bytes.AsSpan().SequenceEqual(verified)) throw new IOException("Cloud photo verification failed.");
            copied++;
        }
        Console.WriteLine($"Media verified: {copied} copied, {skipped} already identical. Source data unchanged.");
    }
}

using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Digital_Krushi_Officer.Controllers;
[Authorize]
public class MediaController(AppDbContext db, IWebHostEnvironment env, ImageUploadService uploads, IPhotoStorage storage) : Controller {
    [AllowAnonymous, HttpGet("/uploads/diseases/{name}")]
    public async Task<IActionResult> DiseaseImage(string name) {
        try { PhotoStorage.ValidateName(name); } catch (ArgumentException) { return NotFound(); }
        if (!await db.DiseaseDatasets.AnyAsync(d => d.IsActive && d.ReferenceImagePath == "/uploads/diseases/" + name)) return NotFound();
        var bytes = await storage.GetAsync(name, true);
        return bytes == null ? NotFound() : File(bytes, PhotoStorage.ContentType(name));
    }
    public async Task<IActionResult> QueryImage(int id, int index = 0) {
        var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query=await db.Queries.AsNoTracking().Include(q=>q.Escalation).SingleOrDefaultAsync(q=>q.Id==id);
        if(query==null) return NotFound();
        var root=query.ParentQueryId??query.Id;
        var assignedContext=User.IsInRole("Officer") && await db.Queries.AnyAsync(q=>q.FarmerId==query.FarmerId && q.Id>=query.Id && (q.Id==root || q.ParentQueryId==root) && q.Escalation!=null && q.Escalation.OfficerId==userId);
        if (!(User.IsInRole("Admin") || query.FarmerId==userId || assignedContext)) return NotFound();
        var paths=QueryMedia.Paths(query);
        if(index<0 || index>=paths.Count) return NotFound();
        var path=paths[index];
        if (path.StartsWith("private:")) {
            var bytes = await storage.GetAsync(Path.GetFileName(path[8..]));
            if (bytes == null) return NotFound();
            Response.Headers.CacheControl = "private, no-store";
            return File(bytes, "image/jpeg");
        }
        var file=path.StartsWith("private:") ? Path.Combine(uploads.PrivateRoot,Path.GetFileName(path[8..])) : path.StartsWith("/uploads/crops/") ? Path.Combine(env.WebRootPath,"uploads","crops",Path.GetFileName(path)) : null;
        if (file==null) return NotFound();
        if (!System.IO.File.Exists(file)) {
            var legacy = await storage.GetAsync(Path.GetFileName(file));
            if (legacy == null) return NotFound();
            Response.Headers.CacheControl="private, no-store";
            return File(legacy, PhotoStorage.ContentType(file));
        }
        Response.Headers.CacheControl="private, no-store";
        var mime=Path.GetExtension(file).ToLowerInvariant() switch { ".png"=>"image/png", ".webp"=>"image/webp", _=>"image/jpeg" };
        return PhysicalFile(file,mime);
    }
}

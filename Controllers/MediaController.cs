using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Digital_Krushi_Officer.Controllers;
[Authorize]
public class MediaController(AppDbContext db, IWebHostEnvironment env, ImageUploadService uploads) : Controller {
    public async Task<IActionResult> QueryImage(int id) {
        var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query=await db.Queries.AsNoTracking().Include(q=>q.Escalation).SingleOrDefaultAsync(q=>q.Id==id);
        if (query?.ImagePath==null || !(User.IsInRole("Admin") || query.FarmerId==userId || (User.IsInRole("Officer") && query.Escalation?.OfficerId==userId))) return NotFound();
        var path=query.ImagePath;
        var file=path.StartsWith("private:") ? Path.Combine(uploads.PrivateRoot,Path.GetFileName(path[8..])) : path.StartsWith("/uploads/crops/") ? Path.Combine(env.WebRootPath,"uploads","crops",Path.GetFileName(path)) : null;
        if (file==null || !System.IO.File.Exists(file)) return NotFound();
        Response.Headers.CacheControl="private, no-store";
        var mime=Path.GetExtension(file).ToLowerInvariant() switch { ".png"=>"image/png", ".webp"=>"image/webp", _=>"image/jpeg" };
        return PhysicalFile(file,mime);
    }
}

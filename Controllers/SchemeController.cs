using Digital_Krushi_Officer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Controllers
{
    [Authorize]
    public class SchemeController : Controller
    {
        private readonly AppDbContext _db;
        public SchemeController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? category)
        {
            var q = _db.GovernmentSchemes.Where(s => s.IsActive && s.Status == "Active" && (s.LastDate == null || s.LastDate >= DateTime.Today));
            if (!string.IsNullOrEmpty(category))
                q = q.Where(s => s.Category == category);
            ViewBag.Category = category;
            ViewBag.Categories = await _db.GovernmentSchemes
                .Where(s => s.IsActive).Select(s => s.Category).Distinct().ToListAsync();
            return View(await q.OrderBy(s => s.SchemeName).ToListAsync());
        }

        public async Task<IActionResult> Detail(int id)
        {
            var scheme = await _db.GovernmentSchemes.FirstOrDefaultAsync(s => s.Id == id && s.IsActive && s.Status == "Active" && (s.LastDate == null || s.LastDate >= DateTime.Today));
            if (scheme == null) return NotFound();
            return View(scheme);
        }
    }
}

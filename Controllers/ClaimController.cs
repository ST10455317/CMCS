
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

       
        public async Task<IActionResult> Index(string lecturerName)
        {
            var claims = string.IsNullOrEmpty(lecturerName)
                ? await _db.Claims.OrderByDescending(c => c.SubmittedAt).ToListAsync()
                : await _db.Claims.Where(c => c.LecturerName == lecturerName)
                                  .OrderByDescending(c => c.SubmittedAt).ToListAsync();

            ViewBag.LecturerName = lecturerName;
            return View(claims);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LecturerName,HoursWorked,HourlyRate,Notes")] Claim claim, IFormFile upload)
        {
            if (!ModelState.IsValid) return View(claim);

           
            if (upload != null && upload.Length > 0)
            {
                var allowed = new[] { ".pdf", ".docx", ".xlsx" };
                var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext) || upload.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("UploadedFileName", "File type or size not allowed.");
                    return View(claim);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var unique = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, unique);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }
                claim.UploadedFileName = unique;
            }

            claim.SubmittedAt = DateTime.UtcNow;
            claim.Status = ClaimStatus.Pending;

            _db.Add(claim);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { lecturerName = claim.LecturerName });
        }

       
        public async Task<IActionResult> Review()
        {
            var pending = await _db.Claims.Where(c => c.Status == ClaimStatus.Pending)
                                        .OrderBy(c => c.SubmittedAt).ToListAsync();
            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Approved;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Review));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Rejected;
            claim.Notes = (claim.Notes ?? "") + "\nManager note: " + reason;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Review));
        }

       
        public IActionResult Download(string file)
        {
            if (string.IsNullOrEmpty(file)) return NotFound();
            var path = Path.Combine(_env.WebRootPath, "uploads", file);
            if (!System.IO.File.Exists(path)) return NotFound();
            var contentType = "application/octet-stream";
            return PhysicalFile(path, contentType, Path.GetFileName(path));
        }
    }
}

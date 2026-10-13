using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVault.Web.Data;
using SafeVault.Web.Models;

namespace SafeVault.Web.Controllers;

[Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class VaultController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        if (q?.Length > 100)
        {
            ViewData["SearchError"] = "Search text must be 100 characters or fewer.";
            q = null;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Owner scoping is applied before search. EF Core parameterizes values supplied to Contains.
        var query = db.VaultNotes.AsNoTracking().Where(note => note.OwnerId == userId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim();
            query = query.Where(note => note.Title.Contains(searchTerm) || note.Content.Contains(searchTerm));
        }

        var notes = await query.OrderByDescending(note => note.CreatedUtc).ToListAsync();
        return View(new VaultIndexViewModel { SearchTerm = q, Notes = notes });
    }

    [HttpGet]
    public IActionResult Create() => View(new VaultNoteInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VaultNoteInputModel input)
    {
        NormalizeAndValidate(input);
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var note = new VaultNote
        {
            Title = input.Title,
            Content = input.Content,
            OwnerId = userId,
            CreatedUtc = DateTime.UtcNow
        };
        db.VaultNotes.Add(note);
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Note created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await db.VaultNotes.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.OwnerId == userId);
        if (note is null)
        {
            return NotFound();
        }

        return View(new VaultNoteInputModel { Title = note.Title, Content = note.Content });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VaultNoteInputModel input)
    {
        NormalizeAndValidate(input);
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await db.VaultNotes.FirstOrDefaultAsync(item => item.Id == id && item.OwnerId == userId);
        if (note is null)
        {
            return NotFound();
        }

        note.Title = input.Title;
        note.Content = input.Content;
        note.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Note updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await db.VaultNotes.FirstOrDefaultAsync(item => item.Id == id && item.OwnerId == userId);
        if (note is null)
        {
            return NotFound();
        }

        db.VaultNotes.Remove(note);
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Note deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void NormalizeAndValidate(VaultNoteInputModel input)
    {
        input.Title = input.Title?.Trim() ?? string.Empty;
        input.Content = input.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            ModelState.AddModelError(nameof(input.Title), "A title is required.");
        }
        if (string.IsNullOrWhiteSpace(input.Content))
        {
            ModelState.AddModelError(nameof(input.Content), "Content is required.");
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVault.Web.Data;
using SafeVault.Web.Models;

namespace SafeVault.Web.Controllers.Api;

[ApiController]
[Route("api/vaultnotes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class VaultNotesApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VaultNoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery, StringLength(100)] string? q = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Every query is owner-scoped. The search value is a LINQ parameter, not SQL text.
        var query = db.VaultNotes.AsNoTracking().Where(note => note.OwnerId == userId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim();
            query = query.Where(note => note.Title.Contains(searchTerm) || note.Content.Contains(searchTerm));
        }

        var items = await query.OrderByDescending(note => note.CreatedUtc)
            .Select(note => new VaultNoteDto(note.Id, note.Title, note.Content, note.CreatedUtc, note.UpdatedUtc))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VaultNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await db.VaultNotes.AsNoTracking()
            .Where(item => item.Id == id && item.OwnerId == userId)
            .Select(item => new VaultNoteDto(item.Id, item.Title, item.Content, item.CreatedUtc, item.UpdatedUtc))
            .FirstOrDefaultAsync();

        return note is null ? NotFound() : Ok(note);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VaultNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(VaultNoteInputModel input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var title = input.Title.Trim();
        var content = input.Content.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(string.IsNullOrWhiteSpace(title) ? nameof(input.Title) : nameof(input.Content),
                "The value must not be empty or whitespace.");
            return ValidationProblem(ModelState);
        }

        var note = new VaultNote
        {
            Title = title,
            Content = content,
            OwnerId = userId,
            CreatedUtc = DateTime.UtcNow
        };
        db.VaultNotes.Add(note);
        await db.SaveChangesAsync();

        var dto = new VaultNoteDto(note.Id, note.Title, note.Content, note.CreatedUtc, note.UpdatedUtc);
        return CreatedAtAction(nameof(GetById), new { id = note.Id }, dto);
    }
}

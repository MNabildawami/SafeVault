using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVault.Web.Data;
using SafeVault.Web.Models;

namespace SafeVault.Web.Controllers;

[Authorize(Policy = "CanViewAdminDashboard")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel(
            UserCount: await userManager.Users.CountAsync(),
            NoteCount: await db.VaultNotes.CountAsync());
        return View(model);
    }
}

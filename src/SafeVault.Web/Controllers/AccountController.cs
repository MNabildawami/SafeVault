using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeVault.Web.Models;

namespace SafeVault.Web.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult.Errors);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "The account could not be configured. Please try again.");
            return View(model);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Vault");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Vault");
        }

        ModelState.AddModelError(string.Empty,
            result.IsLockedOut
                ? "The account is temporarily locked. Try again later."
                : "Invalid sign-in attempt.");
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private void AddIdentityErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            // Avoid leaking internal implementation details from Identity to the browser.
            ModelState.AddModelError(string.Empty, error.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" => "An account with this email already exists.",
                _ => "The account could not be created. Check the input requirements and try again."
            });
        }
    }
}

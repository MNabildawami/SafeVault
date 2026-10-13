using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafeVault.Web.Controllers;
using SafeVault.Web.Data;
using SafeVault.Web.Models;
using SafeVault.Web.Services;
using Xunit;

namespace SafeVault.Tests;

public sealed class SecurityIntegrationTests
{
    [Fact]
    public async Task JwtApiRejectsAnonymousRequests()
    {
        using var factory = new SafeVaultFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync("/api/vaultnotes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var mvcResponse = await client.GetAsync("/Vault");
        Assert.Equal(HttpStatusCode.Redirect, mvcResponse.StatusCode);
        Assert.Contains("/Account/Login", mvcResponse.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Fact]
    public async Task JwtApiRejectsMalformedTokens()
    {
        using var factory = new SafeVaultFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var response = await client.GetAsync("/api/vaultnotes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AdminControllerRequiresAdminPolicy()
    {
        var authorization = typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorization);
        Assert.Equal("CanViewAdminDashboard", authorization!.Policy);
    }

    [Fact]
    public async Task IdentityStoresAHashRatherThanTheSubmittedPassword()
    {
        using var factory = new SafeVaultFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        _ = await client.GetAsync("/health"); // Starts the host and initializes the test database.

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string password = "Strong!TestPassword123";
        var user = new ApplicationUser { UserName = "hash-check@example.test", Email = "hash-check@example.test" };
        var result = await users.CreateAsync(user, password);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));
        var stored = await users.FindByIdAsync(user.Id);
        Assert.NotNull(stored?.PasswordHash);
        Assert.NotEqual(password, stored!.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(stored, password));
    }

    [Fact]
    public async Task RazorViewsEncodeUserSuppliedHtmlInsteadOfExecutingIt()
    {
        using var factory = new SafeVaultFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var registerPage = await client.GetStringAsync("/Account/Register");
        var registerToken = ExtractAntiforgeryToken(registerPage);
        var registration = new Dictionary<string, string>
        {
            ["Email"] = "xss-check@example.test",
            ["Password"] = "Strong!TestPassword123",
            ["ConfirmPassword"] = "Strong!TestPassword123",
            ["__RequestVerificationToken"] = registerToken
        };
        var registerResponse = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(registration));
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var createPage = await client.GetStringAsync("/Vault/Create");
        var createToken = ExtractAntiforgeryToken(createPage);
        var maliciousContent = "<script>alert('xss')</script>";
        var createResponse = await client.PostAsync("/Vault/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Encoding check",
            ["Content"] = maliciousContent,
            ["__RequestVerificationToken"] = createToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var vaultHtml = await client.GetStringAsync("/Vault");
        Assert.Contains("&lt;script&gt;", vaultHtml);
        Assert.DoesNotContain(maliciousContent, vaultHtml);
    }

    [Fact]
    public async Task SqlInjectionTextIsHandledAsSearchInput()
    {
        using var factory = new SafeVaultFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        _ = await client.GetAsync("/health");

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new ApplicationUser { UserName = "api-check@example.test", Email = "api-check@example.test" };
        var createResult = await users.CreateAsync(user, "Strong!TestPassword123");
        Assert.True(createResult.Succeeded);
        await users.AddToRoleAsync(user, "User");
        db.VaultNotes.Add(new VaultNote
        {
            Title = "Confidential account record",
            Content = "Only this owner should see this note.",
            OwnerId = user.Id
        });
        await db.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var token = await tokenService.CreateTokenAsync(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/vaultnotes?q=%27%20OR%201%3D1%20--");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public void VaultNoteValidationRejectsOversizedAndBlankValues()
    {
        var model = new VaultNoteInputModel
        {
            Title = new string('x', 121),
            Content = "valid content"
        };
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model,
            new System.ComponentModel.DataAnnotations.ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(VaultNoteInputModel.Title)));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.CultureInvariant);
        Assert.True(match.Success, "The form did not contain an anti-forgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}

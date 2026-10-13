using System.ComponentModel.DataAnnotations;

namespace SafeVault.Web.Models;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresInSeconds);

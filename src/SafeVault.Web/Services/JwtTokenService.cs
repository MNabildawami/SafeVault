using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SafeVault.Web.Models;

namespace SafeVault.Web.Services;

public sealed class JwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;

    public const int TokenLifetimeMinutes = 15;

    public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
        _issuer = configuration["Jwt:Issuer"] ?? "SafeVault";
        _audience = configuration["Jwt:Audience"] ?? "SafeVault.Client";
        var keyValue = configuration["Jwt:Key"] ?? configuration["SAFEVAULT_JWT_KEY"];
        if (string.IsNullOrWhiteSpace(keyValue) || Encoding.UTF8.GetByteCount(keyValue) < 32)
        {
            throw new InvalidOperationException("A JWT signing secret with at least 32 bytes is required.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public async Task<string> CreateTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(TokenLifetimeMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace BlogApi.Auth;

/// <summary>Strongly-typed JWT settings bound from the "Jwt" config section.</summary>
public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "BlogApi";
    public string Audience { get; set; } = "BlogApi";
    public int ExpiryMinutes { get; set; } = 60;
}

/// <summary>
/// Issues signed JWT access tokens.
///
/// LESSON — a JWT is signed, not encrypted.
/// Anyone can READ the claims inside a JWT (it's just Base64). The signature only
/// proves it hasn't been TAMPERED with and that WE issued it. So: never put
/// secrets in a token, and always validate the signature on the way back in
/// (configured in Program.cs).
/// </summary>
public sealed class JwtTokenService(JwtOptions options)
{
    public string CreateToken(User user)
    {
        // Claims describe WHO the caller is. "sub" = subject (the user id).
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

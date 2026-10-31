using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcoEast.API.Models;
using Microsoft.IdentityModel.Tokens;
namespace EcoEast.API.Services;
public class JwtTokenService(IConfiguration config)
{
    public string Create(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.Email, user.Email ?? "")
        };
        if (user.PersonId.HasValue) claims.Add(new Claim("PersonId", user.PersonId.Value.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

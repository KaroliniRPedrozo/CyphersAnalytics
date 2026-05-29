using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using api.models;

namespace api.services
{
    public class AuthService
    {
        private readonly string _secret;

        public AuthService()
        {
            _secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                      ?? "chave-padrao-dev";
        }

        public string GerarToken(Usuario usuario)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("gameName", usuario.GameName),
                new Claim("tagLine", usuario.TagLine)
            };

            var token = new JwtSecurityToken(
                issuer:            "CyphersAnalytics",
                audience:          "CyphersAnalytics",
                claims:            claims,
                expires:           DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
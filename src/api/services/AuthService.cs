using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using api.models;

namespace api.services
{
    public record JwtSettings(string Secret);

    public class AuthService
    {
        public const string Issuer = "CyphersAnalytics";
        public const string Audience = "CyphersAnalytics";
        // Audience propria: um "state" do RSO nunca e aceito como token de login.
        private const string AudienceRsoState = "CyphersAnalytics.RsoState";

        private readonly SymmetricSecurityKey _key;

        public AuthService(JwtSettings settings)
        {
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        }

        public string GerarToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("gameName", usuario.GameName),
                new Claim("tagLine", usuario.TagLine)
            };

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience, claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Token curto e assinado usado como parametro "state" do OAuth (protecao contra CSRF).</summary>
        public string GerarStateRso(int usuarioId)
        {
            var token = new JwtSecurityToken(
                issuer: Issuer, audience: AudienceRsoState,
                claims: new[] { new Claim("uid", usuarioId.ToString()), new Claim("nonce", Guid.NewGuid().ToString("N")) },
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int? ValidarStateRso(string? state)
        {
            if (string.IsNullOrWhiteSpace(state)) return null;
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(state, new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = Issuer,
                    ValidateAudience = true, ValidAudience = AudienceRsoState,
                    ValidateLifetime = true, ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _key, ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);

                return int.TryParse(principal.FindFirst("uid")?.Value, out var id) ? id : null;
            }
            catch { return null; }
        }
    }
}

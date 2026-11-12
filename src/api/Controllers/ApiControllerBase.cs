using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /// <summary>Helpers para ler a identidade do usuario autenticado a partir do JWT.</summary>
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int? UsuarioId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        protected string? EmailDoToken => User.FindFirstValue(ClaimTypes.Email);
        protected string GameNameDoToken => User.FindFirstValue("gameName") ?? "";
        protected string TagLineDoToken => User.FindFirstValue("tagLine") ?? "";

        protected bool EhOProprioJogador(string gameName, string tagLine) =>
            string.Equals(gameName, GameNameDoToken, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(tagLine.TrimStart('#'), TagLineDoToken, StringComparison.OrdinalIgnoreCase);

        protected ObjectResult Indisponivel(api.services.UpstreamUnavailableException ex)
        {
            var segundos = (int)Math.Ceiling((ex.RetryAfter ?? TimeSpan.FromSeconds(30)).TotalSeconds);
            Response.Headers["Retry-After"] = segundos.ToString();
            return StatusCode(503, new
            {
                erro = "A API da Riot esta temporariamente indisponivel ou limitando requisicoes. Tente novamente em instantes.",
                tentarNovamenteEmSegundos = segundos
            });
        }
    }
}

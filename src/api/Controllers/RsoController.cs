using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.services;
using Asp.Versioning;

namespace api.Controllers
{
    /// <summary>
    /// Riot Sign On (OAuth 2.0 / OpenID). So funciona depois que a Riot aprova a chave de producao e
    /// entrega o client RSO (RSO_ENABLED=true). Ate la, /status informa que o recurso esta pendente
    /// e o app usa apenas o consentimento registrado no cadastro.
    ///
    /// Fluxo: POST /rso/iniciar (autenticado) -> usuario vai para auth.riotgames.com ->
    ///        GET /rso/callback?code&amp;state -> troca o code por token -> /accounts/me -> vincula puuid.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RsoController : ApiControllerBase
    {
        private const string AuthorizeUrl = "https://auth.riotgames.com/authorize";
        private const string TokenUrl     = "https://auth.riotgames.com/token";

        private readonly AppDbContext _context;
        private readonly AuthService _auth;
        private readonly RsoOptions _rso;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<RsoController> _logger;

        public RsoController(AppDbContext context, AuthService auth, RsoOptions rso,
                             IHttpClientFactory httpFactory, ILogger<RsoController> logger)
        {
            _context = context;
            _auth = auth;
            _rso = rso;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("status")]
        public IActionResult Status() => Ok(new { habilitado = _rso.Enabled });

        // O token JWT fica no localStorage e nao vai em navegacao de pagina: por isso o frontend chama este
        // endpoint autenticado, recebe a URL e so entao redireciona o navegador.
        [Authorize]
        [HttpPost("iniciar")]
        public IActionResult Iniciar()
        {
            if (!_rso.Enabled)
                return StatusCode(503, new
                {
                    codigo = "rso_indisponivel",
                    erro = "A vinculacao com a conta Riot ainda nao esta disponivel (aguardando aprovacao da Riot)."
                });

            var state = _auth.GerarStateRso(UsuarioId!.Value);
            var url = $"{AuthorizeUrl}?client_id={Uri.EscapeDataString(_rso.ClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(_rso.RedirectUri)}" +
                      $"&response_type=code&scope=openid&state={Uri.EscapeDataString(state)}";
            return Ok(new { url });
        }

        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            if (!_rso.Enabled) return Redirecionar("indisponivel");
            if (!string.IsNullOrEmpty(error)) return Redirecionar("negado");

            var usuarioId = _auth.ValidarStateRso(state);
            if (usuarioId == null || string.IsNullOrEmpty(code)) return Redirecionar("erro");

            try
            {
                var http = _httpFactory.CreateClient("rso");

                // 1) code -> access_token (client_secret_basic)
                using var tokenReq = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "authorization_code",
                        ["code"] = code,
                        ["redirect_uri"] = _rso.RedirectUri
                    })
                };
                tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_rso.ClientId}:{_rso.ClientSecret}")));

                using var tokenResp = await http.SendAsync(tokenReq);
                if (!tokenResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("RSO: troca de code falhou ({Status})", (int)tokenResp.StatusCode);
                    return Redirecionar("erro");
                }
                var accessToken = JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync())
                    .RootElement.GetProperty("access_token").GetString();

                // 2) access_token -> conta Riot do proprio jogador
                using var meReq = new HttpRequestMessage(HttpMethod.Get,
                    $"https://{_rso.RegionalHost}.api.riotgames.com/riot/account/v1/accounts/me");
                meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using var meResp = await http.SendAsync(meReq);
                if (!meResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("RSO: accounts/me falhou ({Status})", (int)meResp.StatusCode);
                    return Redirecionar("erro");
                }
                var me = JsonDocument.Parse(await meResp.Content.ReadAsStringAsync()).RootElement;
                var puuid = me.GetProperty("puuid").GetString()!;

                // 3) vincula (uma conta Riot so pode pertencer a um usuario)
                if (await _context.Usuarios.AnyAsync(u => u.RsoPuuid == puuid && u.Id != usuarioId))
                    return Redirecionar("conta_ja_vinculada");

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
                if (usuario == null) return Redirecionar("erro");

                usuario.RsoPuuid = puuid;
                usuario.RsoVerificadoEm = DateTime.UtcNow;
                usuario.ConsentimentoEm ??= DateTime.UtcNow; // o opt-in via RSO e, por si so, o consentimento
                if (me.TryGetProperty("gameName", out var gn) && gn.ValueKind == JsonValueKind.String) usuario.GameName = gn.GetString()!;
                if (me.TryGetProperty("tagLine",  out var tl) && tl.ValueKind == JsonValueKind.String) usuario.TagLine  = tl.GetString()!;

                await _context.SaveChangesAsync();
                _logger.LogInformation("RSO: conta Riot vinculada ao usuario {UsuarioId}", usuario.Id);
                return Redirecionar("ok");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RSO: erro no callback");
                return Redirecionar("erro");
            }
        }

        private RedirectResult Redirecionar(string resultado)
            => Redirect($"{_rso.FrontendUrl.TrimEnd('/')}/conta?rso={resultado}");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.models;
using api.services;
using Asp.Versioning;

namespace api.Controllers
{
    /// <summary>
    /// Dados de jogador: somente o PROPRIO usuario autenticado pode ve-los, e apenas depois de
    /// consentir (e, quando o RSO estiver habilitado, de vincular a conta Riot). Isso atende a regra da Riot
    /// de que jogadores que nao deram opt-in nao podem ter dados expostos a terceiros.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class JogadoresController : ApiControllerBase
    {
        private static readonly TimeSpan CacheDoJogador  = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CooldownManual  = TimeSpan.FromMinutes(2);

        private readonly AppDbContext _context;
        private readonly IValorantDataProvider _provider;
        private readonly JogadorSyncService _sync;
        private readonly RsoOptions _rso;
        private readonly ILogger<JogadoresController> _logger;

        public JogadoresController(AppDbContext context, IValorantDataProvider provider, JogadorSyncService sync,
                                   RsoOptions rso, ILogger<JogadoresController> logger)
        {
            _context = context;
            _provider = provider;
            _sync = sync;
            _rso = rso;
            _logger = logger;
        }

        /// <summary>Retorna null se o acesso e permitido; caso contrario, a resposta de erro.</summary>
        private async Task<IActionResult?> VerificarAcessoAsync(string gameName, string tagLine)
        {
            var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UsuarioId);
            if (usuario == null) return Unauthorized(new { erro = "Sessao invalida. Faca login novamente." });

            var proprio = string.Equals(gameName, usuario.GameName, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(tagLine.TrimStart('#'), usuario.TagLine, StringComparison.OrdinalIgnoreCase);
            if (!proprio)
                return StatusCode(403, new { codigo = "somente_proprio_jogador", erro = "Voce so pode consultar os seus proprios dados." });

            if (usuario.ConsentimentoEm == null)
                return StatusCode(403, new { codigo = "consentimento_necessario", erro = "Autorize o uso dos seus dados de jogo para continuar." });

            if (_rso.Enabled && usuario.RsoVerificadoEm == null)
                return StatusCode(403, new { codigo = "rso_necessario", erro = "Vincule sua conta Riot (Riot Sign On) para ver seus dados." });

            return null;
        }

        // Dado agregado/publico: nao identifica jogadores.
        [AllowAnonymous]
        [HttpGet("temporada")]
        public async Task<IActionResult> TemporadaAtual()
        {
            var (season, act) = await _provider.BuscarTemporadaAtual();
            return Ok(new { season, act });
        }

        [HttpGet("{gameName}/{tagLine}")]
        public async Task<IActionResult> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                if (await VerificarAcessoAsync(gameName, tagLine) is { } negado) return negado;

                var nome = gameName.ToLower(); var tag = tagLine.ToLower();
                var jogador = await _context.Jogadores.FirstOrDefaultAsync(j => j.GameName.ToLower() == nome && j.TagLine.ToLower() == tag);

                var desatualizado = jogador == null || DateTime.UtcNow - jogador.UltimaAtualizacao >= CacheDoJogador;
                var dadosAntigos = false;

                if (desatualizado)
                {
                    try
                    {
                        var (sincronizado, _) = await _sync.SincronizarAsync(gameName, tagLine, jogador, UsuarioId, HttpContext.RequestAborted);
                        if (sincronizado == null)
                            return NotFound(new { erro = "Jogador nao encontrado. Verifique o nome e a tag." });
                        jogador = sincronizado;
                    }
                    catch (UpstreamUnavailableException ex) when (jogador != null)
                    {
                        // Degradacao graciosa: serve o ultimo cache em vez de falhar.
                        _logger.LogWarning(ex, "API externa indisponivel; servindo cache de {GameName}", gameName);
                        dadosAntigos = true;
                    }
                }

                var partidas = await _context.Partidas.Where(p => p.Puuid == jogador!.Puuid).ToListAsync();

                var melhorMapa   = partidas.GroupBy(p => p.Mapa).OrderByDescending(g => g.Average(p => p.Kda)).FirstOrDefault()?.Key;
                var melhorAgente = partidas.GroupBy(p => p.Agente).OrderByDescending(g => g.Average(p => p.Kda)).FirstOrDefault()?.Key;
                var kdaGeral     = partidas.Any() ? Math.Round(partidas.Average(p => p.Kda), 2) : 0;
                var taxaVitoria  = partidas.Any()
                    ? $"{Math.Round((double)partidas.Count(p => p.Resultado == "Vitoria") / partidas.Count * 100, 1)}%"
                    : "0%";

                if (dadosAntigos) Response.Headers["X-Data-Stale"] = "true";

                return Ok(new
                {
                    jogador!.GameName, jogador.TagLine, jogador.RankAtual, jogador.RankImagem,
                    TotalPartidas = partidas.Count, MelhorMapa = melhorMapa, MelhorAgente = melhorAgente,
                    KdaGeral = kdaGeral, TaxaVitoria = taxaVitoria,
                    AtualizadoEm = jogador.UltimaAtualizacao, DadosDesatualizados = dadosAntigos
                });
            }
            catch (UpstreamUnavailableException ex) { return Indisponivel(ex); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jogador {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // Atualizacao manual, com intervalo minimo para nao martelar a API da Riot.
        [HttpPut("{gameName}/{tagLine}")]
        public async Task<IActionResult> AtualizarJogador(string gameName, string tagLine)
        {
            try
            {
                if (await VerificarAcessoAsync(gameName, tagLine) is { } negado) return negado;

                var nome = gameName.ToLower(); var tag = tagLine.ToLower();
                var jogador = await _context.Jogadores.FirstOrDefaultAsync(j => j.GameName.ToLower() == nome && j.TagLine.ToLower() == tag);
                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var restante = CooldownManual - (DateTime.UtcNow - jogador.UltimaAtualizacao);
                if (restante > TimeSpan.Zero)
                {
                    var s = (int)Math.Ceiling(restante.TotalSeconds);
                    Response.Headers["Retry-After"] = s.ToString();
                    return StatusCode(429, new { erro = $"Seus dados foram atualizados ha pouco. Aguarde {s}s para atualizar de novo." });
                }

                var (atualizado, novas) = await _sync.SincronizarAsync(gameName, tagLine, jogador, UsuarioId, HttpContext.RequestAborted);
                if (atualizado == null)
                    return BadRequest(new { erro = "Nao foi possivel buscar dados atualizados na API." });

                return Ok(new
                {
                    atualizado.GameName, atualizado.TagLine, atualizado.RankAtual, atualizado.RankImagem,
                    atualizado.UltimaAtualizacao, NovasPartidas = novas, mensagem = "Dados atualizados com sucesso!"
                });
            }
            catch (UpstreamUnavailableException ex) { return Indisponivel(ex); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar jogador {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        [HttpGet("{gameName}/{tagLine}/historico")]
        public async Task<IActionResult> Historico(string gameName, string tagLine, int page = 1, int size = 10)
        {
            try
            {
                if (await VerificarAcessoAsync(gameName, tagLine) is { } negado) return negado;

                page = Math.Max(1, page);
                size = Math.Clamp(size, 1, 50);

                var jogador = await BuscarNoBanco(gameName, tagLine);
                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var consulta = _context.Partidas.Where(p => p.Puuid == jogador.Puuid);
                var total = await consulta.CountAsync();
                var partidas = await consulta.OrderByDescending(p => p.DataPartida)
                    .Skip((page - 1) * size).Take(size).ToListAsync();

                return Ok(new { Page = page, Size = size, Total = total, TotalPaginas = (int)Math.Ceiling((double)total / size), Partidas = partidas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar historico de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        [HttpGet("{gameName}/{tagLine}/estatisticas")]
        public async Task<IActionResult> Estatisticas(string gameName, string tagLine)
        {
            try
            {
                if (await VerificarAcessoAsync(gameName, tagLine) is { } negado) return negado;

                var jogador = await BuscarNoBanco(gameName, tagLine);
                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var partidas = await _context.Partidas.Where(p => p.Puuid == jogador.Puuid).ToListAsync();
                if (!partidas.Any()) return Ok(new { mensagem = "Nenhuma partida encontrada." });

                static object Agrupar<T>(IEnumerable<IGrouping<T, Partida>> grupos, string campo) =>
                    grupos.Select(g => new Dictionary<string, object?>
                    {
                        // Chaves em camelCase para manter o mesmo contrato JSON de antes (objetos anonimos eram camelCase).
                        [campo]         = g.Key,
                        ["partidas"]    = g.Count(),
                        ["kdaMedia"]    = Math.Round(g.Average(p => p.Kda), 2),
                        ["taxaVitoria"] = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitoria") / g.Count() * 100, 1)}%"
                    }).OrderByDescending(d => (double)d["kdaMedia"]!).ToList();

                return Ok(new
                {
                    PorMapa   = Agrupar(partidas.GroupBy(p => p.Mapa),   "mapa"),
                    PorAgente = Agrupar(partidas.GroupBy(p => p.Agente), "agente"),
                    PorModo   = Agrupar(partidas.GroupBy(p => p.Modo),   "modo")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatisticas de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        private Task<Jogador?> BuscarNoBanco(string gameName, string tagLine)
        {
            var nome = gameName.ToLower(); var tag = tagLine.ToLower();
            return _context.Jogadores.FirstOrDefaultAsync(j => j.GameName.ToLower() == nome && j.TagLine.ToLower() == tag);
        }
    }
}

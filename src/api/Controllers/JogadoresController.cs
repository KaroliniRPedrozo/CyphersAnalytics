using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.services;
using Asp.Versioning;

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class JogadoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HenrikService _henrik;
        private readonly ILogger<JogadoresController> _logger;

        public JogadoresController(AppDbContext context, HenrikService henrik, ILogger<JogadoresController> logger)
        {
            _context = context;
            _henrik  = henrik;
            _logger  = logger;
        }

        // GET: api/v1/Jogadores/Karo/BR1
        [HttpGet("{gameName}/{tagLine}")]
        public async Task<IActionResult> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _henrik.BuscarJogador(gameName, tagLine);
                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado. Verifique o nome e a tag." });

                var jogadorExistente = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.Puuid == jogador.Puuid);

                if (jogadorExistente == null)
                    _context.Jogadores.Add(jogador);
                else
                {
                    jogadorExistente.RankAtual         = jogador.RankAtual;
                    jogadorExistente.RankImagem        = jogador.RankImagem;
                    jogadorExistente.UltimaAtualizacao = DateTime.UtcNow;
                }

                var partidas      = await _henrik.BuscarPartidas(gameName, tagLine, jogador.Puuid);
                var matchIdsExist = _context.Partidas.Select(p => p.MatchId).ToHashSet();
                var partidasNovas = partidas.Where(p => !matchIdsExist.Contains(p.MatchId)).ToList();

                _context.Partidas.AddRange(partidasNovas);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} novas partidas salvas para {GameName}", partidasNovas.Count, gameName);

                var todasPartidas = await _context.Partidas
                    .Where(p => p.Puuid == jogador.Puuid)
                    .ToListAsync();

                var melhorMapa = todasPartidas
                    .GroupBy(p => p.Mapa)
                    .OrderByDescending(g => g.Average(p => p.Kda))
                    .FirstOrDefault()?.Key;

                var melhorAgente = todasPartidas
                    .GroupBy(p => p.Agente)
                    .OrderByDescending(g => g.Average(p => p.Kda))
                    .FirstOrDefault()?.Key;

                var kdaGeral    = todasPartidas.Any() ? Math.Round(todasPartidas.Average(p => p.Kda), 2) : 0;
                var taxaVitoria = todasPartidas.Any()
                    ? $"{Math.Round((double)todasPartidas.Count(p => p.Resultado == "Vitoria") / todasPartidas.Count * 100, 1)}%"
                    : "0%";

                return Ok(new
                {
                    jogador.GameName,
                    jogador.TagLine,
                    jogador.RankAtual,
                    jogador.RankImagem,
                    TotalPartidas = todasPartidas.Count,
                    MelhorMapa    = melhorMapa,
                    MelhorAgente  = melhorAgente,
                    KdaGeral      = kdaGeral,
                    TaxaVitoria   = taxaVitoria
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jogador {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // PUT: api/v1/Jogadores/Karo/BR1
        [HttpPut("{gameName}/{tagLine}")]
        public async Task<IActionResult> AtualizarJogador(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                // Rebusca dados atualizados na Henrik API
                var jogadorAtualizado = await _henrik.BuscarJogador(gameName, tagLine);
                if (jogadorAtualizado == null)
                    return BadRequest(new { erro = "Nao foi possivel buscar dados atualizados na API." });

                jogador.RankAtual         = jogadorAtualizado.RankAtual;
                jogador.RankImagem        = jogadorAtualizado.RankImagem;
                jogador.UltimaAtualizacao = DateTime.UtcNow;

                // Importa novas partidas
                var partidas      = await _henrik.BuscarPartidas(gameName, tagLine, jogador.Puuid);
                var matchIdsExist = _context.Partidas.Select(p => p.MatchId).ToHashSet();
                var partidasNovas = partidas.Where(p => !matchIdsExist.Contains(p.MatchId)).ToList();

                _context.Partidas.AddRange(partidasNovas);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Jogador {GameName} atualizado. {Count} novas partidas.", gameName, partidasNovas.Count);

                return Ok(new
                {
                    jogador.GameName,
                    jogador.TagLine,
                    jogador.RankAtual,
                    jogador.RankImagem,
                    jogador.UltimaAtualizacao,
                    NovasPartidas = partidasNovas.Count,
                    mensagem      = "Dados atualizados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar jogador {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/v1/Jogadores/Karo/BR1/historico?page=1&size=10
        [HttpGet("{gameName}/{tagLine}/historico")]
        public async Task<IActionResult> Historico(string gameName, string tagLine, int page = 1, int size = 10)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var total = await _context.Partidas.CountAsync(p => p.Puuid == jogador.Puuid);

                var partidas = await _context.Partidas
                    .Where(p => p.Puuid == jogador.Puuid)
                    .OrderByDescending(p => p.DataPartida)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new
                {
                    Page         = page,
                    Size         = size,
                    Total        = total,
                    TotalPaginas = (int)Math.Ceiling((double)total / size),
                    Partidas     = partidas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar historico de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/v1/Jogadores/Karo/BR1/estatisticas
        [HttpGet("{gameName}/{tagLine}/estatisticas")]
        public async Task<IActionResult> Estatisticas(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var partidas = await _context.Partidas
                    .Where(p => p.Puuid == jogador.Puuid)
                    .ToListAsync();

                if (!partidas.Any())
                    return Ok(new { mensagem = "Nenhuma partida encontrada." });

                var porMapa = partidas
                    .GroupBy(p => p.Mapa)
                    .Select(g => new
                    {
                        Mapa        = g.Key,
                        Partidas    = g.Count(),
                        KdaMedia    = Math.Round(g.Average(p => p.Kda), 2),
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitoria") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                var porAgente = partidas
                    .GroupBy(p => p.Agente)
                    .Select(g => new
                    {
                        Agente      = g.Key,
                        Partidas    = g.Count(),
                        KdaMedia    = Math.Round(g.Average(p => p.Kda), 2),
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitoria") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                var porModo = partidas
                    .GroupBy(p => p.Modo)
                    .Select(g => new
                    {
                        Modo        = g.Key,
                        Partidas    = g.Count(),
                        KdaMedia    = Math.Round(g.Average(p => p.Kda), 2),
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitoria") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                return Ok(new
                {
                    PorMapa   = porMapa,
                    PorAgente = porAgente,
                    PorModo   = porModo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatisticas de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/v1/Jogadores/ranking
        [HttpGet("ranking")]
        public async Task<IActionResult> Ranking()
        {
            try
            {
                var jogadores = await _context.Jogadores.ToListAsync();
                var ranking   = new List<object>();

                foreach (var jogador in jogadores)
                {
                    var partidas = await _context.Partidas
                        .Where(p => p.Puuid == jogador.Puuid)
                        .ToListAsync();

                    if (!partidas.Any()) continue;

                    ranking.Add(new
                    {
                        jogador.GameName,
                        jogador.TagLine,
                        jogador.RankAtual,
                        jogador.RankImagem,
                        TotalPartidas = partidas.Count,
                        KdaGeral      = Math.Round(partidas.Average(p => p.Kda), 2),
                        TaxaVitoria   = Math.Round((double)partidas.Count(p => p.Resultado == "Vitoria") / partidas.Count * 100, 1)
                    });
                }

                var rankingOrdenado = ranking
                    .OrderByDescending(r => ((dynamic)r).KdaGeral)
                    .ToList();

                return Ok(rankingOrdenado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar ranking");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }
    }
}
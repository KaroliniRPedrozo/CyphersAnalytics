using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.services;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        // GET: api/Jogadores/Karo/BR1
        [HttpGet("{gameName}/{tagLine}")]
        public async Task<IActionResult> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _henrik.BuscarJogador(gameName, tagLine);
                if (jogador == null)
                    return NotFound(new { erro = "Jogador não encontrado. Verifique o nome e a tag." });

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

                // Evita duplicatas verificando o MatchId
                var partidas        = await _henrik.BuscarPartidas(gameName, tagLine, jogador.Puuid);
                var matchIdsExist   = _context.Partidas.Select(p => p.MatchId).ToHashSet();
                var partidasNovas   = partidas.Where(p => !matchIdsExist.Contains(p.MatchId)).ToList();

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

                return Ok(new
                {
                    jogador.GameName,
                    jogador.TagLine,
                    jogador.RankAtual,
                    jogador.RankImagem,
                    TotalPartidas = todasPartidas.Count,
                    MelhorMapa    = melhorMapa,
                    MelhorAgente  = melhorAgente,
                    KdaGeral      = todasPartidas.Any() ? Math.Round(todasPartidas.Average(p => p.Kda), 2) : 0,
                    TaxaVitoria   = todasPartidas.Any()
                        ? $"{Math.Round((double)todasPartidas.Count(p => p.Resultado == "Vitória") / todasPartidas.Count * 100, 1)}%"
                        : "0%"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jogador {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/Jogadores/Karo/BR1/historico?page=1&size=10
        [HttpGet("{gameName}/{tagLine}/historico")]
        public async Task<IActionResult> Historico(string gameName, string tagLine, int page = 1, int size = 10)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador não encontrado no banco. Faça uma busca primeiro." });

                var total = await _context.Partidas.CountAsync(p => p.Puuid == jogador.Puuid);

                var partidas = await _context.Partidas
                    .Where(p => p.Puuid == jogador.Puuid)
                    .OrderByDescending(p => p.DataPartida)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new
                {
                    Page        = page,
                    Size        = size,
                    Total       = total,
                    TotalPaginas = (int)Math.Ceiling((double)total / size),
                    Partidas    = partidas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar histórico de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/Jogadores/Karo/BR1/estatisticas
        [HttpGet("{gameName}/{tagLine}/estatisticas")]
        public async Task<IActionResult> Estatisticas(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador não encontrado no banco. Faça uma busca primeiro." });

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
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitória") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                var porAgente = partidas
                    .GroupBy(p => p.Agente)
                    .Select(g => new
                    {
                        Agente      = g.Key,
                        Partidas    = g.Count(),
                        KdaMedia    = Math.Round(g.Average(p => p.Kda), 2),
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitória") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                var porModo = partidas
                    .GroupBy(p => p.Modo)
                    .Select(g => new
                    {
                        Modo        = g.Key,
                        Partidas    = g.Count(),
                        KdaMedia    = Math.Round(g.Average(p => p.Kda), 2),
                        TaxaVitoria = $"{Math.Round((double)g.Count(p => p.Resultado == "Vitória") / g.Count() * 100, 1)}%"
                    })
                    .OrderByDescending(x => x.KdaMedia);

                return Ok(new
                {
                    PorMapa    = porMapa,
                    PorAgente  = porAgente,
                    PorModo    = porModo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas de {GameName}#{TagLine}", gameName, tagLine);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }
    }
}
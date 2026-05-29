using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.services;
using Asp.Versioning;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

        [HttpGet("temporada")]
        public async Task<IActionResult> TemporadaAtual()
        {
            try
            {
                var (season, act) = await _henrik.BuscarTemporadaAtual();
                return Ok(new { season, act });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar temporada atual");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        [HttpGet("{gameName}/{tagLine}")]
        public async Task<IActionResult> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                var jogadorExistente = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                bool precisaAtualizar = jogadorExistente == null || 
                                        (DateTime.UtcNow - jogadorExistente.UltimaAtualizacao).TotalMinutes >= 15;

                string? puuidParaBusca = jogadorExistente?.Puuid;

                if (precisaAtualizar)
                {
                    var jogador = await _henrik.BuscarJogador(gameName, tagLine);
                    if (jogador == null)
                        return NotFound(new { erro = "Jogador nao encontrado. Verifique o nome e a tag." });

                    puuidParaBusca = jogador.Puuid;

                    if (jogadorExistente == null)
                    {
                        _context.Jogadores.Add(jogador);
                        jogadorExistente = jogador; 
                    }
                    else
                    {
                        jogadorExistente.RankAtual         = jogador.RankAtual;
                        jogadorExistente.RankImagem        = jogador.RankImagem;
                        jogadorExistente.UltimaAtualizacao = DateTime.UtcNow;
                    }

                    var partidas      = await _henrik.BuscarPartidas(gameName, tagLine, jogador.Puuid);
                    var matchIdsExist = _context.Partidas.Select(p => p.MatchId).ToHashSet();
                    
                    var partidasNovas = partidas.Where(p => 
                        string.IsNullOrEmpty(p.MatchId) || !matchIdsExist.Contains(p.MatchId)
                    ).ToList();

                    _context.Partidas.AddRange(partidasNovas);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Count} novas partidas salvas para {GameName}", partidasNovas.Count, gameName);
                }
                else
                {
                    _logger.LogInformation("Usando cache local (menos de 15min) para {GameName}", gameName);
                }

                var todasPartidas = await _context.Partidas
                    .Where(p => p.Puuid == puuidParaBusca)
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

                // CORREÇÃO AQUI: Adicionado '!' para dizer ao C# que jogadorExistente nunca será nulo neste ponto
                return Ok(new
                {
                    jogadorExistente!.GameName,
                    jogadorExistente.TagLine,
                    jogadorExistente.RankAtual,
                    jogadorExistente.RankImagem,
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

        [HttpPut("{gameName}/{tagLine}")]
        public async Task<IActionResult> AtualizarJogador(string gameName, string tagLine)
        {
            try
            {
                var jogador = await _context.Jogadores
                    .FirstOrDefaultAsync(j => j.GameName == gameName && j.TagLine == tagLine);

                if (jogador == null)
                    return NotFound(new { erro = "Jogador nao encontrado no banco. Faca uma busca primeiro." });

                var jogadorAtualizado = await _henrik.BuscarJogador(gameName, tagLine);
                if (jogadorAtualizado == null)
                    return BadRequest(new { erro = "Nao foi possivel buscar dados atualizados na API." });

                jogador.RankAtual         = jogadorAtualizado.RankAtual;
                jogador.RankImagem        = jogadorAtualizado.RankImagem;
                jogador.UltimaAtualizacao = DateTime.UtcNow;

                var partidas      = await _henrik.BuscarPartidas(gameName, tagLine, jogador.Puuid);
                var matchIdsExist = _context.Partidas.Select(p => p.MatchId).ToHashSet();
                
                var partidasNovas = partidas.Where(p => 
                    string.IsNullOrEmpty(p.MatchId) || !matchIdsExist.Contains(p.MatchId)
                ).ToList();

                _context.Partidas.AddRange(partidasNovas);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Jogador {GameName} atualizado. {Count} novas partidas.", gameName, partidasNovas.Count);

                // CORREÇÃO AQUI (Linha 118): Adicionado '!' para silenciar o aviso de nulo
                return Ok(new
                {
                    jogador!.GameName,
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
    }
}
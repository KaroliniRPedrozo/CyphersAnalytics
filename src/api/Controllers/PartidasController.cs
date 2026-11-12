using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.models;
using Asp.Versioning;

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Authorize] // antes toda a API de partidas era publica: qualquer um podia listar, criar, editar e apagar
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PartidasController : ApiControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PartidasController> _logger;

        public PartidasController(AppDbContext context, ILogger<PartidasController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        /// <summary>Somente as partidas do usuario autenticado (nunca de terceiros).</summary>
        private IQueryable<Partida> MinhasPartidas()
        {
            var id   = UsuarioId;
            var nome = GameNameDoToken.ToLower();
            var tag  = TagLineDoToken.ToLower();
            return _context.Partidas.Where(p => p.UsuarioId == id || (p.GameName.ToLower() == nome && p.TagLine.ToLower() == tag));
        }

        // GET: api/v1/Partidas?page=1&size=10
        [HttpGet]
        public async Task<IActionResult> GetPartidas(int page = 1, int size = 10)
        {
            try
            {
                page = Math.Max(1, page);
                size = Math.Clamp(size, 1, 50);

                var total = await MinhasPartidas().CountAsync();

                var partidas = await MinhasPartidas()
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
                _logger.LogError(ex, "Erro ao listar partidas");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/v1/Partidas/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPartida(int id)
        {
            try
            {
                var partida = await MinhasPartidas().FirstOrDefaultAsync(p => p.Id == id);

                if (partida == null)
                    return NotFound(new { erro = $"Partida {id} não encontrada." });

                return Ok(partida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar partida {Id}", id);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // POST: api/v1/Partidas
        // Aceita Partida com ou sem Estatísticas (opcionais)
        [HttpPost]
        public async Task<IActionResult> PostPartida([FromBody] CriarPartidaComEstatisticasDto dto)
        {
            try
            {
                // Mapear DTO para Partida
                var partida = new Partida
                {
                    MatchId = dto.MatchId,
                    Puuid = dto.Puuid,
                    // O dono do registro e sempre o usuario do token, nao o que vier no corpo.
                    UsuarioId = UsuarioId,
                    GameName = GameNameDoToken,
                    TagLine = TagLineDoToken,
                    Mapa = dto.Mapa,
                    Modo = dto.Modo,
                    Agente = dto.Agente,
                    Resultado = dto.Resultado,
                    Kills = dto.Kills,
                    Deaths = dto.Deaths,
                    Assists = dto.Assists,
                    Kda = dto.Kda,
                    Score = dto.Score,
                    DataPartida = dto.DataPartida
                };

                // Se houver dados de estatística, cria também
                if (dto.Estatistica != null)
                {
                    partida.Estatistica = new PartidaEstatistica
                    {
                        AcsScore = dto.Estatistica.AcsScore,
                        EconScore = dto.Estatistica.EconScore,
                        RoundasGanhas = dto.Estatistica.RoundasGanhas,
                        RoundasPerdidas = dto.Estatistica.RoundasPerdidas,
                        HeadshotPercentual = dto.Estatistica.HeadshotPercentual,
                        BodyShotCount = dto.Estatistica.BodyShotCount,
                        LegShotCount = dto.Estatistica.LegShotCount,
                        ArmaFavorita = dto.Estatistica.ArmaFavorita,
                        DanosCausados = dto.Estatistica.DanosCausados,
                        DanosRecebidos = dto.Estatistica.DanosRecebidos
                    };
                }

                _context.Partidas.Add(partida);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Partida {Id} criada com sucesso", partida.Id);
                return CreatedAtAction(nameof(GetPartida), new { id = partida.Id }, partida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar partida");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // PUT: api/v1/Partidas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPartida(int id, [FromBody] Partida partidaAtualizada)
        {
            try
            {
                var partida = await MinhasPartidas().FirstOrDefaultAsync(p => p.Id == id);

                if (partida == null)
                    return NotFound(new { erro = $"Partida {id} nao encontrada." });

                // Atualiza apenas os campos permitidos
                partida.Mapa      = partidaAtualizada.Mapa;
                partida.Modo      = partidaAtualizada.Modo;
                partida.Agente    = partidaAtualizada.Agente;
                partida.Resultado = partidaAtualizada.Resultado;
                partida.Kills     = partidaAtualizada.Kills;
                partida.Deaths    = partidaAtualizada.Deaths;
                partida.Assists   = partidaAtualizada.Assists;
                partida.Kda       = partidaAtualizada.Deaths == 0
                    ? partidaAtualizada.Kills + partidaAtualizada.Assists
                    : Math.Round((double)(partidaAtualizada.Kills + partidaAtualizada.Assists) / partidaAtualizada.Deaths, 2);
                partida.Score     = partidaAtualizada.Score;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Partida {Id} atualizada", id);
                return Ok(partida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar partida {Id}", id);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // DELETE: api/v1/Partidas/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartida(int id)
        {
            try
            {
                var partida = await MinhasPartidas().FirstOrDefaultAsync(p => p.Id == id);

                if (partida == null)
                    return NotFound(new { erro = $"Partida {id} não encontrada." });

                partida.DeletedAt = DateTime.UtcNow; // ← Soft Delete
                await _context.SaveChangesAsync();

                _logger.LogInformation("Partida {Id} deletada (soft delete)", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar partida {Id}", id);
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }
    }
}
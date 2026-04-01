using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.models;
using Asp.Versioning;

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PartidasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PartidasController> _logger;

        public PartidasController(AppDbContext context, ILogger<PartidasController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // GET: api/v1/Partidas?page=1&size=10
        [HttpGet]
        public async Task<IActionResult> GetPartidas(int page = 1, int size = 10)
        {
            try
            {
                var total = await _context.Partidas.CountAsync();

                var partidas = await _context.Partidas
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
                var partida = await _context.Partidas.FindAsync(id);

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
        [HttpPost]
        public async Task<IActionResult> PostPartida(Partida partida)
        {
            try
            {
                _context.Partidas.Add(partida);
                await _context.SaveChangesAsync();

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
                var partida = await _context.Partidas.FindAsync(id);

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
                var partida = await _context.Partidas.FindAsync(id);

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
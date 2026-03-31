using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.models;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartidasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartidasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Partidas
        // Lista todas as partidas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Partida>>> GetPartidas()
        {
            return await _context.Partidas.ToListAsync();
        }

        // GET: api/Partidas/5
        // Busca uma partida pelo Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Partida>> GetPartida(int id)
        {
            var partida = await _context.Partidas.FindAsync(id);

            if (partida == null)
                return NotFound(); // Retorna 404 se não encontrar

            return partida;
        }

        // POST: api/Partidas
        // Registra uma nova partida
        [HttpPost]
        public async Task<ActionResult<Partida>> PostPartida(Partida partida)
        {
            _context.Partidas.Add(partida);
            await _context.SaveChangesAsync();

            // Retorna 201 Created com a partida criada
            return CreatedAtAction(nameof(GetPartida), new { id = partida.Id }, partida);
        }

        // DELETE: api/Partidas/5
        // Remove uma partida pelo Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartida(int id)
        {
            var partida = await _context.Partidas.FindAsync(id);

            if (partida == null)
                return NotFound();

            _context.Partidas.Remove(partida);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using api.services;
using Asp.Versioning;

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IValorantDataProvider _provider;

        public StatusController(IValorantDataProvider provider)
        {
            _provider = provider;
        }

        // Dado publico e agregado do jogo (nao identifica jogadores), com cache de 5 min no provedor.
        [HttpGet("agentes-desativados")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _provider.BuscarAgentesDesativadosAsync());
        }
    }
}

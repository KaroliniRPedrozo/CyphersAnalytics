using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using api.services;
using Asp.Versioning; // Adicionado para suportar o versionamento do seu projeto

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")] // Etiqueta obrigatória no seu sistema
    [Route("api/v{version:apiVersion}/[controller]")] // Rota dinâmica padrão
    public class StatusController : ControllerBase
    {
        private readonly HenrikService _henrikService;

        public StatusController(HenrikService henrikService)
        {
            _henrikService = henrikService;
        }

        [HttpGet("agentes-desativados")]
        public async Task<IActionResult> Get()
        {
            var desativados = await _henrikService.BuscarAgentesDesativadosAsync();

            return Ok(desativados);
        }
    }
}
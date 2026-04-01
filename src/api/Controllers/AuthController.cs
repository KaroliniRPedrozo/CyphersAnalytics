using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.database;
using api.models;
using api.services;
using Asp.Versioning;

namespace api.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _auth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, AuthService auth, ILogger<AuthController> logger)
        {
            _context = context;
            _auth    = auth;
            _logger  = logger;
        }

        // POST: api/v1/Auth/registrar
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto)
        {
            try
            {
                if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                    return BadRequest(new { erro = "Email ja cadastrado." });

                var usuario = new Usuario
                {
                    Email     = dto.Email,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                    GameName  = dto.GameName,
                    TagLine   = dto.TagLine
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Novo usuario registrado: {Email}", dto.Email);
                return Ok(new { mensagem = "Usuario criado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuario");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // POST: api/v1/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
                    return Unauthorized(new { erro = "Email ou senha invalidos." });

                var token = _auth.GerarToken(usuario);

                _logger.LogInformation("Login realizado: {Email}", dto.Email);
                return Ok(new { token, usuario.GameName, usuario.TagLine });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer login");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // PUT: api/v1/Auth/perfil
        [HttpPut("perfil")]
        public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilDto dto)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == dto.EmailAtual);

                if (usuario == null)
                    return NotFound(new { erro = "Usuario nao encontrado." });

                if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
                    return Unauthorized(new { erro = "Senha atual incorreta." });

                // Atualiza email se informado
                if (!string.IsNullOrWhiteSpace(dto.NovoEmail) && dto.NovoEmail != dto.EmailAtual)
                {
                    var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.NovoEmail);
                    if (emailExiste)
                        return BadRequest(new { erro = "Este email ja esta em uso." });
                    
                    usuario.Email = dto.NovoEmail;
                }

                // Atualiza senha se informada
                if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
                    usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

                // Atualiza nick do Valorant se informado
                if (!string.IsNullOrWhiteSpace(dto.GameName))
                    usuario.GameName = dto.GameName;

                if (!string.IsNullOrWhiteSpace(dto.TagLine))
                    usuario.TagLine = dto.TagLine;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Perfil atualizado: {Email}", usuario.Email);
                return Ok(new { mensagem = "Perfil atualizado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar perfil");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }
    }

    public record RegistrarDto(string Email, string Senha, string GameName, string TagLine);
    public record LoginDto(string Email, string Senha);
    
    // DTO adicionado para suportar o PUT de atualização de perfil
    public record AtualizarPerfilDto(
        string EmailAtual, 
        string SenhaAtual, 
        string? NovoEmail, 
        string? NovaSenha, 
        string? GameName, 
        string? TagLine
    );
}
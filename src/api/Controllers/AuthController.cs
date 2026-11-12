using Microsoft.AspNetCore.Authorization;
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
    public class AuthController : ApiControllerBase
    {
        public const string VersaoTermos = "2026-11";
        private const int SenhaMinima = 8;

        private readonly AppDbContext _context;
        private readonly AuthService _auth;
        private readonly RsoOptions _rso;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, AuthService auth, RsoOptions rso, ILogger<AuthController> logger)
        {
            _context = context;
            _auth    = auth;
            _rso     = rso;
            _logger  = logger;
        }

        // POST: api/v1/Auth/registrar
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto)
        {
            try
            {
                var email    = (dto.Email ?? "").Trim().ToLowerInvariant();
                var gameName = (dto.GameName ?? "").Trim();
                var tagLine  = (dto.TagLine ?? "").Trim().TrimStart('#');

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Senha) ||
                    string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(tagLine))
                    return BadRequest(new { erro = "Preencha todos os campos." });

                if (!dto.AceitouTermos)
                    return BadRequest(new { erro = "E necessario aceitar os Termos de Uso e a Politica de Privacidade." });

                if (dto.Senha.Length < SenhaMinima)
                    return BadRequest(new { erro = $"A senha deve ter pelo menos {SenhaMinima} caracteres." });

                if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
                    return BadRequest(new { erro = "Email ja cadastrado." });

                var agora = DateTime.UtcNow;
                _context.Usuarios.Add(new Usuario
                {
                    Email           = email,
                    SenhaHash       = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                    GameName        = gameName,
                    TagLine         = tagLine,
                    ConsentimentoEm = agora,
                    TermosVersao    = VersaoTermos
                });
                await _context.SaveChangesAsync();

                // Nao registra o e-mail em log (dado pessoal).
                _logger.LogInformation("Novo usuario registrado");
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
                var email = (dto.Email ?? "").Trim().ToLowerInvariant();
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

                if (usuario == null || !SenhaConfere(dto.Senha, usuario.SenhaHash))
                    return Unauthorized(new { erro = "Email ou senha invalidos." });

                return Ok(new
                {
                    token = _auth.GerarToken(usuario),
                    usuario.GameName,
                    usuario.TagLine,
                    consentimento = usuario.ConsentimentoEm != null,
                    contaRiotVinculada = usuario.RsoVerificadoEm != null,
                    rsoHabilitado = _rso.Enabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer login");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        private static bool SenhaConfere(string? senha, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(senha ?? "", hash); }
            catch { return false; }
        }

        // GET: api/v1/Auth/conta  -> situacao da conta (consentimento / vinculo Riot)
        [Authorize]
        [HttpGet("conta")]
        public async Task<IActionResult> Conta()
        {
            var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == UsuarioId);
            if (u == null) return Unauthorized();

            return Ok(new
            {
                u.Email, u.GameName, u.TagLine, u.CriadoEm,
                consentimento = u.ConsentimentoEm != null,
                consentimentoEm = u.ConsentimentoEm,
                termosVersao = u.TermosVersao,
                contaRiotVinculada = u.RsoVerificadoEm != null,
                rsoVerificadoEm = u.RsoVerificadoEm,
                rsoHabilitado = _rso.Enabled
            });
        }

        // POST: api/v1/Auth/consentimento  -> usuarios antigos (sem consentimento registrado) autorizam o uso dos dados
        [Authorize]
        [HttpPost("consentimento")]
        public async Task<IActionResult> Consentir([FromBody] ConsentimentoDto dto)
        {
            if (!dto.Aceito) return BadRequest(new { erro = "Consentimento nao informado." });

            var u = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == UsuarioId);
            if (u == null) return Unauthorized();

            u.ConsentimentoEm = DateTime.UtcNow;
            u.TermosVersao    = VersaoTermos;
            await _context.SaveChangesAsync();
            return Ok(new { mensagem = "Consentimento registrado." });
        }

        // DELETE: api/v1/Auth/consentimento -> revogar: para de exibir dados e apaga o historico guardado
        [Authorize]
        [HttpDelete("consentimento")]
        public async Task<IActionResult> RevogarConsentimento()
        {
            var u = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == UsuarioId);
            if (u == null) return Unauthorized();

            await ApagarDadosDeJogoAsync(u);
            u.ConsentimentoEm = null;
            await _context.SaveChangesAsync();
            return Ok(new { mensagem = "Consentimento revogado e dados de jogo removidos." });
        }

        // PUT: api/v1/Auth/perfil
        [Authorize]
        [HttpPut("perfil")]
        public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilDto dto)
        {
            try
            {
                // A identidade vem do token, nunca do corpo da requisicao.
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == UsuarioId);
                if (usuario == null) return Unauthorized();

                if (!SenhaConfere(dto.SenhaAtual, usuario.SenhaHash))
                    return Unauthorized(new { erro = "Senha atual incorreta." });

                var novoEmail = (dto.NovoEmail ?? "").Trim().ToLowerInvariant();
                if (novoEmail.Length > 0 && novoEmail != usuario.Email)
                {
                    if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == novoEmail && u.Id != usuario.Id))
                        return BadRequest(new { erro = "Este email ja esta em uso." });
                    usuario.Email = novoEmail;
                }

                if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
                {
                    if (dto.NovaSenha.Length < SenhaMinima)
                        return BadRequest(new { erro = $"A senha deve ter pelo menos {SenhaMinima} caracteres." });
                    usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
                }

                var novoNome = (dto.GameName ?? "").Trim();
                var novaTag  = (dto.TagLine ?? "").Trim().TrimStart('#');
                var mudouNick = (novoNome.Length > 0 && novoNome != usuario.GameName) ||
                                (novaTag.Length > 0 && novaTag != usuario.TagLine);

                if (mudouNick)
                {
                    if (novoNome.Length > 0) usuario.GameName = novoNome;
                    if (novaTag.Length > 0)  usuario.TagLine  = novaTag;
                    // Outro Riot ID = outra conta: a verificacao anterior deixa de valer.
                    usuario.RsoPuuid = null;
                    usuario.RsoVerificadoEm = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensagem = "Perfil atualizado com sucesso!",
                    token = _auth.GerarToken(usuario), // claims (nick/email) mudaram
                    usuario.GameName, usuario.TagLine
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar perfil");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        // GET: api/v1/Auth/meus-dados  -> portabilidade (LGPD art. 18)
        [Authorize]
        [HttpGet("meus-dados")]
        public async Task<IActionResult> MeusDados()
        {
            var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == UsuarioId);
            if (u == null) return Unauthorized();

            var nome = u.GameName.ToLower(); var tag = u.TagLine.ToLower();
            var partidas = await _context.Partidas.AsNoTracking()
                .Where(p => p.UsuarioId == u.Id || (p.GameName.ToLower() == nome && p.TagLine.ToLower() == tag))
                .Select(p => new
                {
                    p.MatchId, p.Mapa, p.Modo, p.Agente, p.Resultado, p.Kills, p.Deaths, p.Assists, p.Kda, p.Score, p.DataPartida,
                    estatistica = p.Estatistica == null ? null : new
                    {
                        p.Estatistica.AcsScore, p.Estatistica.EconScore, p.Estatistica.RoundasGanhas, p.Estatistica.RoundasPerdidas,
                        p.Estatistica.HeadshotPercentual, p.Estatistica.BodyShotCount, p.Estatistica.LegShotCount,
                        p.Estatistica.ArmaFavorita, p.Estatistica.DanosCausados, p.Estatistica.DanosRecebidos
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                usuario = new { u.Email, u.GameName, u.TagLine, u.CriadoEm, u.ConsentimentoEm, u.TermosVersao, u.RsoVerificadoEm },
                partidas
            });
        }

        // DELETE: api/v1/Auth/conta  -> exclusao da conta e de todos os dados (LGPD art. 18, VI)
        [Authorize]
        [HttpDelete("conta")]
        public async Task<IActionResult> ExcluirConta([FromBody] ExcluirContaDto dto)
        {
            try
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == UsuarioId);
                if (usuario == null) return Unauthorized();

                if (!SenhaConfere(dto.Senha, usuario.SenhaHash))
                    return Unauthorized(new { erro = "Senha incorreta." });

                await ApagarDadosDeJogoAsync(usuario);
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Conta {UsuarioId} excluida a pedido do titular", usuario.Id);
                return Ok(new { mensagem = "Conta e dados excluidos definitivamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir conta");
                return StatusCode(500, new { erro = "Erro interno. Tente novamente mais tarde." });
            }
        }

        /// <summary>Remove definitivamente (inclusive soft-deleted) partidas, estatisticas e o cache do jogador.</summary>
        private async Task ApagarDadosDeJogoAsync(Usuario u)
        {
            var nome = u.GameName.ToLower(); var tag = u.TagLine.ToLower();

            var partidas = await _context.Partidas.IgnoreQueryFilters()
                .Where(p => p.UsuarioId == u.Id || (p.GameName.ToLower() == nome && p.TagLine.ToLower() == tag))
                .ToListAsync();
            _context.Partidas.RemoveRange(partidas); // estatisticas saem por cascade

            var jogadores = await _context.Jogadores
                .Where(j => j.GameName.ToLower() == nome && j.TagLine.ToLower() == tag)
                .ToListAsync();
            _context.Jogadores.RemoveRange(jogadores);

            await _context.SaveChangesAsync();
        }
    }

    public record RegistrarDto(string Email, string Senha, string GameName, string TagLine, bool AceitouTermos);
    public record LoginDto(string Email, string Senha);
    public record ConsentimentoDto(bool Aceito);
    public record ExcluirContaDto(string Senha);

    // EmailAtual foi mantido so por compatibilidade: a identidade agora vem do JWT.
    public record AtualizarPerfilDto(
        string? EmailAtual,
        string SenhaAtual,
        string? NovoEmail,
        string? NovaSenha,
        string? GameName,
        string? TagLine
    );
}

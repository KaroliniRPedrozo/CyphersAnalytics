using api.database;
using api.models;
using Microsoft.EntityFrameworkCore;

namespace api.services
{
    /// <summary>
    /// Sincroniza jogador + partidas com o provedor externo e grava no banco (que funciona como cache).
    /// Centraliza a logica antes duplicada entre GET e PUT do JogadoresController.
    /// </summary>
    public class JogadorSyncService
    {
        private readonly AppDbContext _context;
        private readonly IValorantDataProvider _provider;
        private readonly ILogger<JogadorSyncService> _logger;

        public JogadorSyncService(AppDbContext context, IValorantDataProvider provider, ILogger<JogadorSyncService> logger)
        {
            _context = context;
            _provider = provider;
            _logger = logger;
        }

        /// <returns>(null, 0) se o jogador nao existe na API externa. Lanca UpstreamUnavailableException se a API estiver limitando/fora do ar.</returns>
        public async Task<(Jogador? Jogador, int NovasPartidas)> SincronizarAsync(
            string gameName, string tagLine, Jogador? existente, int? usuarioId, CancellationToken ct = default)
        {
            var buscado = await _provider.BuscarJogador(gameName, tagLine);
            if (buscado == null) return (null, 0);

            var partidas = await _provider.BuscarPartidas(gameName, tagLine, buscado.Puuid);

            // O rank pode depender das partidas (API oficial). Null = manter o que BuscarJogador devolveu.
            if (await _provider.BuscarRankAsync(buscado.Puuid) is { } rank)
            {
                buscado.RankAtual = rank.Rank;
                buscado.RankImagem = rank.Imagem;
            }

            if (existente == null)
            {
                _context.Jogadores.Add(buscado);
                existente = buscado;
            }
            else
            {
                existente.RankAtual         = buscado.RankAtual;
                existente.RankImagem        = string.IsNullOrEmpty(buscado.RankImagem) ? existente.RankImagem : buscado.RankImagem;
                existente.UltimaAtualizacao = DateTime.UtcNow;
            }

            // Deduplica por (MatchId, Puuid): o mesmo MatchId aparece para varios jogadores da partida.
            // IgnoreQueryFilters: partidas removidas (soft delete) nao devem voltar na proxima sincronizacao.
            var jaSalvas = (await _context.Partidas.IgnoreQueryFilters()
                    .Where(p => p.Puuid == buscado.Puuid)
                    .Select(p => p.MatchId)
                    .ToListAsync(ct))
                .ToHashSet();

            var novas = partidas
                .Where(p => string.IsNullOrEmpty(p.MatchId) || !jaSalvas.Contains(p.MatchId))
                .ToList();

            foreach (var n in novas) n.UsuarioId = usuarioId;

            _context.Partidas.AddRange(novas);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("{Count} novas partidas salvas para {GameName}", novas.Count, gameName);
            return (existente, novas.Count);
        }
    }
}

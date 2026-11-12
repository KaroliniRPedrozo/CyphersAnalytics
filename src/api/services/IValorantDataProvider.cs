using api.models;

namespace api.services
{
    /// <summary>
    /// Abstracao sobre a origem dos dados do VALORANT. Permite trocar a API nao oficial (Henrik)
    /// pela API oficial da Riot apenas mudando DATA_PROVIDER, sem tocar nos controllers.
    /// </summary>
    public interface IValorantDataProvider
    {
        Task<Jogador?> BuscarJogador(string gameName, string tagLine);
        Task<(int Season, int Act)> BuscarTemporadaAtual();
        Task<List<Partida>> BuscarPartidas(string gameName, string tagLine, string puuid);
        Task<List<object>> BuscarAgentesDesativadosAsync();

        /// <summary>
        /// Rank atual quando o provedor so consegue calcula-lo depois de ler as partidas
        /// (API oficial nao tem endpoint de rank por jogador). Null = manter o rank ja obtido.
        /// </summary>
        Task<(string Rank, string Imagem)?> BuscarRankAsync(string puuid);
    }

    /// <summary>A API externa esta limitando (429) ou fora do ar (5xx). Nao significa "jogador inexistente".</summary>
    public class UpstreamUnavailableException : Exception
    {
        public TimeSpan? RetryAfter { get; }
        public UpstreamUnavailableException(string message, TimeSpan? retryAfter = null) : base(message)
            => RetryAfter = retryAfter;
    }
}

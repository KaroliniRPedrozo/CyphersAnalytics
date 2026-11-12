using System.Net;
using System.Text.Json;
using api.models;
using Microsoft.Extensions.Caching.Memory;

namespace api.services
{
    /// <summary>
    /// Provedor baseado na API OFICIAL da Riot (DATA_PROVIDER=riot).
    ///
    /// Endpoints usados: account-v1 (by-riot-id), val-match-v1 (matchlists/matches),
    /// val-content-v1 e val-status-v1. A autenticacao e feita pelo header X-Riot-Token.
    ///
    /// Observacoes importantes:
    ///  - val-match-v1 so responde com chave de PRODUCAO (e dados de jogadores exigem opt-in via RSO).
    ///  - A API oficial nao possui endpoint de rank por jogador; o rank e derivado do campo
    ///    competitiveTier da partida competitiva mais recente (ver BuscarRankAsync).
    /// </summary>
    public class RiotApiService : IValorantDataProvider
    {
        private const int PartidasPorSincronizacao = 5;

        private readonly HttpClient _http;
        private readonly ILogger<RiotApiService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _regional;  // americas | europe | asia (account-v1)
        private readonly string _plataforma; // br | na | eu | ap | kr | latam (val-*)

        public RiotApiService(HttpClient http, ILogger<RiotApiService> logger, IMemoryCache cache)
        {
            _http = http;
            _logger = logger;
            _cache = cache;
            _regional   = (Environment.GetEnvironmentVariable("RIOT_REGION")   ?? "americas").ToLowerInvariant();
            _plataforma = (Environment.GetEnvironmentVariable("RIOT_PLATFORM") ?? "br").ToLowerInvariant();
        }

        private string Regional  => $"https://{_regional}.api.riotgames.com";
        private string Plataforma => $"https://{_plataforma}.api.riotgames.com";

        private static bool Sucesso(HttpResponseMessage r)
        {
            if (r.IsSuccessStatusCode) return true;
            if (r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode >= 500)
                throw new UpstreamUnavailableException($"API da Riot indisponivel ({(int)r.StatusCode}).", r.Headers.RetryAfter?.Delta);
            return false;
        }

        public async Task<Jogador?> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                var url = $"{Regional}/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
                using var response = await _http.GetAsync(url);
                if (!Sucesso(response)) return null;

                var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                var puuid = root.GetProperty("puuid").GetString()!;

                return new Jogador
                {
                    Puuid     = puuid,
                    GameName  = root.TryGetProperty("gameName", out var gn) ? gn.GetString() ?? gameName : gameName,
                    TagLine   = root.TryGetProperty("tagLine",  out var tl) ? tl.GetString() ?? tagLine  : tagLine,
                    RankAtual = "Unranked",
                    RankImagem = "",
                    UltimaAtualizacao = DateTime.UtcNow
                };
            }
            catch (UpstreamUnavailableException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar conta {GameName}#{TagLine}", gameName, tagLine);
                return null;
            }
        }

        public Task<(string Rank, string Imagem)?> BuscarRankAsync(string puuid)
        {
            if (_cache.TryGetValue($"riot_tier_{puuid}", out int tier) && tier >= 3)
                return Task.FromResult<(string Rank, string Imagem)?>((NomeDoTier(tier), ""));
            return Task.FromResult<(string Rank, string Imagem)?>(null);
        }

        public async Task<(int Season, int Act)> BuscarTemporadaAtual()
        {
            const string chave = "riot_temporada";
            var fallback = (Season: 0, Act: 0);
            if (_cache.TryGetValue(chave, out (int Season, int Act) cached)) return cached;

            try
            {
                var conteudo = await ObterConteudoAsync();
                if (conteudo.HasValue && conteudo.Value.TryGetProperty("acts", out var acts) &&
                    ParsingHelpers.ExtrairTemporada(acts) is { } atual)
                {
                    _cache.Set(chave, atual, TimeSpan.FromHours(1));
                    return atual;
                }
            }
            catch (Exception ex)
            {
                // Temporada e informacao secundaria: se a Riot estiver limitando, mostra o fallback.
                _logger.LogWarning(ex, "Nao foi possivel ler a temporada atual");
            }
            return fallback;
        }

        public async Task<List<Partida>> BuscarPartidas(string gameName, string tagLine, string puuid)
        {
            var partidas = new List<Partida>();
            try
            {
                using var lista = await _http.GetAsync($"{Plataforma}/val/match/v1/matchlists/by-puuid/{puuid}");
                if (!Sucesso(lista)) return partidas;

                var historico = JsonDocument.Parse(await lista.Content.ReadAsStringAsync()).RootElement;
                if (!historico.TryGetProperty("history", out var hist) || hist.ValueKind != JsonValueKind.Array)
                    return partidas;

                var agentes = await MapaDeAgentesAsync();

                foreach (var item in hist.EnumerateArray().Take(PartidasPorSincronizacao))
                {
                    var matchId = item.TryGetProperty("matchId", out var mid) ? mid.GetString() : null;
                    if (string.IsNullOrEmpty(matchId)) continue;

                    using var resp = await _http.GetAsync($"{Plataforma}/val/match/v1/matches/{matchId}");
                    if (!Sucesso(resp)) continue;

                    var partida = ConverterPartida(JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement,
                                                   matchId, gameName, tagLine, puuid, agentes);
                    if (partida != null) partidas.Add(partida);
                }
                return partidas;
            }
            catch (UpstreamUnavailableException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar partidas de {GameName}#{TagLine}", gameName, tagLine);
                return partidas;
            }
        }

        private Partida? ConverterPartida(JsonElement match, string matchId, string gameName, string tagLine,
                                          string puuid, Dictionary<string, string> agentes)
        {
            if (!match.TryGetProperty("matchInfo", out var info) || !match.TryGetProperty("players", out var players))
                return null;

            JsonElement? eu = null;
            foreach (var p in players.EnumerateArray())
                if (p.TryGetProperty("puuid", out var pp) && pp.GetString() == puuid) { eu = p; break; }
            if (eu is null || !eu.Value.TryGetProperty("stats", out var stats)) return null;

            int Int(JsonElement e, string nome) => e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

            var kills = Int(stats, "kills"); var deaths = Int(stats, "deaths"); var assists = Int(stats, "assists");
            var kda = deaths == 0 ? kills + assists : Math.Round((double)(kills + assists) / deaths, 2);

            // Guarda o tier competitivo mais recente (usado para derivar o rank).
            var tier = Int(eu.Value, "competitiveTier");
            if (tier >= 3 && info.TryGetProperty("queueId", out var q0) && q0.GetString() == "competitive")
                _cache.Set($"riot_tier_{puuid}", tier, TimeSpan.FromDays(1));

            var teamId = eu.Value.TryGetProperty("teamId", out var tid) ? tid.GetString() : null;
            var venceu = false;
            if (teamId != null && match.TryGetProperty("teams", out var teams) && teams.ValueKind == JsonValueKind.Array)
                foreach (var t in teams.EnumerateArray())
                    if (t.TryGetProperty("teamId", out var ti) && ti.GetString() == teamId &&
                        t.TryGetProperty("won", out var won)) { venceu = won.ValueKind == JsonValueKind.True; break; }

            var charId = eu.Value.TryGetProperty("characterId", out var cid) ? cid.GetString() ?? "" : "";
            agentes.TryGetValue(charId.ToLowerInvariant(), out var agente);

            var data = DateTime.UtcNow;
            if (info.TryGetProperty("gameStartMillis", out var gs) && gs.TryGetInt64(out var ms) && ms > 0)
                data = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

            var mapaPath = info.TryGetProperty("mapId", out var mp) ? mp.GetString() ?? "" : "";
            var queue    = info.TryGetProperty("queueId", out var qi) ? qi.GetString() ?? "" : "";

            return new Partida
            {
                MatchId = matchId, Puuid = puuid, GameName = gameName, TagLine = tagLine,
                Mapa      = NomeDoMapa(mapaPath),
                Modo      = queue.Length > 0 ? char.ToUpperInvariant(queue[0]) + queue[1..] : "",
                Agente    = agente ?? "",
                Resultado = venceu ? "Vitoria" : "Derrota",
                Kills = kills, Deaths = deaths, Assists = assists, Kda = kda,
                Score = Int(stats, "score"), DataPartida = data
            };
        }

        /// <summary>Ultimo segmento do path do mapa (ex.: /Game/Maps/Ascent/Ascent -> Ascent).</summary>
        private static string NomeDoMapa(string path)
            => string.IsNullOrEmpty(path) ? "" : path.TrimEnd('/').Split('/').Last();

        private async Task<JsonElement?> ObterConteudoAsync()
        {
            const string chave = "riot_content";
            if (_cache.TryGetValue(chave, out JsonElement cached)) return cached;

            using var resp = await _http.GetAsync($"{Plataforma}/val/content/v1/contents?locale=en-US");
            if (!Sucesso(resp)) return null;

            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();
            _cache.Set(chave, root, TimeSpan.FromHours(6));
            return root;
        }

        private async Task<Dictionary<string, string>> MapaDeAgentesAsync()
        {
            var mapa = new Dictionary<string, string>();
            var conteudo = await ObterConteudoAsync();
            if (conteudo.HasValue && conteudo.Value.TryGetProperty("characters", out var chars) && chars.ValueKind == JsonValueKind.Array)
                foreach (var c in chars.EnumerateArray())
                    if (c.TryGetProperty("id", out var id) && c.TryGetProperty("name", out var nome))
                        mapa[(id.GetString() ?? "").ToLowerInvariant()] = nome.GetString() ?? "";
            return mapa;
        }

        public async Task<List<object>> BuscarAgentesDesativadosAsync()
        {
            const string chave = "riot_status";
            if (_cache.TryGetValue(chave, out List<object>? cached) && cached != null) return cached;

            try
            {
                using var resp = await _http.GetAsync($"{Plataforma}/val/status/v1/platform-data");
                if (!resp.IsSuccessStatusCode) return new List<object>();

                var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
                var resultado = root.TryGetProperty("incidents", out var inc)
                    ? ParsingHelpers.ExtrairAgentesDesativados(inc)
                    : new List<object>();
                _cache.Set(chave, resultado, TimeSpan.FromMinutes(5));
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar status da plataforma");
                return new List<object>();
            }
        }

        private static string NomeDoTier(int tier)
        {
            if (tier >= 27) return "Radiant";
            string[] nomes = { "Iron", "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Ascendant", "Immortal" };
            var idx = (tier - 3) / 3;
            return idx is >= 0 and < 8 ? $"{nomes[idx]} {((tier - 3) % 3) + 1}" : "Unranked";
        }
    }
}

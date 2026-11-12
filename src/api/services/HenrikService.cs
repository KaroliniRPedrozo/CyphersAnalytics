using System.Net;
using System.Text.Json;
using api.models;
using Microsoft.Extensions.Caching.Memory;

namespace api.services
{
    /// <summary>
    /// Provedor baseado na API NAO OFICIAL HenrikDev. Mantido como fallback enquanto a chave
    /// de producao da Riot nao e aprovada (DATA_PROVIDER=henrik).
    /// </summary>
    public class HenrikService : IValorantDataProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<HenrikService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _regiao;

        public HenrikService(HttpClient http, ILogger<HenrikService> logger, IMemoryCache cache)
        {
            _http   = http;
            _logger = logger;
            _cache  = cache;
            _regiao = (Environment.GetEnvironmentVariable("HENRIK_REGION") ?? "br").ToLowerInvariant();
        }

        /// <summary>Lanca UpstreamUnavailableException em 429/5xx; devolve false para os demais erros (ex.: 404).</summary>
        private static bool Sucesso(HttpResponseMessage r)
        {
            if (r.IsSuccessStatusCode) return true;
            if (r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode >= 500)
                throw new UpstreamUnavailableException($"API externa indisponivel ({(int)r.StatusCode}).",
                    r.Headers.RetryAfter?.Delta);
            return false;
        }

        public async Task<Jogador?> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                _logger.LogInformation("Buscando jogador {GameName}#{TagLine}", gameName, tagLine);

                var n = Uri.EscapeDataString(gameName);
                var t = Uri.EscapeDataString(tagLine);

                using var response = await _http.GetAsync($"/valorant/v1/account/{n}/{t}");
                if (!Sucesso(response))
                {
                    _logger.LogWarning("Jogador nao encontrado: {GameName}#{TagLine}", gameName, tagLine);
                    return null;
                }

                var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

                string rankAtual = "Unranked", rankImagem = "";
                using var rankResponse = await _http.GetAsync($"/valorant/v1/mmr/{_regiao}/{n}/{t}");
                if (rankResponse.IsSuccessStatusCode)
                {
                    var rankRoot = JsonDocument.Parse(await rankResponse.Content.ReadAsStringAsync()).RootElement;
                    if (rankRoot.TryGetProperty("data", out var rankData) && rankData.ValueKind == JsonValueKind.Object)
                    {
                        if (rankData.TryGetProperty("currenttierpatched", out var tier) && tier.ValueKind == JsonValueKind.String)
                            rankAtual = tier.GetString() ?? "Unranked";

                        if (rankData.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Object &&
                            images.TryGetProperty("small", out var small) && small.ValueKind == JsonValueKind.String)
                            rankImagem = small.GetString() ?? "";
                    }
                }
                else if (rankResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Rank e secundario: segue sem ele em vez de falhar a busca inteira.
                    _logger.LogWarning("Rank nao carregado por limite de requisicoes.");
                }

                return new Jogador
                {
                    Puuid             = data.GetProperty("puuid").GetString()!,
                    GameName          = data.GetProperty("name").GetString()!,
                    TagLine           = data.GetProperty("tag").GetString()!,
                    RankAtual         = rankAtual,
                    RankImagem        = rankImagem,
                    UltimaAtualizacao = DateTime.UtcNow
                };
            }
            catch (UpstreamUnavailableException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jogador {GameName}#{TagLine}", gameName, tagLine);
                return null;
            }
        }

        public Task<(string Rank, string Imagem)?> BuscarRankAsync(string puuid)
            => Task.FromResult<(string Rank, string Imagem)?>(null);

        public async Task<(int Season, int Act)> BuscarTemporadaAtual()
        {
            const string chave = "valorant_temporada";
            var fallback = (Season: 8, Act: 3);

            if (_cache.TryGetValue(chave, out (int Season, int Act) cached)) return cached;

            try
            {
                using var response = await _http.GetAsync("/valorant/v1/content");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Erro ao buscar conteudo da API: {StatusCode}", response.StatusCode);
                    return fallback;
                }

                var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("seasons", out var seasons) &&
                    ParsingHelpers.ExtrairTemporada(seasons) is { } atual)
                {
                    _cache.Set(chave, atual, TimeSpan.FromHours(1));
                    return atual;
                }
                return fallback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair temporada atual da API");
                return fallback;
            }
        }

        public async Task<List<Partida>> BuscarPartidas(string gameName, string tagLine, string puuid)
        {
            try
            {
                _logger.LogInformation("Buscando partidas de {GameName}#{TagLine}", gameName, tagLine);

                var n = Uri.EscapeDataString(gameName);
                var t = Uri.EscapeDataString(tagLine);

                using var response = await _http.GetAsync($"/valorant/v3/matches/{_regiao}/{n}/{t}?size=5");
                if (!Sucesso(response)) return new List<Partida>();

                var root     = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                var partidas = new List<Partida>();

                if (!root.TryGetProperty("data", out var matches) || matches.ValueKind != JsonValueKind.Array)
                    return partidas;

                foreach (var match in matches.EnumerateArray())
                {
                    try
                    {
                        if (!match.TryGetProperty("metadata", out var metadata)) continue;

                        var matchId = metadata.TryGetProperty("matchid", out var mid) ? mid.GetString() ?? "" : "";
                        var mapa    = metadata.TryGetProperty("map",  out var m)  ? m.GetString()  ?? "" : "";
                        var modo    = metadata.TryGetProperty("mode", out var mo) ? mo.GetString() ?? "" : "";

                        // Data real da partida (game_start em segundos Unix), nao a data da sincronizacao.
                        var dataPartida = DateTime.UtcNow;
                        if (metadata.TryGetProperty("game_start", out var gs) && gs.ValueKind == JsonValueKind.Number &&
                            gs.TryGetInt64(out var segundos) && segundos > 0)
                            dataPartida = DateTimeOffset.FromUnixTimeSeconds(segundos).UtcDateTime;

                        if (!match.TryGetProperty("players", out var playersRoot)) continue;
                        if (!playersRoot.TryGetProperty("all_players", out var players)) continue;

                        JsonElement? playerData = null;
                        foreach (var player in players.EnumerateArray())
                        {
                            if (player.TryGetProperty("puuid", out var p) && p.GetString() == puuid)
                            {
                                playerData = player;
                                break;
                            }
                        }

                        if (playerData == null) continue;
                        if (!playerData.Value.TryGetProperty("stats", out var stats)) continue;

                        var kills   = stats.TryGetProperty("kills",   out var k) ? k.GetInt32() : 0;
                        var deaths  = stats.TryGetProperty("deaths",  out var d) ? d.GetInt32() : 0;
                        var assists = stats.TryGetProperty("assists", out var a) ? a.GetInt32() : 0;
                        var score   = stats.TryGetProperty("score",   out var s) ? s.GetInt32() : 0;
                        var kda     = deaths == 0 ? kills + assists : Math.Round((double)(kills + assists) / deaths, 2);

                        var agente = playerData.Value.TryGetProperty("character", out var ch) ? ch.GetString() ?? "" : "";

                        var resultado = "Derrota";
                        if (playerData.Value.TryGetProperty("team", out var teamProp) && teamProp.ValueKind == JsonValueKind.String)
                        {
                            var team = teamProp.GetString()?.ToLowerInvariant();
                            if (team != null && match.TryGetProperty("teams", out var teams) &&
                                teams.TryGetProperty(team, out var teamData) &&
                                teamData.TryGetProperty("has_won", out var hasWon))
                            {
                                resultado = hasWon.GetBoolean() ? "Vitoria" : "Derrota";
                            }
                        }

                        partidas.Add(new Partida
                        {
                            MatchId = matchId, Puuid = puuid, GameName = gameName, TagLine = tagLine,
                            Mapa = mapa, Modo = modo, Agente = agente, Resultado = resultado,
                            Kills = kills, Deaths = deaths, Assists = assists, Kda = kda, Score = score,
                            DataPartida = dataPartida
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Partida ignorada: formato inesperado.");
                    }
                }

                _logger.LogInformation("{Count} partidas encontradas para {GameName}", partidas.Count, gameName);
                return partidas;
            }
            catch (UpstreamUnavailableException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar partidas de {GameName}#{TagLine}", gameName, tagLine);
                return new List<Partida>();
            }
        }

        public async Task<List<object>> BuscarAgentesDesativadosAsync()
        {
            try
            {
                const string chave = "valorant_agentes_desativados";
                if (_cache.TryGetValue(chave, out List<object>? cached) && cached != null) return cached;

                using var resposta = await _http.GetAsync($"/valorant/v1/status/{_regiao}");
                if (!resposta.IsSuccessStatusCode) return new List<object>();

                var root = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
                if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("incidents", out var incidents))
                    return new List<object>();

                var resultado = ParsingHelpers.ExtrairAgentesDesativados(incidents);
                _cache.Set(chave, resultado, TimeSpan.FromMinutes(5));
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar status de agentes desativados");
                return new List<object>();
            }
        }
    }
}

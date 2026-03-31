using System.Text.Json;
using api.models;
using Microsoft.Extensions.Logging;

namespace api.services
{
    public class HenrikService
    {
        private readonly HttpClient _http;
        private readonly ILogger<HenrikService> _logger;

        public HenrikService(HttpClient http, ILogger<HenrikService> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task<Jogador?> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                _logger.LogInformation("Buscando jogador {GameName}#{TagLine}", gameName, tagLine);

                var response = await _http.GetAsync($"/valorant/v1/account/{gameName}/{tagLine}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Jogador não encontrado: {GameName}#{TagLine}", gameName, tagLine);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json).RootElement.GetProperty("data");

                var rankResponse = await _http.GetAsync($"/valorant/v1/mmr/br/{gameName}/{tagLine}");
                var rankJson     = await rankResponse.Content.ReadAsStringAsync();
                var rankRoot     = JsonDocument.Parse(rankJson).RootElement;

                string rankAtual  = "Unranked";
                string rankImagem = "";

                if (rankRoot.TryGetProperty("data", out var rankData) &&
                    rankData.ValueKind != JsonValueKind.Null)
                {
                    if (rankData.TryGetProperty("currenttierpatched", out var tier) &&
                        tier.ValueKind != JsonValueKind.Null)
                        rankAtual = tier.GetString() ?? "Unranked";

                    if (rankData.TryGetProperty("images", out var images) &&
                        images.ValueKind != JsonValueKind.Null &&
                        images.TryGetProperty("small", out var small))
                        rankImagem = small.GetString() ?? "";
                }

                _logger.LogInformation("Jogador encontrado: {GameName} — Rank: {Rank}", gameName, rankAtual);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jogador {GameName}#{TagLine}", gameName, tagLine);
                return null;
            }
        }

        public async Task<List<Partida>> BuscarPartidas(string gameName, string tagLine, string puuid)
        {
            try
            {
                _logger.LogInformation("Buscando partidas de {GameName}#{TagLine}", gameName, tagLine);

                var response = await _http.GetAsync($"/valorant/v3/matches/br/{gameName}/{tagLine}?size=10");
                if (!response.IsSuccessStatusCode) return new List<Partida>();

                var json     = await response.Content.ReadAsStringAsync();
                var root     = JsonDocument.Parse(json).RootElement;
                var partidas = new List<Partida>();

                if (!root.TryGetProperty("data", out var matches) ||
                    matches.ValueKind == JsonValueKind.Null)
                    return partidas;

                foreach (var match in matches.EnumerateArray())
                {
                    try
                    {
                        if (!match.TryGetProperty("metadata", out var metadata)) continue;

                        // ID único da partida para evitar duplicatas
                        var matchId = metadata.TryGetProperty("matchid", out var mid)
                            ? mid.GetString() ?? ""
                            : "";

                        var mapa = metadata.TryGetProperty("map",  out var m)  ? m.GetString()  ?? "" : "";
                        var modo = metadata.TryGetProperty("mode", out var mo) ? mo.GetString() ?? "" : "";

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
                        var kda     = deaths == 0
                            ? kills + assists
                            : Math.Round((double)(kills + assists) / deaths, 2);

                        var agente = playerData.Value.TryGetProperty("character", out var ch)
                            ? ch.GetString() ?? "" : "";

                        string resultado = "Derrota";
                        if (playerData.Value.TryGetProperty("team", out var teamProp) &&
                            teamProp.ValueKind != JsonValueKind.Null)
                        {
                            var team = teamProp.GetString()?.ToLower();
                            if (team != null &&
                                match.TryGetProperty("teams", out var teams) &&
                                teams.TryGetProperty(team, out var teamData) &&
                                teamData.TryGetProperty("has_won", out var hasWon))
                            {
                                resultado = hasWon.GetBoolean() ? "Vitória" : "Derrota";
                            }
                        }

                        partidas.Add(new Partida
                        {
                            MatchId     = matchId, // ← ID único
                            Puuid       = puuid,
                            GameName    = gameName,
                            TagLine     = tagLine,
                            Mapa        = mapa,
                            Modo        = modo,
                            Agente      = agente,
                            Resultado   = resultado,
                            Kills       = kills,
                            Deaths      = deaths,
                            Assists     = assists,
                            Kda         = kda,
                            Score       = score,
                            DataPartida = DateTime.UtcNow
                        });
                    }
                    catch { continue; }
                }

                _logger.LogInformation("{Count} partidas encontradas para {GameName}", partidas.Count, gameName);
                return partidas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar partidas de {GameName}#{TagLine}", gameName, tagLine);
                return new List<Partida>();
            }
        }
    }
}
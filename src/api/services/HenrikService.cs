using System;
using System.Linq;
using System.Text.Json;
using api.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace api.services
{
    public class HenrikService
    {
        private readonly HttpClient _http;
        private readonly ILogger<HenrikService> _logger;
        private readonly IMemoryCache _cache;

        public HenrikService(HttpClient http, ILogger<HenrikService> logger, IMemoryCache cache)
        {
            _http   = http;
            _logger = logger;
            _cache  = cache;

            if (_http.BaseAddress == null)
            {
                var baseUrl = Environment.GetEnvironmentVariable("HENRIK_BASEURL") ?? "https://api.henrikdev.xyz";
                var apiKey  = Environment.GetEnvironmentVariable("HENRIK_APIKEY") ?? "";
                _http.BaseAddress = new Uri(baseUrl);
                if (!_http.DefaultRequestHeaders.Contains("Authorization"))
                    _http.DefaultRequestHeaders.Add("Authorization", apiKey);
            }
        }

        public async Task<Jogador?> BuscarJogador(string gameName, string tagLine)
        {
            try
            {
                _logger.LogInformation("Buscando jogador {GameName}#{TagLine}", gameName, tagLine);

                // Codifica os nomes para evitar quebra na URL por causa de espaços e símbolos
                var encodedGameName = Uri.EscapeDataString(gameName);
                var encodedTagLine  = Uri.EscapeDataString(tagLine);

                // Usa as variáveis codificadas na URL
                var response = await _http.GetAsync($"/valorant/v1/account/{encodedGameName}/{encodedTagLine}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Jogador não encontrado: {GameName}#{TagLine}", gameName, tagLine);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json).RootElement.GetProperty("data");

                // Usa as variáveis codificadas na URL do Rank também
                var rankResponse = await _http.GetAsync($"/valorant/v1/mmr/br/{encodedGameName}/{encodedTagLine}");
                var rankJson     = await rankResponse.Content.ReadAsStringAsync();
                var rankRoot     = JsonDocument.Parse(rankJson).RootElement;

                string rankAtual  = "Unranked";
                string rankImagem = "";

                if (rankRoot.TryGetProperty("data", out var rankData) && rankData.ValueKind != JsonValueKind.Null)
                {
                    if (rankData.TryGetProperty("currenttierpatched", out var tier) && tier.ValueKind != JsonValueKind.Null)
                        rankAtual = tier.GetString() ?? "Unranked";

                    if (rankData.TryGetProperty("images", out var images) && images.ValueKind != JsonValueKind.Null && images.TryGetProperty("small", out var small))
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

        public async Task<(int Season, int Act)> BuscarTemporadaAtual()
        {
            try
            {
                // Verifica cache primeiro
                if (_cache.TryGetValue("valorant_temporada", out (int season, int act) cached))
                    return cached;

                _logger.LogInformation("Buscando temporada/ato atual");

                var response = await _http.GetAsync("/valorant/v1/content");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Erro ao buscar conteúdo da API: {StatusCode}", response.StatusCode);
                    return (8, 3); // Fallback mais recente (Episódio 8, Ato 3)
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                int season = 8, act = 3; // Valores padrão caso não consiga ler

                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("seasons", out var seasons) &&
                    seasons.ValueKind == JsonValueKind.Array)
                {
                    JsonElement? activeAct = null;

                    // 1. Procura o Ato que tem a tag "isActive" = true
                    foreach (var s in seasons.EnumerateArray())
                    {
                        if (s.TryGetProperty("isActive", out var isActive) && isActive.GetBoolean() &&
                            s.TryGetProperty("type", out var type) && type.GetString() == "act")
                        {
                            activeAct = s;
                            break;
                        }
                    }

                    if (activeAct.HasValue)
                    {
                        // 2. Extrai o número do nome do Ato (ex: "ACT 3" vira 3)
                        var actName = activeAct.Value.GetProperty("name").GetString();
                        if (!string.IsNullOrEmpty(actName))
                        {
                            var actNumberStr = new string(actName.Where(char.IsDigit).ToArray());
                            if (int.TryParse(actNumberStr, out var parsedAct)) act = parsedAct;
                        }

                        // 3. O Ato possui um "parentId" que liga ele ao seu respectivo Episódio
                        if (activeAct.Value.TryGetProperty("parentId", out var parentIdProp))
                        {
                            var parentId = parentIdProp.GetString();
                            var activeEpisode = seasons.EnumerateArray().FirstOrDefault(x =>
                                x.TryGetProperty("id", out var idProp) && idProp.GetString() == parentId);

                            if (activeEpisode.ValueKind != JsonValueKind.Undefined)
                            {
                                var epName = activeEpisode.GetProperty("name").GetString();
                                if (!string.IsNullOrEmpty(epName))
                                {
                                    var epNumberStr = new string(epName.Where(char.IsDigit).ToArray());
                                    if (int.TryParse(epNumberStr, out var parsedEp)) season = parsedEp;
                                }
                            }
                        }
                    }
                }

                var result = (season, act);
                // Salva no cache por 1 hora
                _cache.Set("valorant_temporada", result, TimeSpan.FromHours(1));

                _logger.LogInformation("Temporada/Ato atualizada com sucesso: Season {Season}, Act {Act}", season, act);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair temporada atual da API");
                return (8, 3); // Fallback de emergência
            }
        }

        public async Task<List<Partida>> BuscarPartidas(string gameName, string tagLine, string puuid)
        {
            try
            {
                _logger.LogInformation("Buscando partidas de {GameName}#{TagLine}", gameName, tagLine);

                // Codifica os parâmetros também nas partidas
                var encodedGameName = Uri.EscapeDataString(gameName);
                var encodedTagLine  = Uri.EscapeDataString(tagLine);

                // Usa as variáveis codificadas na URL
                using var request  = new HttpRequestMessage(HttpMethod.Get, $"/valorant/v3/matches/br/{encodedGameName}/{encodedTagLine}?size=5");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode) return new List<Partida>();

                var json     = await response.Content.ReadAsStringAsync();
                var root     = JsonDocument.Parse(json).RootElement;
                var partidas = new List<Partida>();

                if (!root.TryGetProperty("data", out var matches) || matches.ValueKind == JsonValueKind.Null)
                    return partidas;

                foreach (var match in matches.EnumerateArray())
                {
                    try
                    {
                        if (!match.TryGetProperty("metadata", out var metadata)) continue;

                        var matchId = metadata.TryGetProperty("matchid", out var mid) ? mid.GetString() ?? "" : "";
                        var mapa    = metadata.TryGetProperty("map",  out var m)  ? m.GetString()  ?? "" : "";
                        var modo    = metadata.TryGetProperty("mode", out var mo) ? mo.GetString() ?? "" : "";

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

                        string resultado = "Derrota";
                        if (playerData.Value.TryGetProperty("team", out var teamProp) && teamProp.ValueKind != JsonValueKind.Null)
                        {
                            var team = teamProp.GetString()?.ToLower();
                            if (team != null && match.TryGetProperty("teams", out var teams) && teams.TryGetProperty(team, out var teamData) && teamData.TryGetProperty("has_won", out var hasWon))
                            {
                                resultado = hasWon.GetBoolean() ? "Vitoria" : "Derrota";
                            }
                        }

                        partidas.Add(new Partida
                        {
                            MatchId     = matchId,
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

        public async Task<List<object>> BuscarAgentesDesativadosAsync()
        {
            try
            {
                var resposta = await _http.GetAsync("/valorant/v1/status/br");
                if (!resposta.IsSuccessStatusCode) return new List<object>();

                var jsonString = await resposta.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(jsonString).RootElement;
                
                var desativados = new List<object>();
                
                if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("incidents", out var incidents))
                    return desativados;

                var todosAgentes = new[] { "Neon", "Omen", "Chamber", "Cypher", "Jett", "Raze", "Iso", "Viper", "Killjoy", "Astra", "Brimstone", "Clove", "Harbor", "Miks", "Phoenix", "Reyna", "Waylay", "Yoru", "Breach", "Fade", "Gekko", "KAYO", "Skye", "Sova", "Tejo", "Deadlock", "Sage", "Veto", "Vyse" };

                foreach (var incidente in incidents.EnumerateArray())
                {
                    if (incidente.TryGetProperty("titles", out var titulos))
                    {
                        foreach (var t in titulos.EnumerateArray())
                        {
                            var texto = t.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                            
                            foreach (var agente in todosAgentes)
                            {
                                if (texto.Contains(agente, StringComparison.OrdinalIgnoreCase) && 
                                   (texto.Contains("disable", StringComparison.OrdinalIgnoreCase) || 
                                    texto.Contains("desativado", StringComparison.OrdinalIgnoreCase) ||
                                    texto.Contains("issue", StringComparison.OrdinalIgnoreCase)))
                                {
                                    desativados.Add(new { 
                                        nome = agente, 
                                        motivo = texto,
                                        data = DateTime.Now.ToString("MMMM yyyy")
                                    });
                                }
                            }
                        }
                    }
                }
                return desativados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar status de agentes desativados");
                return new List<object>();
            }
        }
    }
}
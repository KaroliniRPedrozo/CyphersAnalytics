using System.Text.Json;

namespace api.services
{
    /// <summary>Trechos de parsing compartilhados entre os provedores (Henrik e Riot oficial).</summary>
    internal static class ParsingHelpers
    {
        public static readonly string[] TodosAgentes =
        {
            "Neon", "Omen", "Chamber", "Cypher", "Jett", "Raze", "Iso", "Viper", "Killjoy", "Astra", "Brimstone",
            "Clove", "Harbor", "Miks", "Phoenix", "Reyna", "Waylay", "Yoru", "Breach", "Fade", "Gekko", "KAYO",
            "Skye", "Sova", "Tejo", "Deadlock", "Sage", "Veto", "Vyse"
        };

        /// <summary>
        /// Procura o Ato ativo (type = "act" e isActive = true) e o Episodio pai.
        /// Aceita tanto data.seasons (Henrik) quanto acts (Riot oficial, onde episodios tambem vem na lista).
        /// </summary>
        public static (int Season, int Act)? ExtrairTemporada(JsonElement seasons)
        {
            if (seasons.ValueKind != JsonValueKind.Array) return null;

            JsonElement? activeAct = null;
            foreach (var s in seasons.EnumerateArray())
            {
                if (s.TryGetProperty("isActive", out var isActive) && isActive.ValueKind == JsonValueKind.True &&
                    s.TryGetProperty("type", out var type) &&
                    string.Equals(type.GetString(), "act", StringComparison.OrdinalIgnoreCase))
                {
                    activeAct = s;
                    break;
                }
            }
            if (activeAct is null) return null;

            int act = 0, season = 0;
            var actName = activeAct.Value.TryGetProperty("name", out var an) ? an.GetString() : null;
            if (int.TryParse(new string((actName ?? "").Where(char.IsDigit).ToArray()), out var parsedAct)) act = parsedAct;

            if (activeAct.Value.TryGetProperty("parentId", out var parentIdProp))
            {
                var parentId = parentIdProp.GetString();
                foreach (var x in seasons.EnumerateArray())
                {
                    if (x.TryGetProperty("id", out var idProp) &&
                        string.Equals(idProp.GetString(), parentId, StringComparison.OrdinalIgnoreCase))
                    {
                        var epName = x.TryGetProperty("name", out var en) ? en.GetString() : null;
                        if (int.TryParse(new string((epName ?? "").Where(char.IsDigit).ToArray()), out var parsedEp)) season = parsedEp;
                        break;
                    }
                }
            }

            return (act > 0 && season > 0) ? (season, act) : null;
        }

        /// <summary>Le a lista de incidentes de status e devolve os agentes citados como desativados/com problema.</summary>
        public static List<object> ExtrairAgentesDesativados(JsonElement incidents)
        {
            var desativados = new List<object>();
            if (incidents.ValueKind != JsonValueKind.Array) return desativados;

            foreach (var incidente in incidents.EnumerateArray())
            {
                if (!incidente.TryGetProperty("titles", out var titulos) || titulos.ValueKind != JsonValueKind.Array) continue;

                foreach (var t in titulos.EnumerateArray())
                {
                    var texto = t.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";

                    foreach (var agente in TodosAgentes)
                    {
                        if (texto.Contains(agente, StringComparison.OrdinalIgnoreCase) &&
                            (texto.Contains("disable", StringComparison.OrdinalIgnoreCase) ||
                             texto.Contains("desativado", StringComparison.OrdinalIgnoreCase) ||
                             texto.Contains("issue", StringComparison.OrdinalIgnoreCase)))
                        {
                            desativados.Add(new { nome = agente, motivo = texto, data = DateTime.UtcNow.ToString("MMMM yyyy") });
                        }
                    }
                }
            }
            return desativados;
        }
    }
}

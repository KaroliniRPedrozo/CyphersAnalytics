namespace api.models
{
    public class Partida
    {
        public int Id { get; set; }
        public string MatchId { get; set; } = "";
        public string Puuid { get; set; } = "";
        public string GameName { get; set; } = "";
        public string TagLine { get; set; } = "";
        public string Mapa { get; set; } = "";
        public string Modo { get; set; } = "";
        public string Agente { get; set; } = "";
        public string Resultado { get; set; } = "";
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public double Kda { get; set; }
        public int Score { get; set; }
        public DateTime DataPartida { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; } = null; // ← Soft Delete
    }
}
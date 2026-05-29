namespace api.models
{
    /// <summary>
    /// DTO para criar uma Partida com suas Estatísticas de uma só vez
    /// </summary>
    public class CriarPartidaComEstatisticasDto
    {
        // Dados da Partida
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
        
        // Dados da Estatística (opcional)
        public EstatisticaDto? Estatistica { get; set; }
    }

    /// <summary>
    /// DTO para dados da Estatística
    /// </summary>
    public class EstatisticaDto
    {
        public int AcsScore { get; set; }
        public int EconScore { get; set; }
        public int RoundasGanhas { get; set; }
        public int RoundasPerdidas { get; set; }
        public double HeadshotPercentual { get; set; }
        public int BodyShotCount { get; set; }
        public int LegShotCount { get; set; }
        public string ArmaFavorita { get; set; } = "";
        public int DanosCausados { get; set; }
        public int DanosRecebidos { get; set; }
    }
}

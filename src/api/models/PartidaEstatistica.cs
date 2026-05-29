namespace api.models
{
    public class PartidaEstatistica
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int PartidaId { get; set; }
        
        // Dados detalhados da performance
        public int AcsScore { get; set; }          // Average Combat Score
        public int EconScore { get; set; }         // Economia (gastos de economia)
        public int RoundasGanhas { get; set; }
        public int RoundasPerdidas { get; set; }
        public double HeadshotPercentual { get; set; }
        public int BodyShotCount { get; set; }
        public int LegShotCount { get; set; }
        public string ArmaFavorita { get; set; } = "";
        public int DanosCausados { get; set; }
        public int DanosRecebidos { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        
        // Navigation Property
        public Partida? Partida { get; set; }
    }
}

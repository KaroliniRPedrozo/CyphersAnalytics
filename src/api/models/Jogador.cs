namespace api.models
{
    public class Jogador
    {
        public int Id { get; set; }
        public string Puuid { get; set; } = "";
        public string GameName { get; set; } = "";
        public string TagLine { get; set; } = "";
        public string RankAtual { get; set; } = "";
        public string RankImagem { get; set; } = "";
        public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;
    }
}

namespace api.models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string SenhaHash { get; set; } = "";
        public string GameName { get; set; } = "";
        public string TagLine { get; set; } = "";
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Consentimento (LGPD / politica da Riot): o usuario autoriza o uso dos proprios dados de jogo.
        public DateTime? ConsentimentoEm { get; set; }
        public string? TermosVersao { get; set; }

        // Riot Sign On: preenchido quando a conta Riot foi verificada via OAuth.
        public string? RsoPuuid { get; set; }
        public DateTime? RsoVerificadoEm { get; set; }

        // Relacionamento 1:N (Um Usuário tem Várias Partidas)
        public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
    }
}
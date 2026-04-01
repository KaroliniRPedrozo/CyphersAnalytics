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
    }
}
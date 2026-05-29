using Microsoft.EntityFrameworkCore;
using api.models;

namespace api.database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Partida> Partidas { get; set; }
        public DbSet<Jogador> Jogadores { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<PartidaEstatistica> PartidaEstatisticas { get; set; } // ← novo DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Soft Delete para Partidas
            modelBuilder.Entity<Partida>()
                .HasQueryFilter(p => p.DeletedAt == null);
            
            // Relacionamento 1:1 entre Partida e PartidaEstatistica
            modelBuilder.Entity<Partida>()
                .HasOne(p => p.Estatistica)
                .WithOne(e => e.Partida)
                .HasForeignKey<PartidaEstatistica>(e => e.PartidaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Query filter para PartidaEstatistica seguir o filtro de Partida deletada
            modelBuilder.Entity<PartidaEstatistica>()
                .HasQueryFilter(pe => pe.Partida != null && pe.Partida.DeletedAt == null);
        }
    }
}
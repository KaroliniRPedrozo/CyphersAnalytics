using Microsoft.EntityFrameworkCore;
using api.models;

namespace api.database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Partida> Partidas { get; set; }
        public DbSet<Jogador> Jogadores { get; set; }
        public DbSet<Usuario> Usuarios { get; set; } // ← linha nova

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Partida>()
                .HasQueryFilter(p => p.DeletedAt == null);
        }
    }
}
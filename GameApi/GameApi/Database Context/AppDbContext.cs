using GameApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PlayerScore> PlayerScores { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerScore>(entity =>
            {
                entity.Property(p => p.PlayerName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Score).IsRequired();

                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Buat index agar leaderboard cepat di-sort
                entity.HasIndex(p => new { p.Score, p.Id });

                // Seed data awal
                entity.HasData(
                    new PlayerScore { Id = 1, PlayerName = "Andi", Score = 1200 },
                    new PlayerScore { Id = 2, PlayerName = "Nadia", Score = 980 }
                );
            });
        }
    }
}

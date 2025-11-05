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

                entity.Property(p => p.Score)
                      .IsRequired();

                // Gunakan CURRENT_TIMESTAMP agar SQLite mengisi waktu otomatis
                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Index untuk mempercepat query leaderboard
                entity.HasIndex(p => new { p.Score, p.Id });
            });

            // ✅ Seed data contoh dengan waktu statis (bukan DateTime.UtcNow)
            modelBuilder.Entity<PlayerScore>().HasData(
                new PlayerScore
                {
                    Id = 1,
                    PlayerName = "Andi",
                    Score = 1200,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new PlayerScore
                {
                    Id = 2,
                    PlayerName = "Nadia",
                    Score = 980,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

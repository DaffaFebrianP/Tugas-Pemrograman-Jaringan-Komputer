using GameApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tabel utama leaderboard
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

                // Nilai default waktu otomatis dari SQLite
                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Kolom tambahan leaderboard (opsional, tergantung model)
                entity.Property(p => p.Range)
                      .HasDefaultValue(0);

                entity.Property(p => p.TimeSpent)
                      .HasDefaultValue(0);

                entity.Property(p => p.PresentsCollected)
                      .HasDefaultValue(0);

                // Index untuk mempercepat query leaderboard
                entity.HasIndex(p => new { p.Score, p.Id });
            });

            // ⚠️ Tidak lagi pakai HasData untuk mencegah migrasi duplikat
        }
    }
}

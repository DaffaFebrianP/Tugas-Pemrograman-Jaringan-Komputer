using System;
using System.ComponentModel.DataAnnotations;

namespace GameApi.Models
{
    public class PlayerScore
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string PlayerName { get; set; } = string.Empty;

        // 🧮 Skor total (akan dihitung otomatis di Controller)
        public int Score { get; set; }

        // 🧭 Data tambahan untuk analisis leaderboard
        public float Range { get; set; }              // jarak tempuh (meter)
        public float TimeSpent { get; set; }          // waktu bermain (detik)
        public int PresentsCollected { get; set; }    // hadiah yang dikumpulkan

        // 🕒 Waktu penyimpanan otomatis (diisi oleh SQLite)
        public DateTime CreatedAt { get; set; }
    }
}

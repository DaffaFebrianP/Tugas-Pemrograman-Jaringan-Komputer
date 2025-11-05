using System;

namespace GameApi.Models
{
    public class PlayerScore
    {
        public int Id { get; set; }

        public string PlayerName { get; set; } = string.Empty;

        public int Score { get; set; }

        public double? Speed { get; set; }   // opsional: untuk menyimpan kecepatan pemain

        public DateTime CreatedAt { get; set; } // ❌ tidak lagi diberi = DateTime.UtcNow;
    }
}

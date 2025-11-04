using System;

namespace GameApi.Models
{
    public class PlayerScore
    {
        // Primary key
        public int Id { get; set; }

        // Nama pemain, wajib diisi
        public string PlayerName { get; set; } = string.Empty;

        // Nilai skor pemain
        public int Score { get; set; }

        // Waktu pencatatan data (default diisi otomatis oleh database lewat HasDefaultValueSql)
        public DateTime CreatedAt { get; set; }
    }
}

using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ScoresController(AppDbContext db)
        {
            _db = db;
        }

        // =====================================================================
        // 1️⃣ GET /api/scores?page=1&pageSize=10
        // =====================================================================
        // Ambil semua data skor (urut dari skor tertinggi)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerScore>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = (pageSize < 1 || pageSize > 100) ? 10 : pageSize;

            var query = _db.PlayerScores.AsNoTracking()
                                        .OrderByDescending(s => s.Score)
                                        .ThenBy(s => s.Id);

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            // Tambahkan header jumlah total data
            Response.Headers["X-Total-Count"] = total.ToString();

            return Ok(data);
        }

        // =====================================================================
        // 2️⃣ GET /api/scores/{id}
        // =====================================================================
        // Ambil data berdasarkan ID
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PlayerScore>> GetById(int id)
        {
            var item = await _db.PlayerScores.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // =====================================================================
        // 3️⃣ GET /api/scores/top/{n}
        // =====================================================================
        // Ambil N skor tertinggi (default 10)
        [HttpGet("top/{n:int}")]
        public async Task<ActionResult<IEnumerable<PlayerScore>>> GetTopN(int n)
        {
            n = (n < 1 || n > 100) ? 10 : n;

            var data = await _db.PlayerScores.AsNoTracking()
                                             .OrderByDescending(s => s.Score)
                                             .ThenBy(s => s.Id)
                                             .Take(n)
                                             .ToListAsync();

            return Ok(data);
        }

        // =====================================================================
        // 4️⃣ GET /api/scores/rank/{playerName}
        // =====================================================================
        // Hitung peringkat pemain berdasarkan skor terbaru miliknya
        [HttpGet("rank/{playerName}")]
        public async Task<ActionResult<object>> GetRank(string playerName)
        {
            playerName = playerName.Trim();

            if (string.IsNullOrWhiteSpace(playerName))
                return BadRequest("playerName is required");

            var latest = await _db.PlayerScores
                                  .Where(p => p.PlayerName == playerName)
                                  .OrderByDescending(p => p.CreatedAt)
                                  .ThenByDescending(p => p.Id)
                                  .FirstOrDefaultAsync();

            if (latest is null)
                return NotFound("player not found");

            var betterCount = await _db.PlayerScores.CountAsync(p => p.Score > latest.Score);
            var rank = betterCount + 1;

            return Ok(new
            {
                player = playerName,
                score = latest.Score,
                rank
            });
        }

        // =====================================================================
        // 5️⃣ POST /api/scores
        // =====================================================================
        // Tambahkan skor baru
        [HttpPost]
        public async Task<ActionResult<PlayerScore>> Create([FromBody] PlayerScore input)
        {
            if (string.IsNullOrWhiteSpace(input.PlayerName))
                return BadRequest("PlayerName is required");

            if (input.Score < 0)
                return BadRequest("Score must be >= 0");

            _db.PlayerScores.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        // =====================================================================
        // 6️⃣ PUT /api/scores/{id}
        // =====================================================================
        // Update data berdasarkan ID
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PlayerScore input)
        {
            if (id != input.Id)
                return BadRequest("Route ID and body ID must match");

            var exists = await _db.PlayerScores.AnyAsync(s => s.Id == id);
            if (!exists)
                return NotFound();

            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return NoContent(); // HTTP 204 = sukses tanpa isi
        }

        // =====================================================================
        // 7️⃣ DELETE /api/scores/{id}
        // =====================================================================
        // Hapus data berdasarkan ID
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.PlayerScores.FindAsync(id);
            if (item is null)
                return NotFound();

            _db.PlayerScores.Remove(item);
            await _db.SaveChangesAsync();

            return NoContent(); // HTTP 204
        }
    }
}

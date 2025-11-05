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
        public ScoresController(AppDbContext db) => _db = db;

        // GET /api/scores?page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerScore>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = (pageSize < 1 || pageSize > 100) ? 10 : pageSize;

            var query = _db.PlayerScores.AsNoTracking()
                                        .OrderByDescending(s => s.Score)
                                        .ThenBy(s => s.Id);

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(data);
        }

        // GET /api/scores/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PlayerScore>> GetById(int id)
        {
            var item = await _db.PlayerScores.FindAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        // GET /api/scores/top/{n}
        [HttpGet("top/{n:int}")]
        public async Task<ActionResult<IEnumerable<PlayerScore>>> TopN(int n = 10)
        {
            n = (n < 1 || n > 100) ? 10 : n;
            var data = await _db.PlayerScores.AsNoTracking()
                                             .OrderByDescending(s => s.Score)
                                             .ThenBy(s => s.Id)
                                             .Take(n)
                                             .ToListAsync();
            return Ok(data);
        }

        // GET /api/scores/rank/{playerName}
        [HttpGet("rank/{playerName}")]
        public async Task<ActionResult<object>> GetRank(string playerName)
        {
            playerName = playerName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(playerName)) return BadRequest("playerName required");

            var latest = await _db.PlayerScores
                                  .Where(p => p.PlayerName == playerName)
                                  .OrderByDescending(p => p.CreatedAt)
                                  .ThenByDescending(p => p.Id)
                                  .FirstOrDefaultAsync();
            if (latest == null) return NotFound("player not found");

            var betterCount = await _db.PlayerScores.CountAsync(p => p.Score > latest.Score);
            var rank = betterCount + 1;
            return Ok(new { player = playerName, score = latest.Score, rank });
        }

        // POST /api/scores
        [HttpPost]
        public async Task<ActionResult<PlayerScore>> Create([FromBody] PlayerScore input)
        {
            if (string.IsNullOrWhiteSpace(input.PlayerName)) return BadRequest("PlayerName is required");
            if (input.Score < 0) return BadRequest("Score must be >= 0");

            _db.PlayerScores.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        // PUT /api/scores/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PlayerScore input)
        {
            if (id != input.Id) return BadRequest("Route id != body id");

            var exists = await _db.PlayerScores.AnyAsync(s => s.Id == id);
            if (!exists) return NotFound();

            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/scores/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.PlayerScores.FindAsync(id);
            if (item == null) return NotFound();

            _db.PlayerScores.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

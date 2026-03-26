using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayersManager.Data;
using PlayersManager.Dtos;
using PlayersManager.Models;

namespace PlayersManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlayersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Import a list of players as a new batch with a manual date.
    /// Records are matched against existing players by nickname.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportBatchDto dto)
    {
        if (dto.Players is null || dto.Players.Count == 0)
            return BadRequest("Player list is empty.");

        var batch = new Batch { Date = dto.Date };

        var existingPlayers = await _db.Players
            .Include(p => p.History)
            .ToListAsync();
        var playerByNickname = existingPlayers.ToDictionary(p => p.Nickname);

        var historicalNicknames = await _db.HistoricalPlayerRecords
            .Select(h => new { h.Nickname, h.PlayerId })
            .ToListAsync();

        var playerByHistoricalNickname = historicalNicknames
            .GroupBy(h => h.Nickname)
            .ToDictionary(
                g => g.Key,
                g => g.First().PlayerId);

        foreach (var entry in dto.Players)
        {
            Player? matchedPlayer = null;

            if (playerByNickname.TryGetValue(entry.Nickname, out var directMatch))
            {
                matchedPlayer = directMatch;
            }
            else if (playerByHistoricalNickname.TryGetValue(entry.Nickname, out var historicalPlayerId))
            {
                matchedPlayer = existingPlayers.FirstOrDefault(p => p.Id == historicalPlayerId);
            }

            batch.Records.Add(new BatchRecord
            {
                Nickname = entry.Nickname,
                Power = entry.Power,
                TownHallLevel = entry.TownHallLevel,
                MatchStatus = matchedPlayer is not null ? MatchStatus.Matched : MatchStatus.NotMatched
            });

            if (matchedPlayer is not null)
            {
                var record = new HistoricalPlayerRecord
                {
                    PlayerId = matchedPlayer.Id,
                    Batch = batch,
                    Nickname = entry.Nickname,
                    Power = entry.Power,
                    TownHallLevel = entry.TownHallLevel,
                    RecordedAt = dto.Date
                };
                _db.HistoricalPlayerRecords.Add(record);
            }
        }

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync();

        return Ok(new { batch.Id, batch.Date, RecordCount = batch.Records.Count });
    }

    /// <summary>
    /// Get all batches.
    /// </summary>
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches()
    {
        var batches = await _db.Batches
            .Select(b => new { b.Id, b.Date, RecordCount = b.Records.Count })
            .OrderByDescending(b => b.Date)
            .ToListAsync();

        return Ok(batches);
    }

    /// <summary>
    /// Get all records for a specific batch, including missing players.
    /// </summary>
    [HttpGet("batches/{batchId}/records")]
    public async Task<IActionResult> GetBatchRecords(int batchId)
    {
        var batch = await _db.Batches.FindAsync(batchId);
        if (batch is null)
            return NotFound();

        var records = await _db.BatchRecords
            .Where(r => r.BatchId == batchId)
            .Select(r => new { r.Id, r.Nickname, r.Power, r.TownHallLevel, MatchStatus = (string)(object)r.MatchStatus })
            .ToListAsync();

        var batchNicknames = records.Select(r => r.Nickname).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var historicalLinks = await _db.HistoricalPlayerRecords
            .Where(h => h.BatchId == batchId)
            .Select(h => h.PlayerId)
            .ToListAsync();

        var matchedPlayerIds = historicalLinks.ToHashSet();

        var allPlayers = await _db.Players
            .Select(p => new { p.Id, p.Nickname, p.Status })
            .ToListAsync();

        var missingPlayers = allPlayers
            .Where(p => p.Status == PlayerStatus.Active && !batchNicknames.Contains(p.Nickname) && !matchedPlayerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nickname, Status = p.Status.ToString() })
            .ToList();

        return Ok(new
        {
            Records = records,
            MatchedPlayerIds = matchedPlayerIds.ToList(),
            MissingPlayers = missingPlayers
        });
    }

    /// <summary>
    /// Delete a batch and its records.
    /// </summary>
    [HttpDelete("batches/{batchId}")]
    public async Task<IActionResult> DeleteBatch(int batchId)
    {
        var batch = await _db.Batches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
            return NotFound();

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get all players (latest state). Returns only active players by default.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlayers([FromQuery] bool includeInactive = false)
    {
        var query = _db.Players.AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.Status == PlayerStatus.Active);

        var players = await query
            .Select(p => new
            {
                p.Id,
                p.Nickname,
                LatestRecord = p.History.OrderByDescending(h => h.RecordedAt).FirstOrDefault(),
                Status = (string)(object)p.Status
            })
            .Select(x => new
            {
                x.Id,
                x.Nickname,
                Power = x.LatestRecord != null ? x.LatestRecord.Power : "",
                TownHallLevel = x.LatestRecord != null ? x.LatestRecord.TownHallLevel : "",
                x.Status,
                LastUpdatedAt = x.LatestRecord != null ? (DateTime?)x.LatestRecord.RecordedAt : null
            })
            .ToListAsync();

        return Ok(players);
    }

    /// <summary>
    /// Delete all players and their history.
    /// </summary>
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAllPlayers()
    {
        _db.HistoricalPlayerRecords.RemoveRange(_db.HistoricalPlayerRecords);
        _db.Players.RemoveRange(_db.Players);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a player.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player is null)
            return NotFound();

        _db.Players.Remove(player);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update a player's nickname.
    /// </summary>
    [HttpPatch("{id}/nickname")]
    public async Task<IActionResult> UpdateNickname(int id, [FromBody] UpdateNicknameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nickname))
            return BadRequest("Nickname cannot be empty.");

        var player = await _db.Players
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player is null)
            return NotFound();

        var duplicate = await _db.Players
            .AnyAsync(p => p.Nickname == dto.Nickname && p.Id != id);

        if (duplicate)
            return Conflict("A player with this nickname already exists.");

        player.Nickname = dto.Nickname;
        await _db.SaveChangesAsync();

        var latestRecord = player.History.OrderByDescending(h => h.RecordedAt).FirstOrDefault();
        return Ok(new
        {
            player.Id,
            player.Nickname,
            Power = latestRecord?.Power ?? "",
            TownHallLevel = latestRecord?.TownHallLevel ?? "",
            Status = player.Status.ToString()
        });
    }

    /// <summary>
    /// Toggle a player's status between Active and Inactive.
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var player = await _db.Players
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player is null)
            return NotFound();

        player.Status = player.Status == PlayerStatus.Active
            ? PlayerStatus.Inactive
            : PlayerStatus.Active;

        await _db.SaveChangesAsync();

        var latestRecord = player.History.OrderByDescending(h => h.RecordedAt).FirstOrDefault();
        return Ok(new
        {
            player.Id,
            player.Nickname,
            Power = latestRecord?.Power ?? "",
            TownHallLevel = latestRecord?.TownHallLevel ?? "",
            Status = player.Status.ToString()
        });
    }

    /// <summary>
    /// Create a player from an unmatched batch record.
    /// </summary>
    [HttpPost("from-record/{recordId}")]
    public async Task<IActionResult> AddPlayerFromRecord(int recordId)
    {
        var record = await _db.BatchRecords
            .Include(r => r.Batch)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (record is null)
            return NotFound();

        if (record.MatchStatus == MatchStatus.Matched)
            return BadRequest("This record is already matched.");

        var duplicate = await _db.Players
            .AnyAsync(p => p.Nickname == record.Nickname);

        if (duplicate)
            return Conflict("A player with this nickname already exists.");

        var player = new Player { Nickname = record.Nickname };
        _db.Players.Add(player);

        var historyRecord = new HistoricalPlayerRecord
        {
            Player = player,
            BatchId = record.BatchId,
            Nickname = record.Nickname,
            Power = record.Power,
            TownHallLevel = record.TownHallLevel,
            RecordedAt = record.Batch.Date
        };
        _db.HistoricalPlayerRecords.Add(historyRecord);

        record.MatchStatus = MatchStatus.Matched;
        await _db.SaveChangesAsync();

        return Ok(new { record.Id, record.Nickname, record.Power, record.TownHallLevel });
    }

    /// <summary>
    /// Manually match an unmatched batch record to an existing player.
    /// </summary>
    [HttpPost("match-record/{recordId}")]
    public async Task<IActionResult> ManualMatchRecord(int recordId, [FromBody] ManualMatchDto dto)
    {
        var record = await _db.BatchRecords
            .Include(r => r.Batch)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (record is null)
            return NotFound();

        if (record.MatchStatus == MatchStatus.Matched)
            return BadRequest("This record is already matched.");

        var player = await _db.Players
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => p.Id == dto.PlayerId);

        if (player is null)
            return NotFound("Player not found.");

        var historyRecord = new HistoricalPlayerRecord
        {
            PlayerId = player.Id,
            BatchId = record.BatchId,
            Nickname = record.Nickname,
            Power = record.Power,
            TownHallLevel = record.TownHallLevel,
            RecordedAt = record.Batch.Date
        };
        _db.HistoricalPlayerRecords.Add(historyRecord);

        record.MatchStatus = MatchStatus.Matched;
        await _db.SaveChangesAsync();

        return Ok(new { record.Id, RecordNickname = record.Nickname, PlayerId = player.Id, PlayerNickname = player.Nickname });
    }

    /// <summary>
    /// Create players from all unmatched batch records at once.
    /// </summary>
    [HttpPost("from-batch/{batchId}")]
    public async Task<IActionResult> AddAllPlayersFromBatch(int batchId)
    {
        var batch = await _db.Batches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
            return NotFound();

        var unmatchedRecords = batch.Records
            .Where(r => r.MatchStatus == MatchStatus.NotMatched)
            .ToList();

        if (unmatchedRecords.Count == 0)
            return BadRequest("No unmatched records in this batch.");

        var existingNicknames = await _db.Players
            .Select(p => p.Nickname)
            .ToListAsync();

        var nicknameSet = new HashSet<string>(existingNicknames, StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var playerRecordPairs = new List<(Player player, HistoricalPlayerRecord record)>();

        foreach (var record in unmatchedRecords)
        {
            if (nicknameSet.Contains(record.Nickname))
                continue;

            var player = new Player { Nickname = record.Nickname };
            _db.Players.Add(player);

            var historyRecord = new HistoricalPlayerRecord
            {
                Player = player,
                BatchId = batch.Id,
                Nickname = record.Nickname,
                Power = record.Power,
                TownHallLevel = record.TownHallLevel,
                RecordedAt = batch.Date
            };
            _db.HistoricalPlayerRecords.Add(historyRecord);
            playerRecordPairs.Add((player, historyRecord));

            record.MatchStatus = MatchStatus.Matched;
            nicknameSet.Add(record.Nickname);
            added++;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Added = added, BatchId = batchId });
    }

    /// <summary>
    /// Deactivate all players not found in the given batch.
    /// </summary>
    [HttpPatch("deactivate-missing/{batchId}")]
    public async Task<IActionResult> DeactivateMissingPlayers(int batchId)
    {
        var batch = await _db.Batches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
            return NotFound();

        var batchNicknames = batch.Records
            .Select(r => r.Nickname)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedPlayerIds = await _db.HistoricalPlayerRecords
            .Where(h => h.BatchId == batchId)
            .Select(h => h.PlayerId)
            .Distinct()
            .ToListAsync();

        var matchedSet = matchedPlayerIds.ToHashSet();

        var players = await _db.Players
            .Where(p => p.Status == PlayerStatus.Active)
            .ToListAsync();

        var deactivated = 0;
        foreach (var player in players)
        {
            if (!batchNicknames.Contains(player.Nickname) && !matchedSet.Contains(player.Id))
            {
                player.Status = PlayerStatus.Inactive;
                deactivated++;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Deactivated = deactivated, BatchId = batchId });
    }

    /// <summary>
    /// Get player details with full history.
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetPlayerDetails(int id)
    {
        var player = await _db.Players
            .Include(p => p.History.OrderByDescending(h => h.RecordedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player is null)
            return NotFound();

        var latestRecord = player.History.FirstOrDefault();
        return Ok(new
        {
            player.Id,
            player.Nickname,
            Power = latestRecord?.Power ?? "",
            TownHallLevel = latestRecord?.TownHallLevel ?? "",
            Status = player.Status.ToString(),
            LastUpdatedAt = latestRecord?.RecordedAt,
            History = player.History.Select(h => new
            {
                h.Id,
                h.Nickname,
                h.Power,
                h.TownHallLevel,
                h.RecordedAt,
                h.BatchId
            })
        });
    }

    /// <summary>
    /// Get statistics: player power across all batch dates.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var batches = await _db.Batches
            .OrderBy(b => b.Date)
            .Select(b => new { b.Id, b.Date })
            .ToListAsync();

        var allRecords = await _db.HistoricalPlayerRecords
            .Select(r => new { r.PlayerId, r.BatchId, r.Power })
            .ToListAsync();

        var recordLookup = allRecords
            .GroupBy(r => r.PlayerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(r => r.BatchId, r => r.Power));

        var players = await _db.Players
            .OrderBy(p => p.Nickname)
            .Where(p => p.Status == PlayerStatus.Active)
            .Select(p => new { p.Id, p.Nickname })
            .ToListAsync();

        var result = players.Select(p =>
        {
            recordLookup.TryGetValue(p.Id, out var powers);
            return new
            {
                p.Nickname,
                Powers = batches.Select(b =>
                    powers != null && powers.TryGetValue(b.Id, out var power) ? power : (string?)null
                ).ToList()
            };
        }).ToList();

        return Ok(new
        {
            Dates = batches.Select(b => b.Date),
            Players = result
        });
    }

    /// <summary>
    /// Get all historical records.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistoricalRecords()
    {
        var records = await _db.HistoricalPlayerRecords
            .Include(h => h.Player)
            .Include(h => h.Batch)
            .OrderByDescending(h => h.RecordedAt)
            .Select(h => new
            {
                h.Id,
                h.PlayerId,
                PlayerNickname = h.Player.Nickname,
                h.BatchId,
                BatchDate = h.Batch.Date,
                h.Nickname,
                h.Power,
                h.TownHallLevel,
                h.RecordedAt
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>
    /// Get a single historical record.
    /// </summary>
    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetHistoricalRecord(int id)
    {
        var record = await _db.HistoricalPlayerRecords
            .Include(h => h.Player)
            .Include(h => h.Batch)
            .Where(h => h.Id == id)
            .Select(h => new
            {
                h.Id,
                h.PlayerId,
                PlayerNickname = h.Player.Nickname,
                h.BatchId,
                BatchDate = h.Batch.Date,
                h.Nickname,
                h.Power,
                h.TownHallLevel,
                h.RecordedAt
            })
            .FirstOrDefaultAsync();

        if (record is null)
            return NotFound();

        return Ok(record);
    }

    /// <summary>
    /// Create a new historical record.
    /// </summary>
    [HttpPost("history")]
    public async Task<IActionResult> CreateHistoricalRecord([FromBody] HistoricalRecordDto dto)
    {
        var player = await _db.Players.FindAsync(dto.PlayerId);
        if (player is null)
            return NotFound("Player not found.");

        var batch = await _db.Batches.FindAsync(dto.BatchId);
        if (batch is null)
            return NotFound("Batch not found.");

        var record = new HistoricalPlayerRecord
        {
            PlayerId = dto.PlayerId,
            BatchId = dto.BatchId,
            Nickname = dto.Nickname,
            Power = dto.Power,
            TownHallLevel = dto.TownHallLevel,
            RecordedAt = dto.RecordedAt
        };

        _db.HistoricalPlayerRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { record.Id, record.PlayerId, record.BatchId, record.Nickname, record.Power, record.TownHallLevel, record.RecordedAt });
    }

    /// <summary>
    /// Update an existing historical record.
    /// </summary>
    [HttpPut("history/{id}")]
    public async Task<IActionResult> UpdateHistoricalRecord(int id, [FromBody] HistoricalRecordDto dto)
    {
        var record = await _db.HistoricalPlayerRecords.FindAsync(id);
        if (record is null)
            return NotFound();

        var player = await _db.Players.FindAsync(dto.PlayerId);
        if (player is null)
            return NotFound("Player not found.");

        var batch = await _db.Batches.FindAsync(dto.BatchId);
        if (batch is null)
            return NotFound("Batch not found.");

        record.PlayerId = dto.PlayerId;
        record.BatchId = dto.BatchId;
        record.Nickname = dto.Nickname;
        record.Power = dto.Power;
        record.TownHallLevel = dto.TownHallLevel;
        record.RecordedAt = dto.RecordedAt;

        await _db.SaveChangesAsync();

        return Ok(new { record.Id, record.PlayerId, record.BatchId, record.Nickname, record.Power, record.TownHallLevel, record.RecordedAt });
    }

    /// <summary>
    /// Delete a historical record.
    /// </summary>
    [HttpDelete("history/{id}")]
    public async Task<IActionResult> DeleteHistoricalRecord(int id)
    {
        var record = await _db.HistoricalPlayerRecords.FindAsync(id);
        if (record is null)
            return NotFound();

        _db.HistoricalPlayerRecords.Remove(record);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

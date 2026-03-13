using Microsoft.EntityFrameworkCore;
using PlayersManager.Models;

namespace PlayersManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchRecord> BatchRecords => Set<BatchRecord>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<HistoricalPlayerRecord> HistoricalPlayerRecords => Set<HistoricalPlayerRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasMany(b => b.Records)
                  .WithOne(r => r.Batch)
                  .HasForeignKey(r => r.BatchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(p => p.Nickname).HasMaxLength(255);
            entity.HasIndex(p => p.Nickname).IsUnique();
            entity.Property(p => p.Status).HasConversion<string>();
            entity.HasMany(p => p.History)
                  .WithOne(h => h.Player)
                  .HasForeignKey(h => h.PlayerId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BatchRecord>(entity =>
        {
            entity.Property(r => r.MatchStatus).HasConversion<string>();
        });
    }
}

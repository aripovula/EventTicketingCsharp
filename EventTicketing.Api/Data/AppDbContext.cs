using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.FamilyId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .ToTable(t => t.HasCheckConstraint(
                "ck_events_available_seats", "available_seats >= 0 AND available_seats <= total_seats"));

        // "C" collation makes title order identical on every OS (glibc, musl/Alpine CI)
        // and matches .NET ordinal comparison, so keyset cursors compare the same way.
        modelBuilder.Entity<Event>().Property(e => e.Title).UseCollation("C");

        // One index per list sort key, with id as the keyset-pagination tiebreaker.
        modelBuilder.Entity<Event>().HasIndex(e => new { e.Title, e.Id });
        modelBuilder.Entity<Event>().HasIndex(e => new { e.StartTime, e.Id });
        modelBuilder.Entity<Event>().HasIndex(e => new { e.PriceCents, e.Id });

        // Deleting a user keeps their order history; deleting an event removes its orders.
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Email);

        // One remembered response per key and endpoint.
        modelBuilder.Entity<IdempotencyKey>()
            .HasIndex(k => new { k.Key, k.RequestPath })
            .IsUnique();

        modelBuilder.Entity<IdempotencyKey>()
            .Property(k => k.ResponseBody)
            .HasColumnType("jsonb");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}

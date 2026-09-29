using Microsoft.EntityFrameworkCore;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per entity — these become your database tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatCategory> SeatCategories => Set<SeatCategory>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All enums across ALL tables store as string automatically
        // Database shows "Pending" not 0 — readable, debuggable
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly automatically
        // Picks up every IEntityTypeConfiguration<T> class we create
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters — soft delete applied automatically to every query
        // You never need to write WHERE IsDeleted = false manually
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Seat>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<SeatCategory>().HasQueryFilter(sc => !sc.IsDeleted);
        modelBuilder.Entity<Booking>().HasQueryFilter(b => !b.IsDeleted);
        modelBuilder.Entity<BookingSeat>().HasQueryFilter(bs => !bs.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => !rt.IsDeleted);
        modelBuilder.Entity<OutboxMessage>().HasQueryFilter(om => !om.IsDeleted);
    }
}
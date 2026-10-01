using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ArtistName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.VenueName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.VenueCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Index for common query — list upcoming events by city and date
        builder.HasIndex(e => new { e.VenueCity, e.EventDate });
        builder.HasIndex(e => e.Status);

        // One event has many seats
        builder.HasMany(e => e.Seats)
            .WithOne(s => s.Event)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // One event has many seat categories
        builder.HasMany(e => e.SeatCategories)
            .WithOne(sc => sc.Event)
            .HasForeignKey(sc => sc.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // One event has many bookings
        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.CreatedBy)
            .WithMany(u => u.CreatedEvents)
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
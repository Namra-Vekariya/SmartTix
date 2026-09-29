using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.PaymentStatus)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.PaymentReference)
            .HasMaxLength(100);

        builder.Property(b => b.HoldId)
            .HasMaxLength(100);

        // BookingReference must be unique — SMX-2025-00423 belongs to one booking only
        builder.HasIndex(b => b.BookingReference)
            .IsUnique();

        // Fast lookup for user's booking history
        builder.HasIndex(b => new { b.UserId, b.Status });

        // One booking has many seats
        builder.HasMany(b => b.BookingSeats)
            .WithOne(bs => bs.Booking)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
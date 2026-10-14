using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("seats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RowLabel)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(s => s.SeatCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(20);

        // No two seats in same event can have same row + number
        // This is the database-level guarantee against duplicate seats
        builder.HasIndex(s => new { s.EventId, s.RowLabel, s.SeatNumber })
            .IsUnique();

        // Index for fast seat map queries
        builder.HasIndex(s => new { s.EventId, s.Status });
    }
}
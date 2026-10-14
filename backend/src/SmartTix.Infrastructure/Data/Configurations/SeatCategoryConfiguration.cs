using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class SeatCategoryConfiguration : IEntityTypeConfiguration<SeatCategory>
{
    public void Configure(EntityTypeBuilder<SeatCategory> builder)
    {
        builder.ToTable("seat_categories");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Price)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(sc => sc.ColorHex)
            .HasMaxLength(7);   // "#FF5733"
    }
}
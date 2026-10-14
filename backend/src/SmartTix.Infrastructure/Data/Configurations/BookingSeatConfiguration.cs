using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("booking_seats");

        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.PriceAtBooking)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
    }
}
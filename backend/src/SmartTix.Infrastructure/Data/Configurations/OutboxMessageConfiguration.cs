using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(om => om.Id);

        builder.Property(om => om.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(om => om.Payload)
            .IsRequired()
            .HasColumnType("jsonb");   // PostgreSQL JSONB — faster querying than plain JSON

        builder.Property(om => om.Status)
            .IsRequired()
            .HasMaxLength(20);

        // OutboxWorker queries this index every 30 seconds
        builder.HasIndex(om => om.Status);
    }
}
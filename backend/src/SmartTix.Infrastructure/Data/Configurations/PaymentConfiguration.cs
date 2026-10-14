using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);      // INR, USD etc

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(p => p.GatewayOrderId)
            .HasMaxLength(100);

        builder.Property(p => p.GatewayPaymentId)
            .HasMaxLength(100);

        builder.Property(p => p.GatewaySignature)
            .HasMaxLength(500);

        // Store webhook as jsonb — fast querying, flexible structure
        builder.Property(p => p.WebhookPayload)
            .HasColumnType("jsonb");

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        // Index for looking up payment by gateway IDs
        // Razorpay webhooks send gatewayPaymentId — need fast lookup
        builder.HasIndex(p => p.GatewayOrderId);
        builder.HasIndex(p => p.GatewayPaymentId);

        // One booking can have multiple payment attempts
        // User pays, fails, tries again — each attempt is a new payment row
        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
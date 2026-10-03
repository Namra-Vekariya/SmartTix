using SmartTix.Domain.Common;
using SmartTix.Domain.Enums;

namespace SmartTix.Domain.Entities;

public class Payment : BaseEntity
{
    // Internal reference
    public Guid BookingId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Payment method — Card, UPI, NetBanking, Wallet
    public string? PaymentMethod { get; set; }

    // ── Razorpay specific fields ─────────────────────────────
    // Created when you call Razorpay create order API
    public string? GatewayOrderId { get; set; }      // rzp_order_xxx

    // Returned by Razorpay after user pays
    public string? GatewayPaymentId { get; set; }    // pay_xxx

    // HMAC SHA256 signature — verified on backend to confirm payment is genuine
    // Never trust frontend alone — always verify signature server-side
    public string? GatewaySignature { get; set; }

    // Raw webhook payload — store for debugging and audit
    public string? WebhookPayload { get; set; }

    // When payment was attempted
    public DateTime? PaidAt { get; set; }

    // If payment failed — why
    public string? FailureReason { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
}
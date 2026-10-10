using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes transactional outbox messages. Every tick it picks up Pending
/// BookingConfirmed rows, loads the booking, generates the ticket PDF with
/// QuestPDF and emails it with the attachment. Failures increment RetryCount
/// and are retried on later ticks until MaxRetries, then marked Failed.
/// </summary>
public class OutboxEmailWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;
    private const int MaxRetries = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxEmailWorker> _logger;

    public OutboxEmailWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxEmailWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox email worker started (interval: {Interval}s, batch: {Batch})",
            PollingInterval.TotalSeconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — fall through to exit the loop.
            }
            catch (Exception ex)
            {
                // Never let a transient failure (DB blip, SMTP outage) kill the
                // host — log it and try again on the next tick.
                _logger.LogError(ex, "Outbox email sweep failed");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox email worker stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        // Repositories are DbContext-bound and scoped, so each tick resolves its
        // own scope — same unit of work as an HTTP request.
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var ticketPdfService = scope.ServiceProvider.GetRequiredService<ITicketPdfService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var messages = await outboxRepository.GetPendingBatchAsync(BatchSize, MaxRetries);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(
                    message,
                    bookingRepository,
                    ticketPdfService,
                    emailService,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Transient failure (SMTP down, PDF error) — keep the message
                // Pending so the next tick retries it, until MaxRetries is hit.
                message.RetryCount++;
                message.ErrorMessage = ex.Message;

                if (message.RetryCount >= MaxRetries)
                {
                    message.Status = OutboxMessageStatus.Failed;
                    _logger.LogError(ex,
                        "Outbox message {MessageId} failed permanently after {Attempts} attempt(s)",
                        message.Id, message.RetryCount);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Outbox message {MessageId} failed (attempt {Attempt}/{MaxAttempts}) — will retry",
                        message.Id, message.RetryCount, MaxRetries);
                }
            }

            // Persist status per message so progress survives a crash mid-batch.
            await outboxRepository.SaveChangesAsync();
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IBookingRepository bookingRepository,
        ITicketPdfService ticketPdfService,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        if (message.EventType != OutboxMessage.BookingConfirmedEvent)
        {
            MarkFailed(message, $"Unsupported event type '{message.EventType}'");
            return;
        }

        Guid bookingId;
        try
        {
            using var payload = JsonDocument.Parse(message.Payload);
            bookingId = payload.RootElement.GetProperty("BookingId").GetGuid();
        }
        catch (Exception ex)
        {
            MarkFailed(message, $"Invalid payload: {ex.Message}");
            return;
        }

        var booking = await bookingRepository.GetByIdWithDetailsAsync(bookingId);

        if (booking is null)
        {
            MarkFailed(message, "Booking not found");
            return;
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            MarkFailed(message, $"Booking is {booking.Status} — confirmation email skipped");
            return;
        }

        if (string.IsNullOrWhiteSpace(booking.BookerEmail))
        {
            MarkFailed(message, "Booking has no booker email address");
            return;
        }

        var pdfBytes = ticketPdfService.GenerateTicketPdf(booking);

        await emailService.SendEmailWithAttachmentAsync(
            recipientEmail: booking.BookerEmail,
            subject: $"Booking Confirmed — {booking.BookingReference}",
            body: BuildBookingConfirmationEmail(booking),
            attachmentBytes: pdfBytes,
            attachmentName: $"{booking.BookingReference}.pdf",
            isHtml: true,
            cancellationToken: cancellationToken);

        message.Status = OutboxMessageStatus.Processed;
        message.ProcessedAt = DateTime.UtcNow;
        message.ErrorMessage = null;

        _logger.LogInformation(
            "Confirmation email with ticket PDF sent for booking {Reference} (outbox {MessageId})",
            booking.BookingReference, message.Id);
    }

    private static void MarkFailed(OutboxMessage message, string reason)
    {
        // Not retryable — bad data will never succeed on a later tick.
        message.Status = OutboxMessageStatus.Failed;
        message.ErrorMessage = reason;
        message.ProcessedAt = DateTime.UtcNow;
    }

    private static string BuildBookingConfirmationEmail(Booking booking)
    {
        var seats = string.Join(", ",
            booking.BookingSeats.Select(bs => bs.Seat.SeatCode));

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">

                <div style="background: #6c63ff; padding: 24px; border-radius: 8px; text-align: center;">
                    <h1 style="color: white; margin: 0;">🎵 SmartTix</h1>
                    <p style="color: #e0e0ff; margin: 8px 0 0;">Your booking is confirmed!</p>
                </div>

                <div style="padding: 24px 0;">
                    <h2 style="color: #333;">Booking Confirmation</h2>

                    <div style="background: #f5f5f5; padding: 16px; border-radius: 8px; margin: 16px 0;">
                        <p style="margin: 4px 0;"><strong>Reference:</strong> {booking.BookingReference}</p>
                        <p style="margin: 4px 0;"><strong>Event:</strong> {booking.Event.Name}</p>
                        <p style="margin: 4px 0;"><strong>Artist:</strong> {booking.Event.ArtistName}</p>
                        <p style="margin: 4px 0;"><strong>Venue:</strong> {booking.Event.VenueName}, {booking.Event.VenueCity}</p>
                        <p style="margin: 4px 0;"><strong>Date:</strong> {booking.Event.EventDate:dddd, MMMM dd yyyy 'at' hh:mm tt} UTC</p>
                        <p style="margin: 4px 0;"><strong>Seats:</strong> {seats}</p>
                        <p style="margin: 4px 0;"><strong>Total Paid:</strong> ₹{booking.TotalAmount:N0}</p>
                    </div>

                    <p style="color: #666;">
                        Your PDF ticket is attached. Please bring it to the venue —
                        either printed or on your phone.
                    </p>

                    <div style="background: #fff3cd; padding: 12px; border-radius: 8px; border-left: 4px solid #ffc107;">
                        <p style="margin: 0; color: #856404;">
                            <strong>Important:</strong> Doors open at {booking.Event.DoorsOpenTime:hh:mm tt} UTC.
                            Please arrive 30 minutes early.
                        </p>
                    </div>
                </div>

                <div style="border-top: 1px solid #eee; padding-top: 16px; color: #999; font-size: 12px;">
                    <p>SmartTix — AI-Powered Concert Ticketing</p>
                    <p>This is an automated email. Please do not reply.</p>
                </div>

            </body>
            </html>
            """;
    }
}

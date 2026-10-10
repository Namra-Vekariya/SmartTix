namespace SmartTix.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
        
    Task SendEmailWithAttachmentAsync(
        string recipientEmail,
        string subject,
        string body,
        byte[] attachmentBytes,
        string attachmentName,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}
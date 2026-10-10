using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Services;

public interface ITicketPdfService
{
    byte[] GenerateTicketPdf(Booking booking);
}

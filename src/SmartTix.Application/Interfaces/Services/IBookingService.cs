using SmartTix.Application.DTOs.Bookings;

namespace SmartTix.Application.Interfaces.Services;

public interface IBookingService
{
    Task<HoldSeatsResponse> HoldSeatsAsync(HoldSeatsRequest request, Guid userId);
    Task<BookingDetailDto> ConfirmBookingAsync(ConfirmBookingRequest request, Guid userId);
    Task<BookingDetailDto> GetBookingAsync(Guid bookingId, Guid userId);
    Task<byte[]> GetTicketPdfAsync(Guid bookingId, Guid userId);
    Task<List<BookingDetailDto>> GetUserBookingsAsync(Guid userId);
    Task CancelBookingAsync(Guid bookingId, Guid userId);
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Bookings;
using SmartTix.Application.Interfaces.Services;
using System.Security.Claims;

namespace SmartTix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("hold")]
    public async Task<IActionResult> HoldSeats([FromBody] HoldSeatsRequest request)
    {
        var userId = GetUserIdFromClaims();
        var result = await _bookingService.HoldSeatsAsync(request, userId);
        return Ok(ApiResponse<HoldSeatsResponse>.Ok(result, "Seats held successfully"));
    }

    [HttpGet("hold/{holdId:guid}")]
    public async Task<IActionResult> GetHold(Guid holdId)
    {
        var userId = GetUserIdFromClaims();
        var result = await _bookingService.GetHoldDetailsAsync(holdId, userId);
        return Ok(ApiResponse<HoldDetailsDto>.Ok(result, "Hold retrieved successfully"));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request)
    {
        var userId = GetUserIdFromClaims();
        var result = await _bookingService.ConfirmBookingAsync(request, userId);
        return Ok(ApiResponse<BookingDetailDto>.Ok(result, "Booking confirmed successfully"));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetUserIdFromClaims();
        var result = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(ApiResponse<List<BookingDetailDto>>.Ok(result, "Bookings retrieved successfully"));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var userId = GetUserIdFromClaims();
        var result = await _bookingService.GetBookingAsync(id, userId);
        return Ok(ApiResponse<BookingDetailDto>.Ok(result, "Booking retrieved successfully"));
    }

    [HttpGet("{id:guid}/ticket")]
    public async Task<IActionResult> DownloadTicket(Guid id)
    {
        var userId = GetUserIdFromClaims();
        var pdfBytes = await _bookingService.GetTicketPdfAsync(id, userId);
        return File(pdfBytes, "application/pdf", $"ticket-{id}.pdf");
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userId = GetUserIdFromClaims();
        await _bookingService.CancelBookingAsync(id, userId);
        return Ok(ApiResponse.OkNoData("Booking cancelled successfully"));
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user identity");

        return userId;
    }
}
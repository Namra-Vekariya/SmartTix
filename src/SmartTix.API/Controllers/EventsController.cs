using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Events;
using SmartTix.Application.Interfaces.Services;
using System.Security.Claims;

namespace SmartTix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // ── Public endpoints — no auth required ─────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] EventQueryParams queryParams)
    {
        var result = await _eventService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.Ok(result, "Events retrieved successfully"));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _eventService.GetByIdAsync(id);
        return Ok(ApiResponse<object>.Ok(result, "Event retrieved successfully"));
    }

    [HttpGet("{id:guid}/seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeatMap(Guid id)
    {
        var result = await _eventService.GetSeatMapAsync(id);
        return Ok(ApiResponse<object>.Ok(result, "Seat map retrieved successfully"));
    }

    // ── Admin only endpoints ─────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        // Extract admin user ID from JWT claims
        var adminUserId = GetUserIdFromClaims();

        var result = await _eventService.CreateAsync(request, adminUserId);

        // 201 Created with location header pointing to the new resource
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<object>.Ok(result, "Event created successfully"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = await _eventService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.Ok(result, "Event updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return Ok(ApiResponse.OkNoData("Event cancelled successfully"));
    }

    // ── Private helper ───────────────────────────────────────

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user identity");

        return userId;
    }
}
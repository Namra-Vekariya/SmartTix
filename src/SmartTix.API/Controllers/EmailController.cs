using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTix.Application.Common;
using SmartTix.Application.Interfaces.Services;

namespace SmartTix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestEmail(
        [FromQuery] [EmailAddress] string email,
        CancellationToken cancellationToken)
    {
        await _emailService.SendEmailAsync(
            email,
            "SmartTix Email Test",
            """
            <h2>SmartTix Email Test</h2>
            <p>If you received this email, SMTP configuration is working correctly.</p>
            """,
            isHtml: true,
            cancellationToken);

        return Ok(ApiResponse.OkNoData("Test email sent successfully"));
    }
}

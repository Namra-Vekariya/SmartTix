using SmartTix.Application.DTOs.Admin;

namespace SmartTix.Application.Interfaces.Services;

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
}

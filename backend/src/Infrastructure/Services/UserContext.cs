using Boekhouding.Application.Interfaces;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// UserContext implementatie die ICurrentUserService wraps
/// </summary>
public class UserContext : IUserContext
{
    private readonly ICurrentUserService _currentUserService;

    public UserContext(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Guid? UserId => _currentUserService.GetUserId();
    public string? UserEmail => _currentUserService.GetUserEmail();
}

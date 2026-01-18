using Boekhouding.Domain.Common;
using Boekhouding.Domain.Enums;

namespace Boekhouding.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Viewer;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}

namespace Boekhouding.Api.Authorization;

public static class Policies
{
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireAccountantRole = "RequireAccountantRole";
    public const string RequireViewerRole = "RequireViewerRole";
    public const string RequireAccountantOrAdmin = "RequireAccountantOrAdmin";
    public const string RequireAdminOrOwner = "RequireAdminOrOwner"; // Alias for RequireAccountantOrAdmin
}

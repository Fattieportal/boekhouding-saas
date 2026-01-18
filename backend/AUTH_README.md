# JWT Authentication & Authorization Implementation

## Overzicht

Dit project implementeert een complete JWT-gebaseerde authenticatie en autorisatie oplossing voor de Boekhouding SaaS applicatie.

## Architectuur

### Domain Layer
- **User Entity** (`Domain/Entities/User.cs`): Gebruiker entiteit met email, password hash, role, en status
- **Role Enum** (`Domain/Enums/Role.cs`): Drie rollen: Admin, Accountant, Viewer

### Application Layer
- **DTOs** (`Application/DTOs/Auth/`):
  - `RegisterRequest`: Email, Password, Role
  - `LoginRequest`: Email, Password
  - `AuthResponse`: Token, Email, Role, ExpiresAt
- **Interfaces**:
  - `IAuthService`: Authenticatie logica (register, login, password hashing)
  - `ITokenService`: JWT token generatie

### Infrastructure Layer
- **Services** (`Infrastructure/Services/`):
  - `AuthService`: Implementatie van authenticatie logica met BCrypt password hashing
  - `TokenService`: JWT token generatie met configureerbare instellingen
- **Data**:
  - `UserConfiguration`: EF Core configuratie voor User entity
  - `DbSeeder`: Automatische seeding van test gebruikers in development

### API Layer
- **Controllers**:
  - `AuthController`: Register, Login, en /me endpoints
- **Authorization**:
  - `Policies`: Policy-based authorization helpers
  - `Roles`: Role constants
- **Configuration**:
  - JWT Authentication middleware
  - Swagger met JWT support
  - CORS configuratie

## Implementatie Details

### Password Security
- **BCrypt.Net-Next**: State-of-the-art password hashing
- Automatische salt generatie
- Configureerbare work factor

### JWT Configuration
```json
{
  "Jwt": {
    "Key": "SuperSecretKeyForDevelopmentPurposesOnly123456789",
    "Issuer": "Boekhouding.Api",
    "Audience": "Boekhouding.Client",
    "ExpiryHours": "24"
  }
}
```

### Token Claims
- `sub`: User ID (GUID)
- `email`: User email address
- `role`: User role (Admin, Accountant, Viewer)
- `jti`: Unique JWT ID

### Authorization Policies

| Policy | Toegestane Rollen |
|--------|------------------|
| RequireAdminRole | Admin |
| RequireAccountantRole | Accountant |
| RequireViewerRole | Viewer, Accountant, Admin |
| RequireAccountantOrAdmin | Accountant, Admin |

## Database Schema

### Users Table
```sql
CREATE TABLE "Users" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Email" varchar(255) NOT NULL UNIQUE,
    "PasswordHash" varchar(500) NOT NULL,
    "Role" varchar(50) NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "IX_Users_Email" UNIQUE ("Email")
);
```

## Test Gebruikers (Development)

Automatisch aangemaakt bij eerste run:
- **admin@local.test** / Admin123! (Admin role)
- **accountant@local.test** / Accountant123! (Accountant role)
- **viewer@local.test** / Viewer123! (Viewer role)

## API Endpoints

### Authentication
- `POST /api/auth/register` - Registreer nieuwe gebruiker
- `POST /api/auth/login` - Login en ontvang JWT token
- `GET /api/auth/me` - Huidige gebruiker info (authenticated)

### Health Check
- `GET /health` - API health status (public)

## Swagger Integration

Swagger UI is beschikbaar op `http://localhost:5001/swagger` met:
- JWT Authorization support
- Interactieve API testing
- Request/Response voorbeelden
- XML documentatie comments

## Dependencies

### NuGet Packages
- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0)
- `System.IdentityModel.Tokens.Jwt` (8.0.0)
- `BCrypt.Net-Next` (4.0.3)
- `Microsoft.EntityFrameworkCore` (8.0.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.0)

## Security Best Practices

✅ **Geïmplementeerd:**
- BCrypt password hashing met salt
- JWT tokens met expiratie
- HTTPS enforcement (configurable)
- Role-based authorization
- Policy-based authorization
- Unique email constraint
- Active user check

⚠️ **Voor Productie:**
- Verplaats JWT key naar Azure Key Vault / AWS Secrets Manager
- Implementeer refresh tokens
- Voeg rate limiting toe
- Implementeer account lockout na failed logins
- Voeg email verificatie toe
- Implementeer password reset flow
- Voeg audit logging toe
- Configureer token revocation

## Gebruik in Controllers

```csharp
// Public endpoint - no authentication
[AllowAnonymous]
[HttpGet("public")]
public IActionResult Public() => Ok("Public data");

// Authenticated - any role
[Authorize]
[HttpGet("authenticated")]
public IActionResult Authenticated() => Ok("Authenticated data");

// Role-based - specific role
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public IActionResult Delete(Guid id) => Ok("Deleted");

// Policy-based - multiple roles
[Authorize(Policy = Policies.RequireAccountantOrAdmin)]
[HttpPost]
public IActionResult Create() => Ok("Created");

// Get current user info
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
```

## Migraties

### Huidige migraties:
1. `20260117155725_InitialCreate` - Klanten, Facturen, FactuurRegels
2. `20260117162514_AddUserAuthentication` - Users tabel

### Nieuwe migratie aanmaken:
```powershell
cd backend/src/Api
dotnet ef migrations add MigrationName --project ..\Infrastructure --startup-project .
```

### Database updaten:
```powershell
cd backend/src/Api
dotnet ef database update --project ..\Infrastructure --startup-project .
```

## Testing

Zie [AUTH_TESTING.md](./AUTH_TESTING.md) voor complete test instructies met cURL voorbeelden.

## Troubleshooting

### 401 Unauthorized
- Check of token correct is meegegeven: `Authorization: Bearer {token}`
- Controleer of token niet expired is
- Valideer JWT configuratie (Key, Issuer, Audience)

### 403 Forbidden
- Gebruiker is authenticated maar heeft niet de juiste role
- Check policy requirements
- Controleer User.Role claim

### Token niet geaccepteerd
- Controleer ClockSkew configuratie
- Valideer token signing key
- Check token expiration time

## Roadmap

- [ ] Refresh token implementatie
- [ ] Email verificatie
- [ ] Password reset flow
- [ ] Two-factor authentication (2FA)
- [ ] Account lockout policy
- [ ] Audit logging
- [ ] Rate limiting
- [ ] Token revocation list
- [ ] OAuth2 providers (Google, Microsoft)

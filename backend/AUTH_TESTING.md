# Authentication & Authorization Test Instructies

## Overzicht
De API heeft nu volledige JWT-gebaseerde authenticatie en autorisatie geïmplementeerd.

## Test Gebruikers

De volgende test gebruikers zijn automatisch aangemaakt bij development:

| Email                    | Password         | Role       |
|--------------------------|------------------|------------|
| admin@local.test         | Admin123!        | Admin      |
| accountant@local.test    | Accountant123!   | Accountant |
| viewer@local.test        | Viewer123!       | Viewer     |

## Endpoints

### 1. Register (POST /api/auth/register)

Registreer een nieuwe gebruiker.

**cURL Voorbeeld:**
```powershell
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/register" -Method POST -ContentType "application/json" -Body '{"email":"test@example.com","password":"Test123!","role":"Viewer"}' | Select-Object -ExpandProperty Content
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "test@example.com",
  "role": "Viewer",
  "expiresAt": "2026-01-18T16:30:00Z"
}
```

### 2. Login (POST /api/auth/login)

Login met bestaande gebruiker.

**cURL Voorbeeld (Admin):**
```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}' | ConvertFrom-Json
$token = $response.token
Write-Host "Token: $token"
```

**cURL Voorbeeld (Accountant):**
```powershell
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"accountant@local.test","password":"Accountant123!"}' | Select-Object -ExpandProperty Content
```

**cURL Voorbeeld (Viewer):**
```powershell
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"viewer@local.test","password":"Viewer123!"}' | Select-Object -ExpandProperty Content
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@local.test",
  "role": "Admin",
  "expiresAt": "2026-01-18T16:30:00Z"
}
```

### 3. Get Current User (GET /api/auth/me)

Test authenticated endpoint - geeft huidige gebruiker info terug.

**cURL Voorbeeld:**
```powershell
# Eerst inloggen en token opslaan
$response = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}' | ConvertFrom-Json
$token = $response.token

# Gebruik token om /me endpoint te testen
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -Headers @{Authorization="Bearer $token"} | Select-Object -ExpandProperty Content
```

**Response:**
```json
{
  "userId": "uuid-here",
  "email": "admin@local.test",
  "role": "Admin",
  "claims": [
    {"type": "sub", "value": "uuid-here"},
    {"type": "email", "value": "admin@local.test"},
    {"type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "value": "Admin"},
    {"type": "jti", "value": "uuid-here"}
  ]
}
```

## Authorization Policies

De volgende policies zijn beschikbaar voor gebruik met `[Authorize(Policy = "...")]`:

- **RequireAdminRole**: Alleen Admin
- **RequireAccountantRole**: Alleen Accountant
- **RequireViewerRole**: Viewer, Accountant, of Admin
- **RequireAccountantOrAdmin**: Accountant of Admin

### Voorbeeld gebruik in Controller:

```csharp
[HttpGet]
[Authorize(Policy = Policies.RequireAdminRole)]
public IActionResult AdminOnlyEndpoint()
{
    return Ok("Only admins can see this");
}

[HttpPost]
[Authorize(Policy = Policies.RequireAccountantOrAdmin)]
public IActionResult CreateInvoice()
{
    return Ok("Accountants and Admins can create invoices");
}

[HttpGet]
[Authorize(Policy = Policies.RequireViewerRole)]
public IActionResult ViewData()
{
    return Ok("All authenticated users can view this");
}
```

## Swagger UI Testing

1. Open browser: http://localhost:5001/swagger
2. Klik op "Authorize" knop (rechts bovenin)
3. Login om token te krijgen:
   - POST /api/auth/login
   - Use body: `{"email":"admin@local.test","password":"Admin123!"}`
   - Kopieer de `token` waarde uit de response
4. Plak token in "Authorize" dialog: `Bearer {jouw-token-hier}`
5. Klik "Authorize"
6. Test nu protected endpoints zoals GET /api/auth/me

## Complete Test Flow

```powershell
# 1. Test Health Check (no auth required)
Invoke-WebRequest -Uri "http://localhost:5001/health" | Select-Object -ExpandProperty Content

# 2. Login als Admin
$adminResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}' | ConvertFrom-Json
$adminToken = $adminResponse.token
Write-Host "Admin Token: $adminToken"

# 3. Test authenticated endpoint
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -Headers @{Authorization="Bearer $adminToken"} | Select-Object -ExpandProperty Content

# 4. Register nieuwe gebruiker
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/register" -Method POST -ContentType "application/json" -Body '{"email":"newuser@example.com","password":"NewUser123!","role":"Viewer"}' | Select-Object -ExpandProperty Content

# 5. Login met nieuwe gebruiker
$newUserResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"newuser@example.com","password":"NewUser123!"}' | ConvertFrom-Json
$newUserToken = $newUserResponse.token

# 6. Test met nieuwe gebruiker
Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -Headers @{Authorization="Bearer $newUserToken"} | Select-Object -ExpandProperty Content

# 7. Test invalid login (should fail)
try {
    Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"WrongPassword"}' -ErrorAction Stop
} catch {
    Write-Host "Expected 401 Unauthorized: $_"
}

# 8. Test zonder token (should fail)
try {
    Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -ErrorAction Stop
} catch {
    Write-Host "Expected 401 Unauthorized: $_"
}
```

## JWT Token Details

- **Signing Key**: Configureerbaar via appsettings.json (`Jwt:Key`)
- **Issuer**: Boekhouding.Api
- **Audience**: Boekhouding.Client
- **Expiry**: 24 uur (configureerbaar via `Jwt:ExpiryHours`)
- **Claims**: 
  - `sub`: User ID (GUID)
  - `email`: User email
  - `role`: User role (Admin, Accountant, Viewer)
  - `jti`: JWT ID (voor token revocation in de toekomst)

## Security Notes

⚠️ **Voor Development Only:**
- De JWT key in appsettings is voor development
- In productie: gebruik secrets manager of environment variables
- Passwords worden veilig gehashed met BCrypt
- HTTPS is verplicht in productie (configureerbaar)

## Volgende Stappen

Om de KlantenController te beveiligen:

```csharp
[Authorize(Policy = Policies.RequireViewerRole)] // Iedereen kan lezen
public class KlantenController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() { }
    
    [Authorize(Policy = Policies.RequireAccountantOrAdmin)] // Alleen Accountant/Admin
    [HttpPost]
    public async Task<IActionResult> Create() { }
    
    [Authorize(Policy = Policies.RequireAccountantOrAdmin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update() { }
    
    [Authorize(Policy = Policies.RequireAdminRole)] // Alleen Admin
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete() { }
}
```

# Multi-Tenant Support

Deze applicatie ondersteunt vanaf dag 1 multi-tenancy met harde isolatie tussen tenants.

## Architectuur

### Entities

- **Tenant**: Bevat tenant informatie (Id, Name, KvK, VatNumber, CreatedAt)
- **UserTenant**: Join table tussen User en Tenant met rol per tenant (Admin/Accountant/Viewer)
- **Alle business entities**: Bevatten TenantId voor isolatie (Klant, Factuur, FactuurRegel)

### Tenant Context

Elke request moet een `X-Tenant-Id` header bevatten met een geldige GUID. De `TenantMiddleware` valideert:

1. Of de header aanwezig is en een geldige GUID bevat
2. Of de gebruiker geauthenticeerd is
3. Of de gebruiker toegang heeft tot de opgegeven tenant (via UserTenant tabel)
4. Set de TenantId in de `ITenantContext` service

### Query Filters

EF Core global query filters zorgen ervoor dat alle queries automatisch gefilterd worden op TenantId:

```csharp
modelBuilder.Entity<Klant>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<Factuur>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<FactuurRegel>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
```

Bij het opslaan van nieuwe entities wordt de TenantId automatisch gezet vanuit de context.

## API Endpoints

### Tenant Management

#### GET /api/tenants/my
Haal alle tenants op waar de ingelogde gebruiker toegang tot heeft.

**Headers:**
```
Authorization: Bearer {jwt-token}
```

**Response:**
```json
[
  {
    "id": "guid",
    "name": "Acme Corporation",
    "role": "Admin"
  }
]
```

#### POST /api/tenants
Maak een nieuwe tenant aan. De huidige gebruiker wordt automatisch als Admin gekoppeld.

**Headers:**
```
Authorization: Bearer {jwt-token}
```

**Request Body:**
```json
{
  "name": "Acme Corporation",
  "kvK": "12345678",
  "vatNumber": "NL123456789B01"
}
```

**Response:**
```json
{
  "id": "guid",
  "name": "Acme Corporation",
  "kvK": "12345678",
  "vatNumber": "NL123456789B01",
  "createdAt": "2026-01-17T10:00:00Z",
  "role": "Admin"
}
```

#### GET /api/tenants/{id}
Haal details op van een specifieke tenant.

**Headers:**
```
Authorization: Bearer {jwt-token}
X-Tenant-Id: {tenant-guid}
```

### Alle andere endpoints

Alle andere endpoints (klanten, facturen, etc.) vereisen de `X-Tenant-Id` header.

**Headers:**
```
Authorization: Bearer {jwt-token}
X-Tenant-Id: {tenant-guid}
```

## Gebruik

### 1. Registreer en login

```powershell
# Registreer
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "role": "Admin"
}

# Login
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

### 2. Maak een tenant aan

```powershell
POST /api/tenants
Authorization: Bearer {token}
{
  "name": "My Company",
  "kvK": "12345678",
  "vatNumber": "NL123456789B01"
}
```

### 3. Gebruik de tenant ID voor alle requests

```powershell
GET /api/klanten
Authorization: Bearer {token}
X-Tenant-Id: {tenant-id}
```

## Test Script

Run het test script om de multi-tenant functionaliteit te testen:

```powershell
cd backend
.\test-multitenant.ps1
```

Dit script:
1. Registreert een gebruiker
2. Logt in en krijgt een JWT token
3. Maakt een tenant aan
4. Haalt alle tenants van de gebruiker op
5. Probeert een klant aan te maken (vereist aangepaste KlantenController)
6. Test validatie zonder X-Tenant-Id header
7. Test validatie met ongeldige tenant ID

## Migratie

De database migratie `AddMultiTenantSupport` voegt de volgende tabellen en kolommen toe:

- Tabel `Tenants`
- Tabel `UserTenants` (join table)
- Kolom `TenantId` aan `Klanten`, `Facturen`, `FactuurRegels`
- Indexes en foreign keys

Run de migratie:

```powershell
cd backend/src/Api
dotnet ef database update --project ..\Infrastructure --startup-project .
```

## Beveiliging

- **Hard Isolation**: Query filters zorgen ervoor dat data van andere tenants NOOIT zichtbaar is
- **Access Control**: Middleware valideert dat gebruiker toegang heeft tot tenant
- **Automatische TenantId**: Bij opslaan wordt TenantId automatisch gezet, kan niet overschreven worden
- **Header Validatie**: X-Tenant-Id moet een geldige GUID zijn
- **Authentication Required**: Alle tenant-scoped requests vereisen authenticatie

## Uitbreidingen voor de Toekomst

### Controllers aanpassen

Alle bestaande controllers (KlantenController, etc.) werken automatisch met multi-tenancy door:
1. De global query filters
2. De automatische TenantId toewijzing bij SaveChanges

Je hoeft alleen de `X-Tenant-Id` header toe te voegen aan je requests.

### Extra functies

- **Tenant Settings**: Per-tenant configuratie (thema, logo, etc.)
- **Tenant Gebruikers Beheer**: Endpoints om gebruikers uit te nodigen voor een tenant
- **Tenant Rol Management**: Gebruiker rollen binnen tenant wijzigen
- **Tenant Statistieken**: Dashboard per tenant
- **Tenant Export**: Data export per tenant voor compliance

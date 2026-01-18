# Audit Log & Security Hardening Implementation

## Overzicht

Deze implementatie voegt uitgebreide audit logging en security hardening toe aan het boekhouding SaaS systeem.

## 1. Audit Log Functionaliteit

### Database Schema

De `AuditLogs` tabel bevat de volgende velden:
- **TenantId**: Tenant waarvoor de actie werd uitgevoerd
- **ActorUserId**: Gebruiker die de actie uitvoerde
- **Action**: Type actie (Create, Update, Delete, Post, Reverse, Pay, Sync, Match)
- **EntityType**: Type entiteit (Tenant, InvoiceTemplate, TenantBranding, SalesInvoice, JournalEntry, BankConnection, BankTransaction)
- **EntityId**: ID van de betreffende entiteit
- **Timestamp**: UTC timestamp van de actie
- **DiffJson**: JSON met veranderingen (before/after state)
- **IpAddress**: IP adres van de gebruiker
- **UserAgent**: Browser/client user agent

### Gelogde Acties

#### Verplicht te loggen:
1. **Tenant Management**
   - Tenant aanmaken
   - Tenant wijzigen
   - Tenant verwijderen

2. **Template & Branding**
   - InvoiceTemplate create/update/delete
   - TenantBranding create/update/delete

3. **Sales Invoices**
   - Invoice post (van Draft naar Posted)
   - Invoice payment registratie
   - Invoice storno

4. **Journal Entries**
   - JournalEntry post
   - JournalEntry reverse

5. **Bank Integration**
   - Bank connection aanmaken/verwijderen
   - Bank sync acties
   - Transaction matching

### Service Interface

```csharp
public interface IAuditLogService
{
    Task LogAsync(
        Guid tenantId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        object? diff = null,
        string? ipAddress = null,
        string? userAgent = null);
    
    Task<IEnumerable<AuditLog>> GetLogsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? entityType = null,
        Guid? entityId = null,
        int skip = 0,
        int take = 100);
    
    Task<IEnumerable<AuditLog>> GetEntityLogsAsync(
        Guid tenantId,
        string entityType,
        Guid entityId);
}
```

### Gebruik in Services

Voorbeeld van audit logging bij invoice posting:

```csharp
// In SalesInvoiceService.PostInvoiceAsync
var oldState = new { invoice.Status, invoice.PostedAt };
invoice.Status = InvoiceStatus.Posted;
invoice.PostedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();

await _auditLogService.LogAsync(
    tenantId: invoice.TenantId,
    actorUserId: userId,
    action: "Post",
    entityType: "SalesInvoice",
    entityId: invoice.Id,
    diff: new { 
        Before = oldState, 
        After = new { invoice.Status, invoice.PostedAt }
    },
    ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
    userAgent: httpContext.Request.Headers["User-Agent"].ToString()
);
```

## 2. Security Hardening

### Rate Limiting

**RateLimitingMiddleware** implementeert rate limiting op auth endpoints:

- **Endpoints**: `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh`
- **Limiet**: 5 requests per minuut per IP address
- **Response**: HTTP 429 Too Many Requests

**Features:**
- Per-IP tracking met X-Forwarded-For support voor reverse proxies
- Sliding window algorithm
- Automatische cleanup van oude entries
- Configureerbare limieten

### CORS Hardening

Strikte CORS configuratie in `Program.cs`:

```csharp
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "https://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithExposedHeaders("X-Tenant-Id")
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});
```

**Configuratie in appsettings.json:**

```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://localhost:3000",
    "https://app.yourdomain.com"
  ]
}
```

### Security Validation Middleware

**SecurityValidationMiddleware** voert de volgende validaties uit:

1. **Origin Validatie**
   - Controleert Origin header tegen whitelist
   - Blokkeert requests van onbekende origins
   - Exempt: health check en swagger endpoints

2. **Tenant Isolation**
   - Valideert dat X-Tenant-Id header aanwezig is voor authenticated requests
   - Controleert dat header overeenkomt met TenantId claim in JWT
   - Voorkomt cross-tenant data access

3. **Content-Type Validatie**
   - Vereist `application/json` of `multipart/form-data` voor POST/PUT
   - Voorkomt onverwachte content types

**Tenant Isolation Flow:**
```
1. User authenticates → JWT issued with TenantId claim
2. Request includes X-Tenant-Id header
3. SecurityValidationMiddleware validates:
   - Header present?
   - Header matches JWT claim?
   - If not → HTTP 403 Forbidden
4. TenantMiddleware sets context
5. Global query filters enforce tenant isolation
```

### Middleware Volgorde

Kritieke volgorde in de request pipeline:

```csharp
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Security middleware BEFORE authentication
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<SecurityValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Tenant middleware AFTER authentication/authorization
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
```

## 3. API Endpoints

### Audit Logs ophalen

```
GET /api/auditlogs?startDate=2026-01-01&endDate=2026-01-31&entityType=SalesInvoice&skip=0&take=100
Authorization: Bearer {token}
X-Tenant-Id: {tenantId}
```

**Response:**
```json
[
  {
    "id": "...",
    "timestamp": "2026-01-17T10:30:00Z",
    "action": "Post",
    "entityType": "SalesInvoice",
    "entityId": "...",
    "actor": {
      "actorUserId": "...",
      "email": "user@example.com"
    },
    "diffJson": "{\"before\":{\"status\":\"Draft\"},\"after\":{\"status\":\"Posted\"}}",
    "ipAddress": "192.168.1.100"
  }
]
```

### Entity-specifieke audit logs

```
GET /api/auditlogs/entity/SalesInvoice/{invoiceId}
Authorization: Bearer {token}
X-Tenant-Id: {tenantId}
```

## 4. Installatie

### Stap 1: Database Migratie

```powershell
# Voer migratie uit
cat backend/migrations/001_Add_AuditLog_Table.sql | docker exec -i boekhouding-postgres psql -U postgres -d boekhouding

# Verificeer
echo "SELECT COUNT(*) FROM \"AuditLogs\";" | docker exec -i boekhouding-postgres psql -U postgres -d boekhouding
```

### Stap 2: Build het project

```powershell
cd backend
dotnet build
```

### Stap 3: Configureer appsettings.json

Voeg toe aan `appsettings.json`:

```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://localhost:3000"
  ]
}
```

### Stap 4: Start de applicatie

```powershell
dotnet run --project src/Api
```

## 5. Testing

Zie `AUDIT_SECURITY_TEST_CHECKLIST.md` voor gedetailleerde test scenarios.

## 6. Performance Overwegingen

### Database Indices

De AuditLogs tabel heeft indices op:
- `TenantId` - voor tenant filtering
- `(TenantId, Timestamp)` - voor time-based queries
- `(TenantId, EntityType, EntityId)` - voor entity-specific queries
- `ActorUserId` - voor user activity tracking

### JSONB Column

`DiffJson` gebruikt PostgreSQL JSONB voor:
- Efficiënte opslag
- Query capabilities (indien nodig)
- Automatische validatie

### Rate Limiting Cleanup

De RateLimitingMiddleware houdt een in-memory cache bij. Voor production:
- Implementeer periodieke cleanup (background service)
- Overweeg distributed cache (Redis) voor multi-instance deployments

## 7. Best Practices

### Audit Logging

1. **Wat te loggen:**
   - State-changing operations (Create, Update, Delete, Post, Reverse)
   - Security-sensitive acties (Login, Permission changes)
   - Business-critical events (Payments, Bank syncs)

2. **Wat NIET te loggen:**
   - Read operations (te veel volume)
   - Gevoelige data (passwords, tokens)
   - Health checks

3. **DiffJson Format:**
   ```json
   {
     "before": { "field": "oldValue" },
     "after": { "field": "newValue" }
   }
   ```

### Security

1. **Rate Limiting:**
   - Pas aan per endpoint type
   - Monitor voor DDoS patterns
   - Whitelist trusted IPs indien nodig

2. **CORS:**
   - Gebruik exacte origins in production
   - Geen wildcards (*) in production
   - Valideer subdomain patterns

3. **Tenant Isolation:**
   - Altijd X-Tenant-Id header vereisen
   - Nooit trust client-side tenant selection
   - Log tenant mismatch attempts

## 8. Monitoring & Alerts

Stel alerts in voor:
- Hoog aantal rate limit violations (mogelijk attack)
- Tenant isolation violations (security breach attempt)
- Failed audit log writes (data integrity)
- Ongebruikelijke audit patterns (bulk deletes, off-hours activity)

## 9. Compliance

Deze implementatie ondersteunt:
- **GDPR**: Audit trail voor data access/changes
- **SOC 2**: Change tracking en access logs
- **NEN-ISO 27001**: Security controls en monitoring

## 10. Troubleshooting

### Rate Limit False Positives

Als legitieme users rate limited worden:
1. Check X-Forwarded-For header configuratie
2. Verhoog limieten voor specifieke endpoints
3. Implementeer user-based rate limiting (ipv IP-based)

### Audit Log Volume

Als AuditLogs tabel te groot wordt:
1. Implementeer archivering (ouder dan 1 jaar)
2. Partition table per maand
3. Aggregate old logs voor reporting

### Performance Issues

Als audit logging performance impact heeft:
1. Gebruik async logging met queue
2. Batch insert meerdere logs
3. Overweeg separate audit database

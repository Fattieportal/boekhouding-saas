# Accounting Basis Implementation

## Overview

Deze implementatie voegt een dubbel boekhouden foundation toe aan de boekhouding SaaS applicatie met:
- **Accounts (Grootboekrekeningen)**: Voor het bijhouden van financiële transacties
- **Journals (Dagboeken)**: Voor het groeperen van boekingen

## Database Schema

### Account Entity
- `Id` (Guid) - Primary Key
- `TenantId` (Guid) - Foreign Key naar Tenant
- `Code` (string, max 20) - Unieke code per tenant (bijv. "1100", "8000")
- `Name` (string, max 200) - Naam van de rekening
- `Type` (AccountType enum) - Type rekening
- `IsActive` (bool) - Of de rekening actief is
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime?)

**AccountType Enum:**
- `Asset` (1) - Activa/bezittingen
- `Liability` (2) - Passiva/schulden
- `Equity` (3) - Eigen vermogen
- `Revenue` (4) - Opbrengsten/omzet
- `Expense` (5) - Kosten

**Unique Constraints:**
- Unique index op `(TenantId, Code)` - Account code moet uniek zijn per tenant

### Journal Entity
- `Id` (Guid) - Primary Key
- `TenantId` (Guid) - Foreign Key naar Tenant
- `Code` (string, max 20) - Unieke code per tenant (bijv. "VRK", "INK")
- `Name` (string, max 200) - Naam van het dagboek
- `Type` (JournalType enum) - Type dagboek
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime?)

**JournalType Enum:**
- `Sales` (1) - Verkoopdagboek
- `Purchase` (2) - Inkoopdagboek
- `Bank` (3) - Bankdagboek
- `General` (4) - Memoriaal/diversen

**Unique Constraints:**
- Unique index op `(TenantId, Code)` - Journal code moet uniek zijn per tenant

## Seed Data (NL Minimal Chart)

Voor elke tenant worden automatisch de volgende accounts aangemaakt:

| Code | Naam | Type |
|------|------|------|
| 0100 | Eigen vermogen | Equity |
| 1100 | Debiteuren | Asset |
| 1300 | Bank | Asset |
| 1600 | Crediteuren | Liability |
| 1700 | Te betalen BTW | Liability |
| 4000 | Kosten | Expense |
| 8000 | Omzet | Revenue |

Voor elke tenant worden automatisch de volgende journals aangemaakt:

| Code | Naam | Type |
|------|------|------|
| VRK | Verkopen | Sales |
| INK | Inkopen | Purchase |
| BANK | Bank | Bank |
| MEM | Memoriaal | General |

## API Endpoints

### Accounts

**GET /api/accounts**
- Haal alle accounts op met paginering en filtering
- Query parameters:
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20, max: 100)
  - `search` (string) - Zoek in code en naam
  - `type` (AccountType) - Filter op type
  - `isActive` (bool) - Filter op actieve status
- Requires: Authentication + valid Tenant-Id header
- Returns: Paginated list met accounts

**GET /api/accounts/{id}**
- Haal specifieke account op via ID
- Requires: Authentication + valid Tenant-Id header
- Returns: AccountDto

**GET /api/accounts/by-code/{code}**
- Haal account op via code
- Requires: Authentication + valid Tenant-Id header
- Returns: AccountDto

**POST /api/accounts**
- Maak nieuwe account aan
- Requires: Accountant role of hoger
- Body: CreateAccountDto
- Returns: 201 Created met AccountDto

**PUT /api/accounts/{id}**
- Werk bestaande account bij
- Requires: Accountant role of hoger
- Body: UpdateAccountDto
- Returns: AccountDto

**DELETE /api/accounts/{id}**
- Verwijder account
- Requires: Accountant role of hoger
- Returns: 204 No Content

### Journals

**GET /api/journals**
- Haal alle journals op met paginering en filtering
- Query parameters:
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20, max: 100)
  - `search` (string) - Zoek in code en naam
  - `type` (JournalType) - Filter op type
- Requires: Authentication + valid Tenant-Id header
- Returns: Paginated list met journals

**GET /api/journals/{id}**
- Haal specifieke journal op via ID
- Requires: Authentication + valid Tenant-Id header
- Returns: JournalDto

**GET /api/journals/by-code/{code}**
- Haal journal op via code
- Requires: Authentication + valid Tenant-Id header
- Returns: JournalDto

**POST /api/journals**
- Maak nieuwe journal aan
- Requires: Accountant role of hoger
- Body: CreateJournalDto
- Returns: 201 Created met JournalDto

**PUT /api/journals/{id}**
- Werk bestaande journal bij
- Requires: Accountant role of hoger
- Body: UpdateJournalDto
- Returns: JournalDto

**DELETE /api/journals/{id}**
- Verwijder journal
- Requires: Accountant role of hoger
- Returns: 204 No Content

## Voorbeeld Requests

### Login
```powershell
$loginBody = @{
    email = "accountant@local.test"
    password = "Accountant123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
    -Method Post -Body $loginBody -ContentType "application/json"

$token = $response.token
$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = "11111111-1111-1111-1111-111111111111"
}
```

### Accounts ophalen
```powershell
# Alle accounts
$accounts = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts" `
    -Method Get -Headers $headers

# Met paginering
$accounts = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts?page=1&pageSize=10" `
    -Method Get -Headers $headers

# Filter op type (Asset = 1)
$assets = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts?type=1" `
    -Method Get -Headers $headers

# Zoeken
$results = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts?search=Bank" `
    -Method Get -Headers $headers

# Via code
$account = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts/by-code/1100" `
    -Method Get -Headers $headers
```

### Account aanmaken
```powershell
$newAccount = @{
    code = "7000"
    name = "Kantoorkosten"
    type = 5  # Expense
    isActive = $true
} | ConvertTo-Json

$created = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts" `
    -Method Post -Body $newAccount -Headers $headers -ContentType "application/json"
```

### Account bijwerken
```powershell
$updateAccount = @{
    code = "7000"
    name = "Algemene kantoorkosten"
    type = 5
    isActive = $true
} | ConvertTo-Json

$updated = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts/$accountId" `
    -Method Put -Body $updateAccount -Headers $headers -ContentType "application/json"
```

### Journals ophalen
```powershell
# Alle journals
$journals = Invoke-RestMethod -Uri "http://localhost:5001/api/journals" `
    -Method Get -Headers $headers

# Filter op type (Sales = 1)
$sales = Invoke-RestMethod -Uri "http://localhost:5001/api/journals?type=1" `
    -Method Get -Headers $headers

# Via code
$journal = Invoke-RestMethod -Uri "http://localhost:5001/api/journals/by-code/VRK" `
    -Method Get -Headers $headers
```

### Journal aanmaken
```powershell
$newJournal = @{
    code = "KAS"
    name = "Kasdagboek"
    type = 3  # Bank
} | ConvertTo-Json

$created = Invoke-RestMethod -Uri "http://localhost:5001/api/journals" `
    -Method Post -Body $newJournal -Headers $headers -ContentType "application/json"
```

## Validatie & Business Rules

### Accounts
- Code is verplicht en max 20 karakters
- Naam is verplicht en max 200 karakters
- Type is verplicht en moet een geldige AccountType zijn
- Code moet uniek zijn binnen een tenant
- Bij het bijwerken wordt de uniqueness van de nieuwe code gecontroleerd

### Journals
- Code is verplicht en max 20 karakters
- Naam is verplicht en max 200 karakters
- Type is verplicht en moet een geldige JournalType zijn
- Code moet uniek zijn binnen een tenant
- Bij het bijwerken wordt de uniqueness van de nieuwe code gecontroleerd

## Multi-tenancy

Alle accounts en journals zijn volledig tenant-scoped:
- Global query filters zorgen ervoor dat alleen data van de huidige tenant wordt opgehaald
- TenantId wordt automatisch gezet bij het aanmaken
- Unique constraints zijn per tenant (TenantId + Code)
- API endpoints vereisen een geldige X-Tenant-Id header

## Beveiliging

- Alle endpoints vereisen authenticatie (Bearer token)
- GET endpoints zijn toegankelijk voor alle geauthenticeerde gebruikers
- POST, PUT, DELETE endpoints vereisen de "AccountantOrHigher" policy
  - Toegestaan voor: Admin, Accountant
  - Niet toegestaan voor: Viewer

## Database Migratie

De implementatie bevat een nieuwe migratie: `AddAccountsAndJournals`

**Migrations toepassen:**
```bash
cd backend/src/Api
dotnet ef database update --project ..\Infrastructure --startup-project .
```

**Nieuwe migratie maken:**
```bash
dotnet ef migrations add MigrationName --project ..\Infrastructure --startup-project .
```

## Testing

Start de API:
```bash
cd backend/src/Api
dotnet run
```

De API luistert op: http://localhost:5001

Gebruik de voorbeelden hierboven of de Swagger UI op: http://localhost:5001/swagger

## Project Structuur

```
backend/src/
├── Domain/
│   ├── Entities/
│   │   ├── Account.cs          # Account entity
│   │   └── Journal.cs          # Journal entity
│   └── Enums/
│       ├── AccountType.cs      # Account type enum
│       └── JournalType.cs      # Journal type enum
├── Application/
│   ├── DTOs/
│   │   ├── Accounts/
│   │   │   ├── AccountDto.cs
│   │   │   ├── CreateAccountDto.cs
│   │   │   └── UpdateAccountDto.cs
│   │   └── Journals/
│   │       ├── JournalDto.cs
│   │       ├── CreateJournalDto.cs
│   │       └── UpdateJournalDto.cs
│   └── Interfaces/
│       ├── IAccountService.cs  # Account service interface
│       └── IJournalService.cs  # Journal service interface
├── Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs  # Updated with DbSets
│   │   ├── DbSeeder.cs              # Updated with seed data
│   │   └── Configurations/
│   │       ├── AccountConfiguration.cs
│   │       └── JournalConfiguration.cs
│   └── Services/
│       ├── AccountService.cs   # Account service implementation
│       └── JournalService.cs   # Journal service implementation
└── Api/
    └── Controllers/
        ├── AccountsController.cs   # Accounts API
        └── JournalsController.cs   # Journals API
```

## Volgende Stappen

Deze implementatie legt de basis voor dubbel boekhouden. Volgende features kunnen zijn:

1. **Journal Entries (Boekingen)**
   - Boeking entity met debet/credit regels
   - Validatie dat debet = credit
   - Link naar accounts en journals

2. **Account Hierarchy**
   - Parent-child relaties tussen accounts
   - Consolidated balances

3. **Fiscal Years & Periods**
   - Boekjaren en periodes
   - Period locking

4. **Reporting**
   - Balance sheet (Balans)
   - Profit & Loss (Winst & Verlies)
   - Trial balance (Proef & saldibalans)

5. **Import/Export**
   - CSV import voor accounts
   - Standard chart of accounts templates

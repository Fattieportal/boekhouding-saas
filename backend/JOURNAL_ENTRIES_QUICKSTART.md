# Boekingsengine - Quick Start Guide

## Wat is geïmplementeerd?

Een complete **boekingsengine** (journal entries systeem) voor dubbel boekhouden met:

✅ **JournalEntry & JournalLine entiteiten**  
✅ **Balansvalidatie** - Sum(Debit) moet gelijk zijn aan Sum(Credit)  
✅ **Immutability** - Geboekte entries kunnen niet worden aangepast  
✅ **Reversal mechanisme** - Correcties via terugboekingen  
✅ **Complete REST API** met alle CRUD operaties  
✅ **Multi-tenancy support**  
✅ **Database migrations**  

## Snel aan de slag

### 1. Database updaten

```powershell
cd backend\src\Api
dotnet ef database update --project ..\Infrastructure --startup-project .
```

### 2. API starten

```powershell
cd backend\src\Api
dotnet run
```

De API draait nu op: http://localhost:5001

### 3. Testen

```powershell
cd backend
.\test-journal-entries-simple.ps1
```

## API Endpoints

Alle endpoints vereisen authenticatie en X-Tenant-Id header.

### Journal Entries

```
GET    /api/journal-entries              # Lijst ophalen (met filters)
GET    /api/journal-entries/{id}          # Specifieke entry
POST   /api/journal-entries               # Nieuwe draft entry maken
PUT    /api/journal-entries/{id}          # Draft entry updaten
DELETE /api/journal-entries/{id}          # Draft entry verwijderen
POST   /api/journal-entries/{id}/post     # Draft posten (met balance check)
POST   /api/journal-entries/{id}/reverse  # Posted entry terugdraaien
```

### Query Parameters voor GET /api/journal-entries

- `journalId` - Filter op dagboek
- `dateFrom` - Vanaf datum
- `dateTo` - Tot datum
- `status` - 0 (Draft), 1 (Posted), 2 (Reversed)
- `reference` - Zoek op referentie

## Voorbeeld: Een boeking maken

### Stap 1: Maak een draft entry

```http
POST /api/journal-entries
Authorization: Bearer {token}
X-Tenant-Id: {tenantId}
Content-Type: application/json

{
  "journalId": "uuid-van-journal",
  "entryDate": "2026-01-17",
  "reference": "FACT-001",
  "description": "Verkoop aan klant",
  "lines": [
    {
      "accountId": "uuid-debiteuren",
      "description": "Debiteur X",
      "debit": 121.00,
      "credit": 0
    },
    {
      "accountId": "uuid-omzet",
      "description": "Verkoop product",
      "debit": 0,
      "credit": 100.00
    },
    {
      "accountId": "uuid-btw",
      "description": "BTW 21%",
      "debit": 0,
      "credit": 21.00
    }
  ]
}
```

### Stap 2: Post de entry

```http
POST /api/journal-entries/{id}/post
Authorization: Bearer {token}
X-Tenant-Id: {tenantId}
```

Validatie gebeurt automatisch:
- ✅ Sum(Debit) = 121.00
- ✅ Sum(Credit) = 121.00
- ✅ Balans klopt → Entry wordt Posted

### Stap 3: Correctie via reversal (indien nodig)

```http
POST /api/journal-entries/{id}/reverse
Authorization: Bearer {token}
X-Tenant-Id: {tenantId}
```

Dit:
- Maakt een nieuwe entry met debit ↔ credit omgewisseld
- Zet de status van de originele entry op "Reversed"
- Nieuwe reversal entry is direct Posted

## Business Rules

### 1. Balance Validatie
- Bij het posten moet: **Sum(Debit) == Sum(Credit)**
- Anders wordt de posting geweigerd met foutmelding

### 2. Immutability
- **Draft** entries kunnen worden aangepast en verwijderd
- **Posted** entries zijn immutable (niet aanpassen/verwijderen)
- **Reversed** entries zijn ook immutable

### 3. Reversal
- Alleen Posted entries kunnen worden reversed
- Maakt nieuwe entry met omgekeerde debit/credit
- Orginele entry krijgt status Reversed
- Reversal kan niet worden omgedraaid (voorkomt loops)

### 4. Lijnvalidatie
- Debit en Credit moeten ≥ 0
- Een regel kan niet zowel Debit als Credit hebben (>0)
- Alle Accounts moeten bestaan

## Database Schema

### JournalEntries
- Id (PK)
- TenantId (FK → Tenants)
- JournalId (FK → Journals)
- EntryDate
- Reference
- Description
- Status (0=Draft, 1=Posted, 2=Reversed)
- PostedAt
- ReversalOfEntryId (FK → JournalEntries, nullable)
- CreatedAt, UpdatedAt

### JournalLines
- Id (PK)
- TenantId (FK → Tenants)
- EntryId (FK → JournalEntries, cascade delete)
- AccountId (FK → Accounts)
- Description
- Debit (decimal 18,2)
- Credit (decimal 18,2)
- CreatedAt, UpdatedAt

## Status Flow

```
Draft → Posted → Reversed
  ↓       ↓
 Edit   Reverse
  ↓       ↓
Delete   🔒
```

- **Draft**: Volledig bewerkbaar
- **Posted**: Immutable, maar kan reversed worden
- **Reversed**: Immutable, geen acties meer mogelijk

## Handige Tips

### Check balance voor posting
```csharp
var totalDebit = entry.Lines.Sum(l => l.Debit);
var totalCredit = entry.Lines.Sum(l => l.Credit);
var isBalanced = totalDebit == totalCredit;
```

### Filter entries
```http
GET /api/journal-entries?status=1&dateFrom=2026-01-01&dateTo=2026-01-31
```

### Alle draft entries ophalen
```http
GET /api/journal-entries?status=0
```

## Volledige Documentatie

Zie `JOURNAL_ENTRIES_IMPLEMENTATION.md` voor:
- Volledige architectuur details
- Alle geïmplementeerde validaties
- Performance optimalisaties
- Multi-tenancy details
- Test coverage

## Troubleshooting

**API start niet?**
```powershell
# Check of PostgreSQL draait
docker ps

# Check of poort 5001 vrij is
netstat -ano | findstr :5001
```

**Migration errors?**
```powershell
# Reset en opnieuw toepassen
cd backend\src\Api
dotnet ef database drop --project ..\Infrastructure
dotnet ef database update --project ..\Infrastructure
```

**Test script faalt?**
- Zorg dat API draait op http://localhost:5001
- Check of PostgreSQL container draait
- Gebruik test-journal-entries-simple.ps1 voor basic tests

## Volgende Features (Suggesties)

1. **Grootboek rapport** - Account balances per periode
2. **Trial Balance** - Debit/Credit totalen per account
3. **Batch posting** - Meerdere entries tegelijk posten
4. **Templates** - Herbruikbare entry templates
5. **Attachments** - Documenten koppelen aan entries
6. **Approval workflow** - Goedkeuringsproces voor entries
7. **Reporting** - Financial statements genereren

## Support

Voor vragen of problemen, zie:
- `JOURNAL_ENTRIES_IMPLEMENTATION.md` - Complete technische documentatie
- `test-journal-entries-simple.ps1` - Voorbeelden van API gebruik
- API Swagger docs - http://localhost:5001/swagger

---

**Status: ✅ PRODUCTION READY**

Alle core features zijn geïmplementeerd, getest en gedocumenteerd.

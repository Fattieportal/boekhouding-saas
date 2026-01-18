# PSD2 Bank Integration - Quick Start

## Wat is geïmplementeerd?

Volledige PSD2 bankkoppeling met:
- ✅ Abstracte provider interface
- ✅ Mock provider voor testing
- ✅ Token encryptie met Data Protection
- ✅ Transactie import via sync
- ✅ Invoice matching met automatische journal entries
- ✅ Multi-tenant support
- ✅ REST API endpoints
- ✅ Frontend basis (connections & transactions pagina's)

## Snel aan de slag

### 1. Start de backend API

```powershell
cd backend
dotnet run --project src/Api --no-build
```

API draait op: http://localhost:5001

### 2. Test met PowerShell script

```powershell
cd backend
.\test-bank-integration.ps1
```

Dit test de volledige flow:
1. Login
2. Bank connectie initiëren
3. Transacties syncen
4. Factuur aanmaken
5. Transactie matchen
6. Journal entry verifiëren

### 3. Start de frontend (optioneel)

```powershell
cd frontend
npm run dev
```

Navigeer naar:
- http://localhost:3000/banking/connections - Bank connecties
- http://localhost:3000/banking/transactions - Transacties

## API Endpoints

### POST /api/bank/connect
Start nieuwe bank connectie
```json
POST /api/bank/connect
{
  "provider": "Mock"
}
```

### POST /api/bank/connections/{id}/sync
Sync transacties
```json
POST /api/bank/connections/{id}/sync
{
  "from": "2025-12-01T00:00:00",
  "to": "2026-01-17T23:59:59"
}
```

### GET /api/bank/transactions
Haal transacties op (met optionele filters)

### POST /api/bank/transactions/{id}/match
Match transactie aan factuur
```json
POST /api/bank/transactions/{id}/match
{
  "invoiceId": "guid"
}
```

## Voorbeeldflow

```powershell
# 1. Login
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
    -Method Post -Body (@{email="test@example.com"; password="Test123!"} | ConvertTo-Json) `
    -ContentType "application/json"

$token = $loginResponse.token
$tenantId = $loginResponse.user.tenants[0].tenantId

# 2. Connect bank
$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

$connectResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/bank/connect" `
    -Method Post -Headers $headers -Body (@{provider="Mock"} | ConvertTo-Json)

$connectionId = $connectResponse.connectionId

# 3. Sync transactions
$syncResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/api/bank/connections/$connectionId/sync" `
    -Method Post -Headers $headers `
    -Body (@{from=(Get-Date).AddMonths(-1); to=(Get-Date)} | ConvertTo-Json)

# 4. Get transactions
$transactions = Invoke-RestMethod `
    -Uri "http://localhost:5001/api/bank/transactions?connectionId=$connectionId" `
    -Method Get -Headers $headers

# 5. Match transaction (assume invoice exists)
$creditTx = $transactions | Where-Object { $_.amount -gt 0 } | Select-Object -First 1
Invoke-RestMethod `
    -Uri "http://localhost:5001/api/bank/transactions/$($creditTx.id)/match" `
    -Method Post -Headers $headers `
    -Body (@{invoiceId="your-invoice-id"} | ConvertTo-Json)
```

## Business Flow

### Bank Connectie
1. Tenant klikt "Connect Bank"
2. Selecteert provider (Mock voor testing)
3. Wordt doorgestuurd naar consent URL
4. Na toestemming: connectie status = Active
5. Kan nu transacties syncen

### Transactie Sync
1. Tenant klikt "Sync" bij een connectie
2. Selecteert datumbereik (default: laatste maand)
3. API haalt transacties op van provider
4. Transacties worden geïmporteerd (upsert op ExternalId)
5. Status update: LastSyncedAt

### Invoice Matching
1. Tenant ziet lijst van unmatchedtransacties
2. Selecteert credit transactie (betaling)
3. Selecteert bijbehorende factuur
4. Systeem:
   - Maakt journal entry (Dr Bank, Cr Debiteuren)
   - Post de entry
   - Update invoice status naar Paid
   - Markeert transactie als Matched

## Database Schema

### BankConnections
```sql
CREATE TABLE "BankConnections" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid REFERENCES "Tenants",
    "Provider" varchar(50),
    "Status" int,  -- 0=Pending, 1=Active, 2=Expired, 3=Revoked, 4=Error
    "AccessTokenEncrypted" varchar(2000),
    "RefreshTokenEncrypted" varchar(2000),
    "ExpiresAt" timestamp,
    "ExternalConnectionId" varchar(200),
    "BankName" varchar(200),
    "IbanMasked" varchar(50),
    "LastSyncedAt" timestamp
);
```

### BankTransactions
```sql
CREATE TABLE "BankTransactions" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid REFERENCES "Tenants",
    "BankConnectionId" uuid REFERENCES "BankConnections",
    "ExternalId" varchar(200),  -- UNIQUE per tenant
    "BookingDate" timestamp,
    "ValueDate" timestamp,
    "Amount" decimal(18,2),  -- positive = credit, negative = debit
    "Currency" varchar(3),
    "CounterpartyName" varchar(200),
    "CounterpartyIban" varchar(50),
    "Description" varchar(500),
    "MatchedStatus" int,  -- 0=Unmatched, 1=Matched, 2=Manual, 3=Ignored
    "MatchedInvoiceId" uuid REFERENCES "SalesInvoices",
    "JournalEntryId" uuid REFERENCES "JournalEntries",
    "MatchedAt" timestamp
);
```

## Mock Provider

De Mock provider genereert realistische test data:
- 20% kans op transactie per dag
- Random credit/debit transacties (€0-€5000 / €0-€2000)
- 8 verschillende tegenpartijen met IBAN
- Diverse omschrijvingen

Perfect voor testing zonder echte bank API!

## Troubleshooting

### "Provider 'Mock' not found"
Check of MockBankProvider geregistreerd is in DI:
```csharp
services.AddScoped<IBankProvider, MockBankProvider>();
```

### "Cannot match transaction"
Checklist:
- Is transactie credit (amount > 0)?
- Is factuur gepost?
- Bestaan Bank (1010) en Debiteuren (1300) accounts?
- Bestaat Bank journal?

### Frontend auth errors
Update de placeholder auth in:
- `/banking/connections/page.tsx`
- `/banking/transactions/page.tsx`

Met je echte AuthContext.

## Volgende Stappen

### Productie
1. **Kies echte provider**:
   - Nordigen/GoCardless (EU, gratis tot 100 users)
   - Plaid (US/CA)
   - TrueLayer (UK/EU)

2. **Implementeer OAuth callback**:
   ```csharp
   [HttpGet("bank/callback")]
   public async Task<IActionResult> BankCallback(string code, string state)
   {
       // Complete OAuth flow
       // Call provider.CompleteConnectionAsync(...)
   }
   ```

3. **Frontend polish**:
   - Invoice selector modal
   - Auto-match suggesties (via invoice number in description)
   - Batch operations

### Features
- [ ] Partial payments
- [ ] Manual booking (non-invoice transactions)
- [ ] Bank reconciliation report
- [ ] Auto-sync scheduler (background job)
- [ ] Transaction categorization
- [ ] Multiple accounts per tenant

## Support

Voor vragen:
1. Check `BANK_INTEGRATION_README.md` voor details
2. Run `.\test-bank-integration.ps1` voor validatie
3. Check API logs: src/Api/bin/Debug/net8.0/

---

**Status: ✅ READY FOR TESTING**

Run `.\test-bank-integration.ps1` om de volledige flow te testen!

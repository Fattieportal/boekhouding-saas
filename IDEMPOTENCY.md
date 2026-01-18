# Idempotency & Business Invariants

Dit document beschrijft het idempotente gedrag van kritieke API operaties en de hard-enforced business invariants in het systeem.

## 📋 Idempotente Operaties

### 1. POST Invoice (Accounting Entry Creation)

**Endpoint:** `POST /api/salesinvoices/{id}/post`

**Idempotent Gedrag:**
- Als invoice al status `Posted` heeft → return bestaande invoice DTO (geen fout)
- Als invoice status `Draft` of `Sent` heeft → maak journal entry en update status
- Als invoice al andere status heeft → throw InvalidOperationException

**Database State:**
- Journal entry wordt NIET dubbel aangemaakt (status check voorkomt dit)
- JournalEntryId wordt NIET overschreven als al gezet
- Multiple POST calls op dezelfde invoice zijn veilig

**Code Locatie:**
```csharp
// SalesInvoiceService.PostInvoiceAsync (regel ~380)
if (invoice.Status == InvoiceStatus.Posted)
{
    return MapToDto(invoice); // Idempotent return
}
```

**Business Invariants:**
1. Invoice moet lines hebben (Count > 0)
2. Invoice Total moet > 0 zijn
3. Invoice moet status Draft of Sent hebben
4. Calculated totals moeten matchen (binnen 0.01m tolerantie)

---

### 2. Sync Bank Transactions

**Endpoint:** `POST /api/bank/connections/{id}/sync`

**Idempotent Gedrag:**
- Transacties worden geïdentificeerd door `ExternalId` (unique per tenant)
- Bestaande transacties met zelfde ExternalId worden NIET dubbel aangemaakt
- Database constraint: `UNIQUE INDEX ON (TenantId, ExternalId)`

**Database State:**
- INSERT wordt UPSERT via ExternalId check
- Meerdere sync calls voor dezelfde periode zijn veilig
- Transacties die al gematched zijn blijven gematched

**Code Locatie:**
```csharp
// BankService.SyncTransactionsAsync
// Uses ExternalId uniqueness constraint to prevent duplicates
var existingTransaction = await _context.Set<BankTransaction>()
    .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ExternalId == externalId);

if (existingTransaction != null)
{
    // Update existing instead of creating duplicate
    continue;
}
```

**Business Invariants:**
1. ExternalId moet uniek zijn per tenant
2. BankConnection moet Active of Pending status hebben
3. Datum range (from-to) moet geldig zijn

---

### 3. Match Transaction to Invoice

**Endpoint:** `POST /api/bank/transactions/{transactionId}/match`

**Idempotent Gedrag:**
- Transaction met status != Unmatched → throw InvalidOperationException
- Eenmaal gematched kan NIET opnieuw gematched worden
- Multiple match calls op dezelfde transaction falen met duidelijke fout

**Database State:**
- Journal entry wordt NIET dubbel aangemaakt (status check)
- Invoice OpenAmount wordt NIET dubbel verlaagd
- Transaction MatchedStatus voorkomt herhaalde matching

**Code Locatie:**
```csharp
// BankService.MatchTransactionToInvoiceAsync (regel ~223)
if (transaction.MatchedStatus != BankTransactionMatchStatus.Unmatched)
{
    throw new InvalidOperationException("Transaction is already matched");
}
```

**Business Invariants:**
1. Transaction moet status Unmatched hebben
2. Invoice moet OpenAmount > 0 hebben (niet volledig betaald)
3. Invoice moet status Posted of Sent hebben (niet Draft)
4. Transaction Amount moet positief zijn (credit transaction)

---

### 4. Create Journal Entry

**Endpoint:** `POST /api/journalentries`

**Idempotent Gedrag:**
- Geen expliciete idempotency (nieuwe entry wordt altijd aangemaakt)
- Client moet zelf duplicate prevention implementeren
- Aanbeveling: Use unique Reference field voor client-side deduplication

**Business Invariants:**
1. Debit totaal moet gelijk zijn aan Credit totaal
2. Minimaal 2 lines (debit + credit)
3. Entry mag NIET gepost worden zonder balanced lines

---

### 5. Post Journal Entry

**Endpoint:** `POST /api/journalentries/{id}/post`

**Idempotent Gedrag:**
- Als entry al status `Posted` heeft → throw InvalidOperationException
- Posten is NIET herhaalbaar (eenmalige actie)
- Posted entries zijn immutable

**Code Locatie:**
```csharp
// JournalEntryService.PostEntryAsync
if (entry.Status == JournalEntryStatus.Posted)
{
    throw new InvalidOperationException("Journal entry is already posted");
}
```

**Business Invariants:**
1. Entry moet balanced zijn (Debit = Credit)
2. Entry moet minimaal 2 lines hebben
3. Entry moet status Draft hebben
4. Posted entries kunnen NIET meer gewijzigd of verwijderd worden

---

## 🔒 Hard Business Invariants (Enforced)

### Invoice Lifecycle

**Status Transitions:**
```
Draft (0) → Sent (1) → Posted (2) → Paid (3)
          ↓           ↓
       Delete    Immutable
```

**Invariants:**
1. ✅ Alleen Draft invoices kunnen gewijzigd/verwijderd worden
2. ✅ Posten kan alleen vanuit Draft of Sent status
3. ✅ Posted invoices zijn immutable (kan alleen status wijzigen naar Paid)
4. ✅ Paid status wordt automatisch gezet bij OpenAmount = 0

### Payment Matching

**Invariants:**
1. ✅ Transaction kan maar 1x gematched worden (MatchedStatus check)
2. ✅ Invoice moet Posted/Sent zijn (niet Draft)
3. ✅ Invoice moet OpenAmount > 0 hebben
4. ✅ Alleen credit transactions (Amount > 0) kunnen gematched worden

**Code Enforcement:**
```csharp
// BankService.MatchTransactionToInvoiceAsync heeft 4 validaties:
if (transaction.MatchedStatus != BankTransactionMatchStatus.Unmatched)
    throw new InvalidOperationException("Transaction is already matched");

if (invoice.OpenAmount <= 0)
    throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} is already fully paid");

if (invoice.Status == InvoiceStatus.Draft)
    throw new InvalidOperationException($"Cannot match payments to draft invoices");

if (transaction.Amount <= 0)
    throw new InvalidOperationException("Can only match credit transactions");
```

### OpenAmount Tracking

**Invariants:**
1. ✅ OpenAmount wordt geïnitialiseerd met Total bij invoice creation
2. ✅ OpenAmount wordt gereset naar Total bij invoice update (Draft only)
3. ✅ OpenAmount wordt verlaagd bij payment match
4. ✅ Status wordt Paid bij OpenAmount <= 0.01m (rounding tolerance)

**Update Logic:**
```csharp
// Bij payment match:
invoice.OpenAmount -= transaction.Amount;
if (invoice.OpenAmount <= 0.01m)
{
    invoice.OpenAmount = 0;
    invoice.Status = InvoiceStatus.Paid;
}
```

### Journal Entry Immutability

**Invariants:**
1. ✅ Posted journal entries kunnen NIET gewijzigd worden
2. ✅ Posted journal entries kunnen NIET verwijderd worden
3. ✅ Reverse entry moet aangemaakt worden voor correcties
4. ✅ Debit moet altijd gelijk zijn aan Credit

### Multi-Tenancy

**Invariants:**
1. ✅ Alle queries filteren op TenantId (via ITenantContext)
2. ✅ Alle entities hebben TenantId foreign key
3. ✅ Cross-tenant access is onmogelijk (DB constraint + query filter)
4. ✅ JWT moet X-Tenant-Id header bevatten

---

## ⚠️ Niet-Idempotente Operaties

Deze operaties zijn **NIET** idempotent en vereisen client-side duplicate prevention:

### 1. Create Invoice
- Elke POST creëert nieuwe invoice
- Gebruik unique InvoiceNumber voor client-side dedup
- Database heeft GEEN uniqueness constraint op InvoiceNumber

### 2. Create Contact
- Elke POST creëert nieuw contact
- Gebruik Email uniqueness voor client-side dedup
- Database HEEFT uniqueness constraint op Email (per tenant)

### 3. Create Journal Entry
- Elke POST creëert nieuwe entry
- Gebruik unique Reference field voor client-side dedup
- Database heeft GEEN uniqueness constraint op Reference

### 4. Sync Bank (eerste keer per ExternalId)
- Eerste sync voor nieuwe ExternalId creëert transaction
- Tweede sync met zelfde ExternalId skip creatie (idempotent)
- Idempotency werkt via ExternalId uniqueness

---

## 🧪 Testing Idempotency

### Test: Double Post Invoice

```powershell
# First post (should succeed)
POST /api/salesinvoices/{id}/post
# Response: 200 OK, Status = Posted

# Second post (should be idempotent)
POST /api/salesinvoices/{id}/post
# Response: 200 OK, Status = Posted (same invoice returned)
```

### Test: Double Match Transaction

```powershell
# First match (should succeed)
POST /api/bank/transactions/{transactionId}/match
# Response: 200 OK, MatchedStatus = MatchedToInvoice

# Second match (should fail)
POST /api/bank/transactions/{transactionId}/match
# Response: 400 Bad Request
# Error: "Transaction is already matched"
```

### Test: Double Sync Same Period

```powershell
# First sync (imports 10 new transactions)
POST /api/bank/connections/{id}/sync
# Response: { transactionsImported: 10, transactionsUpdated: 0 }

# Second sync (same period, should be idempotent)
POST /api/bank/connections/{id}/sync
# Response: { transactionsImported: 0, transactionsUpdated: 10 }
# (existing transactions updated, no duplicates)
```

---

## 📝 Best Practices

### Voor API Clients

1. **Always check response status:**
   - 200 OK = success (idempotent operations may return existing state)
   - 400 Bad Request = validation error (check error message)
   - 409 Conflict = state conflict (entity already in target state)

2. **Handle idempotent operations gracefully:**
   - PostInvoice returning existing Posted invoice is NOT an error
   - Retry POST operations if network fails
   - Don't create duplicate UI state on idempotent success

3. **Use business keys for deduplication:**
   - InvoiceNumber for invoice lookup
   - ExternalId for bank transactions
   - Email for contacts
   - Reference for journal entries

### Voor Backend Developers

1. **Add status checks before state mutations:**
   ```csharp
   if (entity.Status == TargetStatus)
       return entity; // Idempotent return
   ```

2. **Use database constraints:**
   - UNIQUE constraints for business keys
   - Foreign keys with appropriate DELETE behavior
   - CHECK constraints for business rules

3. **Document non-idempotent operations:**
   - Clearly mark in API docs
   - Add warnings in code comments
   - Provide client-side dedup guidance

---

**Last Updated:** 2026-01-18  
**Applies to:** Fase D MVP (Batches 1-4)

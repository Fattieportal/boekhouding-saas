# FASE D: BATCH 3 & 4 COMPLEET

## ✅ BATCH 3: Enhanced DTOs & Payment Links

### Payment Links Implementatie

**Nieuwe DTOs:**
- File: `backend/src/Application/DTOs/Banking/BankTransactionDto.cs`
- Added 3 DTO classes:
  - `BankTransactionDto` (bestaand, voor sync responses)
  - `BankTransactionFullDto` (nieuw, voor GET /api/bank/transactions)
  - `PaymentTransactionDto` (nieuw, voor gebruik in SalesInvoiceDto)

**BankTransactionFullDto:**
```csharp
public class BankTransactionFullDto
{
    public Guid Id { get; set; }
    public Guid BankConnectionId { get; set; }
    public string BankName { get; set; }
    public string ExternalId { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public string? Description { get; set; }
    public BankTransactionMatchStatus MatchedStatus { get; set; }
    public Guid? MatchedInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }  // Deep link naar factuur
    public Guid? JournalEntryId { get; set; }   // Deep link naar journal
    public DateTime? MatchedAt { get; set; }
}
```

**PaymentTransactionDto:**
```csharp
public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public DateTime BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Description { get; set; }
    public Guid? JournalEntryId { get; set; }  // Deep link naar journal entry
    public DateTime MatchedAt { get; set; }
}
```

### Enhanced SalesInvoiceDto

**File:** `backend/src/Application/DTOs/SalesInvoices/SalesInvoiceDtos.cs`

**Added Property:**
```csharp
public List<PaymentTransactionDto> Payments { get; set; } = new();
```

**Betekenis:**
- Elke factuur toont nu alle matched bank transacties (betalingen)
- Frontend kan payment history weergeven bij factuur detail
- Deep links naar journal entries voor elke betaling

### Service Updates

**1. BankController Enhanced**
- File: `backend/src/Api/Controllers/BankController.cs`
- Changed: `GET /api/bank/transactions` nu mapped naar `BankTransactionFullDto`
- Added using: `Boekhouding.Application.DTOs.Banking`
- Response bevat nu `InvoiceNumber` voor deep link

**2. SalesInvoiceService Enhanced**
- File: `backend/src/Infrastructure/Services/SalesInvoiceService.cs`
- Added using: `Boekhouding.Application.DTOs.Banking`
- Enhanced: `GetInvoiceByIdAsync()` method

**New Logic in GetInvoiceByIdAsync:**
```csharp
// Load matched payments (bank transactions)
var payments = await _context.Set<BankTransaction>()
    .Where(t => t.MatchedInvoiceId == id 
             && t.MatchedStatus == BankTransactionMatchStatus.MatchedToInvoice)
    .OrderBy(t => t.BookingDate)
    .ToListAsync();

dto.Payments = payments.Select(p => new PaymentTransactionDto
{
    Id = p.Id,
    BookingDate = p.BookingDate,
    Amount = p.Amount,
    Currency = p.Currency,
    CounterpartyName = p.CounterpartyName,
    Description = p.Description,
    JournalEntryId = p.JournalEntryId,
    MatchedAt = p.MatchedAt ?? DateTime.UtcNow
}).ToList();
```

### API Contract Changes

**GET /api/salesinvoices/{id}** - Enhanced Response:
```json
{
  "id": "guid",
  "invoiceNumber": "INV-2026-001",
  "status": 2,
  "total": 1210.00,
  "openAmount": 605.00,
  "payments": [
    {
      "id": "guid",
      "bookingDate": "2026-01-15T00:00:00Z",
      "amount": 605.00,
      "currency": "EUR",
      "counterpartyName": "Acme Corp",
      "description": "Payment INV-2026-001",
      "journalEntryId": "guid",
      "matchedAt": "2026-01-15T10:30:00Z"
    }
  ],
  ...
}
```

**GET /api/bank/transactions** - Enhanced Response:
```json
[
  {
    "id": "guid",
    "bankConnectionId": "guid",
    "bankName": "ING Bank",
    "externalId": "ext-123",
    "bookingDate": "2026-01-15T00:00:00Z",
    "amount": 605.00,
    "currency": "EUR",
    "counterpartyName": "Acme Corp",
    "matchedStatus": 1,
    "matchedInvoiceId": "guid",
    "invoiceNumber": "INV-2026-001",  // Deep link
    "journalEntryId": "guid"           // Deep link
  }
]
```

### Deep Links Enabled (Backend Ready)

**Invoice → Payments:**
- ✅ `GET /api/salesinvoices/{id}` bevat `Payments[]` array
- ✅ Elke payment heeft `Id` voor link naar transactie detail
- ✅ Elke payment heeft `JournalEntryId` voor link naar journal

**Bank Transaction → Invoice:**
- ✅ `GET /api/bank/transactions` bevat `InvoiceNumber`
- ✅ Response bevat `MatchedInvoiceId` voor link naar factuur detail
- ✅ Response bevat `JournalEntryId` voor link naar journal

**Frontend Implementation Pending:**
- [ ] Invoice detail page: toon payments tabel met links
- [ ] Transaction page: klikbare invoice number
- [ ] Navigation naar journal entries

---

## ✅ BATCH 4: Financial Reports (P&L & Balance Sheet)

### DTOs Created

**File:** `backend/src/Application/DTOs/Reports/FinancialReportDtos.cs`

**ProfitLossDto (Winst & Verlies):**
```csharp
public class ProfitLossDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Revenue (Opbrengsten)
    public List<AccountLineDto> RevenueAccounts { get; set; }
    public decimal TotalRevenue { get; set; }
    
    // Expenses (Kosten)
    public List<AccountLineDto> ExpenseAccounts { get; set; }
    public decimal TotalExpenses { get; set; }
    
    // Net Income (Winst)
    public decimal NetIncome { get; set; }
}
```

**BalanceSheetDto (Balans):**
```csharp
public class BalanceSheetDto
{
    public DateTime AsOfDate { get; set; }
    
    // Assets (Activa)
    public List<AccountLineDto> AssetAccounts { get; set; }
    public decimal TotalAssets { get; set; }
    
    // Liabilities (Passiva - Schulden)
    public List<AccountLineDto> LiabilityAccounts { get; set; }
    public decimal TotalLiabilities { get; set; }
    
    // Equity (Eigen Vermogen)
    public List<AccountLineDto> EquityAccounts { get; set; }
    public decimal TotalEquity { get; set; }
    
    // Balance check (should be 0)
    public decimal Balance { get; set; } // Assets - (Liabilities + Equity)
}
```

**AccountLineDto (Regel in rapport):**
```csharp
public class AccountLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; }
    public string AccountName { get; set; }
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}
```

### Service Implementation

**Interface:**
- File: `backend/src/Application/Interfaces/IFinancialReportService.cs`
- Methods:
  - `GetProfitLossAsync(DateTime from, DateTime to)`
  - `GetBalanceSheetAsync(DateTime asOf)`

**Implementation:**
- File: `backend/src/Infrastructure/Services/FinancialReportService.cs`
- Lines: 245 total

**P&L Logic:**
```csharp
// Load all POSTED journal entries in date range
var entries = await _context.Set<JournalEntry>()
    .Include(e => e.Lines).ThenInclude(l => l.Account)
    .Where(e => e.TenantId == tenantId 
             && e.Status == JournalEntryStatus.Posted
             && e.EntryDate >= from 
             && e.EntryDate <= to)
    .ToListAsync();

// Group by account and calculate balances
// Revenue: Credit - Debit (normal credit balance)
// Expense: Debit - Credit (normal debit balance)
// Net Income = Total Revenue - Total Expenses
```

**Balance Sheet Logic:**
```csharp
// Load all POSTED entries up to asOf date
var entries = await _context.Set<JournalEntry>()
    .Include(e => e.Lines).ThenInclude(l => l.Account)
    .Where(e => e.TenantId == tenantId 
             && e.Status == JournalEntryStatus.Posted
             && e.EntryDate <= asOf)
    .ToListAsync();

// Group by account and calculate balances
// Asset: Debit - Credit (normal debit balance)
// Liability: Credit - Debit (normal credit balance)
// Equity: Credit - Debit (normal credit balance)
// Balance check: Assets = Liabilities + Equity
```

**CalculateBalance Method:**
```csharp
private decimal CalculateBalance(AccountType accountType, List<JournalLine> lines)
{
    var totalDebit = lines.Sum(l => l.Debit);
    var totalCredit = lines.Sum(l => l.Credit);
    
    return accountType switch
    {
        AccountType.Asset => totalDebit - totalCredit,
        AccountType.Liability => totalCredit - totalDebit,
        AccountType.Equity => totalCredit - totalDebit,
        AccountType.Revenue => totalCredit - totalDebit,
        AccountType.Expense => totalDebit - totalCredit,
        _ => 0
    };
}
```

### Controller Implementation

**File:** `backend/src/Api/Controllers/FinancialReportsController.cs`

**Endpoints:**

**1. GET /api/reports/profit-loss**
- Query params: `from` (defaults to start of current year), `to` (defaults to today)
- Returns: `ProfitLossDto`

**2. GET /api/reports/balance-sheet**
- Query params: `asOf` (defaults to today)
- Returns: `BalanceSheetDto`

**Example Requests:**
```bash
# P&L for current month
GET /api/reports/profit-loss?from=2026-01-01&to=2026-01-31

# Balance Sheet as of today
GET /api/reports/balance-sheet

# Balance Sheet as of year-end 2025
GET /api/reports/balance-sheet?asOf=2025-12-31
```

### DI Registration

**File:** `backend/src/Infrastructure/DependencyInjection.cs`
```csharp
services.AddScoped<IFinancialReportService, FinancialReportService>();
```

### Example Response Formats

**P&L Report:**
```json
{
  "fromDate": "2026-01-01",
  "toDate": "2026-01-31",
  "revenueAccounts": [
    {
      "accountId": "guid",
      "accountCode": "8000",
      "accountName": "Sales Revenue",
      "accountType": 4,
      "balance": 50000.00,
      "transactionCount": 15
    }
  ],
  "totalRevenue": 50000.00,
  "expenseAccounts": [
    {
      "accountId": "guid",
      "accountCode": "6000",
      "accountName": "Operating Expenses",
      "accountType": 5,
      "balance": 12000.00,
      "transactionCount": 8
    }
  ],
  "totalExpenses": 12000.00,
  "netIncome": 38000.00
}
```

**Balance Sheet:**
```json
{
  "asOfDate": "2026-01-18",
  "assetAccounts": [
    {
      "accountId": "guid",
      "accountCode": "1010",
      "accountName": "Bank",
      "accountType": 1,
      "balance": 75000.00,
      "transactionCount": 42
    },
    {
      "accountId": "guid",
      "accountCode": "1300",
      "accountName": "Debtors",
      "accountType": 1,
      "balance": 25000.00,
      "transactionCount": 20
    }
  ],
  "totalAssets": 100000.00,
  "liabilityAccounts": [
    {
      "accountId": "guid",
      "accountCode": "2000",
      "accountName": "Creditors",
      "accountType": 2,
      "balance": 15000.00,
      "transactionCount": 10
    }
  ],
  "totalLiabilities": 15000.00,
  "equityAccounts": [
    {
      "accountId": "guid",
      "accountCode": "3000",
      "accountName": "Share Capital",
      "accountType": 3,
      "balance": 50000.00,
      "transactionCount": 1
    },
    {
      "accountId": "guid",
      "accountCode": "3900",
      "accountName": "Retained Earnings",
      "accountType": 3,
      "balance": 35000.00,
      "transactionCount": 50
    }
  ],
  "totalEquity": 85000.00,
  "balance": 0.00  // Should be 0 if balanced (Assets = Liabilities + Equity)
}
```

---

## 📊 Files Modified/Created

### Batch 3 (6 files)

**Modified:**
1. `Application/DTOs/Banking/BankTransactionDto.cs` - Added 2 new DTO classes
2. `Application/DTOs/SalesInvoices/SalesInvoiceDtos.cs` - Added Payments property
3. `Api/Controllers/BankController.cs` - Use BankTransactionFullDto instead of anonymous
4. `Infrastructure/Services/SalesInvoiceService.cs` - Load payments in GetInvoiceByIdAsync

### Batch 4 (4 files)

**Created:**
1. `Application/DTOs/Reports/FinancialReportDtos.cs` - 3 DTO classes (58 lines)
2. `Application/Interfaces/IFinancialReportService.cs` - Interface (17 lines)
3. `Infrastructure/Services/FinancialReportService.cs` - Implementation (245 lines)
4. `Api/Controllers/FinancialReportsController.cs` - 2 endpoints (54 lines)

**Modified:**
1. `Infrastructure/DependencyInjection.cs` - Added service registration

---

## 🧪 Testing Commands

### Test Payment Links

```powershell
# Login
$baseUrl = "http://localhost:5001/api"
$body = @{ email = "admin@demo.local"; password = "Admin123!" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $body -ContentType "application/json"
$token = $response.token
$tenant = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Method Get -Headers @{ "Authorization" = "Bearer $token" }
$headers = @{ "Authorization" = "Bearer $token"; "X-Tenant-Id" = $tenant.id }

# Get invoice with payments
$invoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices/{invoiceId}" -Method Get -Headers $headers
Write-Host "Invoice $($invoice.invoiceNumber) has $($invoice.payments.Count) payments"
$invoice.payments | ConvertTo-Json

# Get bank transactions with invoice links
$transactions = Invoke-RestMethod -Uri "$baseUrl/bank/transactions" -Method Get -Headers $headers
$matched = $transactions | Where-Object { $_.matchedStatus -eq 1 }
Write-Host "Matched transactions: $($matched.Count)"
$matched | Select-Object invoiceNumber, amount, bookingDate | Format-Table
```

### Test Financial Reports

```powershell
# P&L for current month
$from = (Get-Date -Day 1).ToString("yyyy-MM-dd")
$to = (Get-Date).ToString("yyyy-MM-dd")
$pl = Invoke-RestMethod -Uri "$baseUrl/reports/profit-loss?from=$from&to=$to" -Method Get -Headers $headers
Write-Host "P&L Report ($from to $to):"
Write-Host "  Total Revenue: $($pl.totalRevenue)"
Write-Host "  Total Expenses: $($pl.totalExpenses)"
Write-Host "  Net Income: $($pl.netIncome)"
$pl.revenueAccounts | Select-Object accountCode, accountName, balance | Format-Table

# Balance Sheet as of today
$bs = Invoke-RestMethod -Uri "$baseUrl/reports/balance-sheet" -Method Get -Headers $headers
Write-Host "Balance Sheet as of $(Get-Date -Format 'yyyy-MM-dd'):"
Write-Host "  Total Assets: $($bs.totalAssets)"
Write-Host "  Total Liabilities: $($bs.totalLiabilities)"
Write-Host "  Total Equity: $($bs.totalEquity)"
Write-Host "  Balance Check: $($bs.balance) (should be 0.00)"
$bs.assetAccounts | Select-Object accountCode, accountName, balance | Format-Table
```

---

## 🎯 Definition of Done - Batch 3 & 4

### Batch 3: Enhanced DTOs & Payment Links
- [x] PaymentTransactionDto created
- [x] BankTransactionFullDto created
- [x] SalesInvoiceDto enhanced with Payments property
- [x] BankController uses proper DTOs
- [x] SalesInvoiceService loads payments
- [x] Backend builds successfully
- [ ] Frontend displays payment list on invoice detail
- [ ] Frontend shows invoice link on transaction page
- [ ] End-to-end navigation tested

### Batch 4: Financial Reports
- [x] P&L DTO created
- [x] Balance Sheet DTO created
- [x] AccountLineDto created
- [x] IFinancialReportService interface
- [x] FinancialReportService implementation
- [x] Balance calculation logic (correct debit/credit treatment)
- [x] GET /api/reports/profit-loss endpoint
- [x] GET /api/reports/balance-sheet endpoint
- [x] Service registered in DI
- [x] Backend builds successfully
- [ ] Integration tests with demo data
- [ ] Frontend reports page

---

## 📝 Next Steps (Batch 5-6)

### Batch 5: Frontend Dashboard & Deep Links (2 hours)
- [ ] Update frontend Dashboard page to use /api/dashboard
- [ ] Display all metrics (invoices, revenue, bank, activity)
- [ ] Add clickable links in activity feed
- [ ] Invoice detail: show payments table
- [ ] Transaction page: clickable invoice numbers
- [ ] Reports page: P&L and Balance Sheet with charts

### Batch 6: Documentation & Smoke Tests (1 hour)
- [ ] Update root README with new endpoints
- [ ] Create BUSINESS_RULES.md (all invariants documented)
- [ ] Document idempotency behavior
- [ ] Create `test-phase-d-complete.ps1` smoke test
- [ ] Test all glue connections end-to-end
- [ ] Create PHASE_D_COMPLETE.md summary

---

**Status: FASE D BATCHES 3-4 COMPLEET ✅**  
**Build: SUCCESS ✅**  
**New Endpoints: 2 (P&L, Balance Sheet) ✅**  
**Enhanced DTOs: 5 ✅**  
**Deep Links: BACKEND READY ✅**

# FASE D: MVP GLUE - GAP ANALYSIS

## 🔍 Geïdentificeerde MVP Gaps

### P0 - Critical (moet voor MVP)

**G1. Dashboard Aggregate Endpoint ontbreekt**
- **Status**: ❌ Niet aanwezig
- **Impact**: Frontend dashboard toont alleen stubs, geen echte data
- **Oplossing**: GET /api/dashboard met aggregated metrics
- **Wat**: Invoices (unpaid, overdue, paid), Revenue, Bank sync status, Recent activity

**G2. OpenAmount tracking in SalesInvoice**
- **Status**: ❌ Niet aanwezig
- **Impact**: Kan niet bepalen welke invoices gedeeltelijk betaald zijn
- **Datamodel**: SalesInvoice heeft wel Total maar geen `OpenAmount` of `PaidAmount`
- **Oplossing**: Voeg `OpenAmount` toe (computed of stored)

**G3. Inconsistente Unpaid/Overdue definitie**
- **Status**: ⚠️ Impliciete logica
- **Impact**: Frontend en backend kunnen anders definiëren wanneer invoice "open" is
- **Huidige logica**: Status enum (Draft/Sent/Posted/Paid) maar geen expliciete "Unpaid" check
- **Oplossing**: Consistente business rules + query filters

**G4. Invoice ↔ Payment links ontbreken in DTO**
- **Status**: ⚠️ Partieel (database heeft links, DTO exposeert niet alles)
- **Database**: 
  - `SalesInvoice.JournalEntryId` ✅
  - `BankTransaction.MatchedInvoiceId` ✅
  - `BankTransaction.JournalEntryId` ✅
- **DTO**: 
  - `SalesInvoiceDto.JournalEntryId` ✅ exposed
  - `SalesInvoiceDto` mist: `PaidTransactions[]`, `OpenAmount`
  - `BankTransactionDto` mist: `InvoiceNumber` visible

**G5. Duplicate match prevention**
- **Status**: ⚠️ Bestaat maar niet 100% foolproof
- **Huidige check**: `BankService` checkt `transaction.MatchedStatus != Unmatched`
- **Missend**: Geen check of invoice al fully paid is via andere transactie
- **Oplossing**: Check `invoice.OpenAmount > 0` voor match toestaan

### P1 - High Priority (maakt MVP "samenhangend")

**G6. Deep links tussen modules**
- **Status**: ⚠️ Partieel aanwezig
- **Frontend**: Banking transactions tonen invoice link, maar invoice detail toont geen payment info
- **Ontbreekt**:
  - Invoice detail → "View Journal Entry" link
  - Invoice detail → "View Payment Transaction(s)" link
  - Bank transaction detail → "View Journal Entry" link
- **Oplossing**: Enhance DTOs + add UI links

**G7. Overdue invoices query filter**
- **Status**: ❌ Niet aanwezig
- **API**: GET /api/sales-invoices heeft geen `?overdue=true` filter
- **Oplossing**: Voeg query parameter toe + backend filter logic

**G8. Recent Activity feed (cross-module)**
- **Status**: ❌ Dashboard toont geen activity
- **AuditLog**: Heeft alle data, maar geen UI rendering
- **Oplossing**: Dashboard endpoint haalt top 10 recent audit logs + maps to readable labels

**G9. Balance Sheet & P&L reports**
- **Status**: ❌ Niet geïmplementeerd
- **Huidige reports**: Alleen VAT report + AR report
- **MVP Need**: Minimal P&L (Revenue vs Expense totals) + Balance Sheet (Assets/Liabilities/Equity)
- **Oplossing**: 2 nieuwe endpoints die JournalLines aggregeren

**G10. Idempotency documentation**
- **Status**: ✅ Code heeft idempotency, maar niet gedocumenteerd
- **Voorbeeld**: `PostInvoiceAsync` checkt `if (invoice.Status == Posted) return existing`
- **Oplossing**: Expliciet documenteren in API specs + add tests

## 🎯 Prioriteitsmatrix

| Gap | Priority | Impact | Effort | Deliverable |
|-----|----------|--------|--------|-------------|
| G1 - Dashboard endpoint | P0 | High | Medium | 1 controller + service method |
| G2 - OpenAmount tracking | P0 | High | Medium | Migration + DTO update |
| G3 - Unpaid/Overdue rules | P0 | High | Low | Business logic + filters |
| G4 - Invoice payment links | P0 | Medium | Low | DTO enhancement |
| G5 - Duplicate prevention | P0 | Medium | Low | Validation logic |
| G6 - Deep links UI | P1 | Medium | Low | Frontend enhancements |
| G7 - Overdue filter | P1 | Medium | Low | Query parameter |
| G8 - Activity feed | P1 | Medium | Low | Dashboard integration |
| G9 - Financial reports | P1 | High | High | 2 endpoints + logic |
| G10 - Idempotency docs | P1 | Low | Low | Documentation |

## 📦 Implementatie Batches

### Batch 1: Core Data Model Fixes (D2)
- G2: Add OpenAmount to SalesInvoice
- G3: Define Unpaid/Overdue business rules
- G5: Enhance duplicate prevention

### Batch 2: Dashboard Endpoint (D2)
- G1: Create /api/dashboard endpoint
- G8: Integrate activity feed

### Batch 3: Query Filters & Links (D3, D4)
- G4: Enhance DTOs with payment links
- G7: Add overdue filter
- G6: Frontend deep links

### Batch 4: Reports (D5)
- G9: P&L report
- G9: Balance Sheet report

### Batch 5: Documentation & Tests (D8, D10)
- G10: Idempotency docs
- Smoke test script

## 🔗 Bestaande Infrastructure (Good News!)

**Wat werkt al goed:**
- ✅ SalesInvoice.JournalEntryId relationship exists
- ✅ BankTransaction.MatchedInvoiceId + JournalEntryId exist
- ✅ AuditLog captures all actions
- ✅ Tenant isolation via middleware
- ✅ JWT authentication working
- ✅ Invoice posting creates journal entries correctly
- ✅ Bank matching updates invoice to Paid (if amount >= total)
- ✅ Idempotent post invoice (returns existing if already posted)

**Wat needs glue:**
- ⚠️ DTOs don't expose all relationships (e.g., payment info on invoice)
- ⚠️ No aggregate dashboard data
- ⚠️ No OpenAmount tracking (partial payments not supported)
- ⚠️ No overdue invoice queries
- ⚠️ Missing financial reports (P&L, Balance Sheet)

## 📊 Expected Outcome

Na Fase D:
- ✅ Dashboard toont real-time business metrics
- ✅ Invoices hebben duidelijke "Open Amount" + overdue status
- ✅ Deep links tussen invoice ↔ transaction ↔ journal entry
- ✅ Duplicate matching impossible
- ✅ Minimal financial reporting (P&L, Balance Sheet)
- ✅ Recent activity feed visible
- ✅ Consistent business rules documented
- ✅ Smoke test validates end-to-end flow


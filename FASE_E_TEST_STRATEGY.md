# Fase E: Test & Verificatie Strategie

**Datum:** 18 januari 2026  
**Status:** ✅ ACTIEF  
**Doel:** Bewijsbaar maken dat de MVP altijd werkt via herhaalbare tests

---

## 📊 E1: Teststrategie Analyse

### Huidige Situatie

**Bestaande Test Infrastructure:**
- ✅ **PowerShell Scripts**: 20+ smoke test scripts (test-*.ps1)
- ✅ **Docker Compose**: Postgres + pgAdmin setup
- ✅ **Demo Seeding**: Automatische data seeding in ApplicationDbContext
- ❌ **xUnit Tests**: Geen integration test project aanwezig
- ❌ **E2E Tests**: Playwright alleen voor PDF rendering, niet voor testing

**Bestaande Scripts:**
1. `test-demo-complete.ps1` - Full flow (Login → Tenant → Invoices → Journal → Bank)
2. `test-phase-d-quick.ps1` - Dashboard + Reports smoke test (9/9 PASSING)
3. `test-auth.ps1` - Authentication flow
4. `test-sales-invoices.ps1` - Invoice CRUD
5. `test-bank-integration.ps1` - Bank sync + matching
6. `test-vat-report.ps1` - VAT reporting

### Gekozen Strategie

**Backend Testing:**
- **Approach**: xUnit Integration Tests met WebApplicationFactory
- **Database**: Real Postgres via docker-compose (geen mocks)
- **Scope**: Happy path + idempotency + failure scenarios

**Frontend Testing:**
- **Approach**: PowerShell CLI smoke test (uitbreiding van test-phase-d-quick.ps1)
- **Rationale**: Sneller dan Playwright setup, bestaande scripts hergebruiken
- **Scope**: Complete flow via REST API calls

**Test Environment:**
- **Infra Start**: `docker compose up -d` (Postgres + pgAdmin)
- **Migrations**: Automatisch via EF Core migrations
- **Seeding**: Demo tenant + users via ApplicationDbContext seeding
- **API**: Gestart via `dotnet run` of in-memory via WebApplicationFactory

---

## 🏗️ Architectuur

```
┌─────────────────────────────────────────────────────────┐
│                  FASE E TEST PYRAMID                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────────────────────────────────┐        │
│  │   E2E Smoke Test (PowerShell)              │        │
│  │   - Full MVP flow                          │        │
│  │   - REST API calls                         │        │
│  │   - Real database                          │        │
│  └────────────────────────────────────────────┘        │
│                        ▲                                │
│                        │                                │
│  ┌────────────────────────────────────────────┐        │
│  │   Integration Tests (xUnit)                │        │
│  │   - WebApplicationFactory                  │        │
│  │   - Real Postgres (Testcontainers)         │        │
│  │   - Happy path scenarios                   │        │
│  │   - Idempotency checks                     │        │
│  │   - Failure scenarios                      │        │
│  └────────────────────────────────────────────┘        │
│                        ▲                                │
│                        │                                │
│  ┌────────────────────────────────────────────┐        │
│  │   Infrastructure Layer                     │        │
│  │   - Docker Compose (Postgres 16)           │        │
│  │   - EF Core Migrations                     │        │
│  │   - Demo Seeding                           │        │
│  └────────────────────────────────────────────┘        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 Test Scope

### E2: Backend Integration Test - Happy Path

**Flow:**
1. ✅ Start database + migrations
2. ✅ Run dev seeder (Demo tenant + users + data)
3. ✅ Login als demo user
4. ✅ Select tenant
5. ✅ Create contact
6. ✅ Create sales invoice
7. ✅ Render PDF
8. ✅ Post invoice
9. ✅ Bank sync (mock provider)
10. ✅ Match transaction
11. ✅ Invoice wordt Paid
12. ✅ Haal VAT report op
13. ✅ Haal dashboard op

**Assertions:**
- JWT token is valid and contains correct claims
- TenantId is correctly applied to all operations
- Invoice totals are calculated correctly
- JournalEntry is balanced (debit = credit)
- BankTransaction is matched to correct invoice
- Invoice status changes to Paid after match
- VAT report contains correct amounts
- Dashboard shows accurate metrics

### E3: Idempotency Tests

**Tests:**
1. Post invoice 2x → Only 1 JournalEntry created
2. Bank sync 2x → No duplicate transactions
3. Match transaction 2x → Error on second attempt

### E4: Failure Scenarios

**Tests:**
1. Post invoice without lines → 400 Bad Request
2. Match on non-posted invoice → 400 Bad Request
3. No X-Tenant-Id header → 401 Unauthorized
4. Access to other tenant's data → 403 Forbidden

### E5: Frontend Smoke Test

**PowerShell Script (`test-mvp-complete.ps1`):**
- Complete MVP flow via REST API
- All 13 happy path steps
- Detailed output with assertions
- Exit code 0 on success, 1 on failure

---

## 🚀 Test Execution

### Commands

```powershell
# 1. Start infrastructure
cd infra
docker compose up -d

# Wait for Postgres to be ready
Start-Sleep -Seconds 10

# 2. Run migrations
cd ../backend
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# 3. Start API (in background)
cd src/Api
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run" -WindowStyle Minimized

# Wait for API to start
Start-Sleep -Seconds 10

# 4. Run integration tests
cd ../..
dotnet test

# 5. Run smoke test
cd backend
.\test-mvp-complete.ps1
```

### One-Liner Verification

```powershell
# Complete MVP verification (clean environment)
docker compose -f infra/docker-compose.yml up -d; Start-Sleep 10; cd backend; dotnet ef database drop --force --project src/Infrastructure --startup-project src/Api; dotnet ef database update --project src/Infrastructure --startup-project src/Api; cd src/Api; Start-Process powershell -ArgumentList "-NoExit","-Command","dotnet run" -WindowStyle Minimized; Start-Sleep 10; cd ..\..; dotnet test; .\test-mvp-complete.ps1
```

---

## ✅ Deliverables Checklist

- [ ] E1: Test strategy document (THIS FILE)
- [ ] E2: xUnit Integration Test project
  - [ ] MvpHappyPathTests.cs (13 steps)
  - [ ] Test helpers (login, create invoice, etc.)
- [ ] E3: Idempotency tests
  - [ ] IdempotencyTests.cs (3 scenarios)
- [ ] E4: Failure scenario tests
  - [ ] FailureScenarioTests.cs (4 scenarios)
- [ ] E5: Frontend smoke test
  - [ ] test-mvp-complete.ps1 (full flow)
- [ ] E6: Test runner commands
  - [ ] run-all-tests.ps1 (orchestrator script)
- [ ] E7: Documentation
  - [ ] Updated README.md with test instructions
  - [ ] FASE_E_TEST_COMPLETE.md (completion report)

---

## 📈 Success Criteria

**Definition of Done:**
- ✅ All integration tests passing (green)
- ✅ Smoke test completes without errors
- ✅ Can verify MVP with 1 command
- ✅ Tests run on clean environment (drop + recreate DB)
- ✅ Documentation shows "How to verify MVP works"

**Expected Output:**
```
===============================
MVP VERIFICATION COMPLETE
===============================
✅ Integration Tests: 20/20 PASSED
✅ Smoke Test: 13/13 STEPS OK
✅ MVP Status: FULLY FUNCTIONAL

Time: 45 seconds
Database: PostgreSQL 16
API: ASP.NET Core 8.0
```

---

## 🛠️ Implementation Notes

### xUnit Setup
- Use `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory)
- Use `Testcontainers.PostgreSql` for database (optional, can use existing docker)
- Configure test appsettings.json with test connection string

### PowerShell Script Patterns
```powershell
# Error handling
$ErrorActionPreference = "Stop"
try {
    # Test logic
    if ($condition) {
        Write-Host "  ✅ Assertion passed" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Assertion failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
```

### Test Data Strategy
- Use demo seeding for base data (tenant, users, chart of accounts)
- Create test-specific data in tests (contacts, invoices, transactions)
- Clean up after tests (or use transaction rollback)

---

## 📝 Next Steps

**Immediate Actions:**
1. Create `backend/tests/Api.IntegrationTests` project
2. Implement `MvpHappyPathTests.cs` with 13-step flow
3. Create `test-mvp-complete.ps1` smoke test
4. Add `run-all-tests.ps1` orchestrator
5. Update root README.md with verification instructions

**Priority:**
🔴 HIGH - E2 (Happy path test) - Critical for MVP verification  
🟡 MEDIUM - E3 & E4 (Idempotency + Failures) - Important for robustness  
🟢 LOW - Documentation polish - Nice to have

**Time Estimate:**
- E2 (Happy path): 45 minutes
- E3 (Idempotency): 20 minutes
- E4 (Failures): 15 minutes
- E5 (Smoke test): 15 minutes
- E6 (Runner): 10 minutes
- E7 (Docs): 15 minutes
**Total: ~2 hours**

---

**Status:** Ready for implementation ✅

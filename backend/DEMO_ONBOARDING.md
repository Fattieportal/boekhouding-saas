# 🎯 Demo Onboarding Guide - Complete End-to-End Flow

Deze guide helpt je om de **Demo Company BV** tenant te testen met realistische data: van login tot VAT rapport.

## 📋 Prerequisites

- **.NET 8 SDK** geïnstalleerd
- **Docker Desktop** draaiend (voor PostgreSQL)
- **PowerShell** (voor test scripts)
- **Git** (optional, voor clonen)

---

## 🚀 Quick Start - Van Scratch naar Demo in 5 minuten

### Stap 1: Start Database
```powershell
cd infra
docker-compose up -d
```

**Verwacht resultaat:** PostgreSQL draait op `localhost:5432`

### Stap 2: Start Backend + Auto-Seeding
```powershell
cd backend/src/Api
dotnet run
```

**Verwacht resultaat:**
```
🌱 Starting Database Seeding...
  ✅ Created 3 test users
  ✅ Seeded accounting data for 2 tenants
  ✅ Seeded contacts for 2 tenants

🌱 Starting Demo Data Seeding...
  👥 Seeding demo users...
    ✅ Created admin@demo.local
    ✅ Created accountant@demo.local
  🏢 Seeding demo tenant...
    ✅ Created tenant: Demo Company BV
    ✅ Linked users to demo tenant
    ✅ Created tenant branding
    ✅ Created invoice template
  👥 Seeding demo contacts...
    ✅ Created 3 demo contacts
  📄 Seeding demo invoices...
    ✅ Created 2 demo invoices
    📖 Posting invoice DEMO-2026-0001 to accounting...
      ✅ Created journal entry DEMO-2026-0001
  🏦 Seeding demo bank data...
    ✅ Created bank connection
    ✅ Created 2 bank transactions (1 matched, 1 unmatched)
    📖 Creating bank payment journal entry...
      ✅ Created bank payment journal entry
🎉 Demo Data Seeding Completed!

Now listening on: https://localhost:7195
```

API is nu beschikbaar op **https://localhost:7195**

### Stap 3: Test de Demo Flow
```powershell
cd backend
.\test-demo-complete.ps1
```

**Verwacht resultaat:**
```
=== DEMO COMPLETE FLOW TEST ===

Step 1: Login as admin@demo.local...
  ✅ Logged in. Token: eyJhbGciOiJIUzI1NiI...

Step 2: Get Demo Company BV tenant...
  ✅ Demo Tenant: Demo Company BV
     ID: <guid>
     KvK: 12345678
     BTW: NL123456789B01

Step 3: Get demo contacts...
  ✅ Found 3 contacts:
     - Acme Corporation (Customer)
     - TechStart Solutions (Customer)
     - Office Supplies Nederland (Supplier)

Step 4: Get demo sales invoices...
  ✅ Found 2 invoices:
     - DEMO-2026-0001: €1210 (Paid)
     - DEMO-2026-0002: €545 (Sent)

Step 5: Get journal entries...
  ✅ Found 2 journal entries:
     - DEMO-2026-0001: Sales Invoice DEMO-2026-0001 (Posted)
     - demo-tx-001: Payment for DEMO-2026-0001 (Posted)

Step 6: Get bank connections...
  ✅ Found 1 bank connection(s):
     - Demo Bank NL: NL**DEMO****1234 (Active)

Step 7: Get bank transactions...
  ✅ Found 2 bank transactions:
     - €1210 from Acme Corporation → Matched to invoice
     - €-125.5 from Office Supplies B.V. → Unmatched

Step 8: Generate VAT report...
  ✅ VAT Report Q1 2026:
     Total Sales (excl VAT): €1500
     Total VAT: €255
     VAT by Rate:
       - 21%: €210
       - 9%: €45

Step 9: Get tenant branding...
  ✅ Tenant Branding:
     Primary Color: #1E40AF
     Font: Inter, sans-serif

Step 10: Get invoice templates...
  ✅ Found 1 template(s):
     - Professional Template (default)

Step 11: Get audit logs...
  ✅ Found X audit log entries (showing last 10)

=== DEMO FLOW SUMMARY ===
✅ Authentication working (admin@demo.local)
✅ Tenant: Demo Company BV
✅ Contacts: 3 contacts seeded
✅ Invoices: 2 invoices (1 posted)
✅ Accounting: 2 journal entries
✅ Banking: 1 connection, 2 transactions
✅ VAT Report: €255 total VAT
✅ Branding & Templates configured
✅ Audit logging active

🎉 DEMO COMPLETE FLOW: ALL CHECKS PASSED!
```

---

## 🔐 Demo Credentials

### Admin User
- **Email:** `admin@demo.local`
- **Password:** `Admin123!`
- **Role:** Admin (volledige rechten)

### Accountant User
- **Email:** `accountant@demo.local`
- **Password:** `Admin123!`
- **Role:** Accountant (read + create)

---

## 📊 Demo Tenant: Demo Company BV

### Bedrijfsgegevens
- **Naam:** Demo Company BV
- **KvK:** 12345678
- **BTW-nummer:** NL123456789B01
- **Land:** Nederland

### Seeded Data

#### Contacts (3)
1. **Acme Corporation** (Customer)
   - Email: finance@acme.example
   - BTW: NL999888777B01
   - Plaats: Amsterdam

2. **TechStart Solutions** (Customer)
   - Email: billing@techstart.example
   - BTW: NL888777666B01
   - Plaats: Utrecht

3. **Office Supplies Nederland** (Supplier)
   - Email: verkoop@officesupplies.example
   - BTW: NL777666555B01
   - Plaats: Rotterdam

#### Sales Invoices (2)
1. **DEMO-2026-0001** (Posted → Paid)
   - Klant: Acme Corporation
   - Datum: 5 januari 2026
   - Bedrag: €1.000 + €210 BTW (21%) = €1.210
   - Status: **Paid** (betaald via bank transactie)
   - Journal Entry: Automatisch geboekt naar Debiteuren/Omzet/BTW

2. **DEMO-2026-0002** (Sent)
   - Klant: TechStart Solutions
   - Datum: 15 januari 2026
   - Bedrag: €500 + €45 BTW (9%) = €545
   - Status: **Sent** (nog niet betaald)

#### Bank Integration
- **Provider:** Mock Bank NL
- **IBAN:** NL**DEMO****1234
- **Status:** Active

**Transactions (2):**
1. **€1.210 IN** van Acme Corporation
   - Datum: 10 januari 2026
   - Status: **Matched** → DEMO-2026-0001 → Invoice status = Paid
   - Journal Entry: Bank/Debiteuren boeking

2. **€125,50 UIT** naar Office Supplies B.V.
   - Datum: 16 januari 2026
   - Status: **Unmatched** (nog niet gematcht)

#### Accounting
- **Grootboekrekeningen:** Standaard NL grootboekschema (1000-9999)
- **Dagboeken:** Inkoop, Verkoop, Bank, Memoriaal
- **Journal Entries:** 2 geboekte entries (invoice + betaling)

#### Branding & Templates
- **Logo URL:** Placeholder demo logo
- **Kleuren:** Blue (#1E40AF) + Slate (#64748B)
- **Font:** Inter
- **Template:** Professional Template (Scriban HTML/CSS)

---

## 🛠️ Happy Path - 10 Stappen

### 1. Login
```powershell
POST /api/auth/login
Body: { "email": "admin@demo.local", "password": "Admin123!" }
```
**Zie:** JWT token + userId

### 2. Get Tenants
```powershell
GET /api/tenants
Header: Authorization: Bearer {token}
```
**Zie:** Demo Company BV in lijst

### 3. Set Tenant Context
```powershell
GET /api/contacts
Header: Authorization: Bearer {token}
Header: X-Tenant-Id: {demoTenantId}
```
**Zie:** 3 contacts (Acme, TechStart, Office Supplies)

### 4. View Invoices
```powershell
GET /api/salesinvoices
```
**Zie:** 2 invoices (1 Paid, 1 Sent)

### 5. View Invoice Details
```powershell
GET /api/salesinvoices/{invoiceId}
```
**Zie:** Lines, VAT, total, status, contact info

### 6. View Journal Entries
```powershell
GET /api/journalentries
```
**Zie:** Posted entries voor invoice + betaling

### 7. View Bank Connections
```powershell
GET /api/bank/connections
```
**Zie:** Mock Bank NL (Active)

### 8. View Bank Transactions
```powershell
GET /api/bank/transactions
```
**Zie:** 2 transactions (1 matched, 1 unmatched)

### 9. Generate VAT Report
```powershell
GET /api/reports/vat?year=2026&quarter=1
```
**Zie:** €1.500 sales, €255 VAT (€210 @ 21%, €45 @ 9%)

### 10. View Audit Logs
```powershell
GET /api/auditlogs?pageSize=10
```
**Zie:** Alle acties (login, create invoice, match transaction, etc.)

---

## 🧪 Test Scripts Overzicht

| Script | Doel |
|--------|------|
| `test-demo-complete.ps1` | **Volledige demo flow** (alle 11 stappen) |
| `test-auth.ps1` | Test authenticatie met demo users |
| `test-contacts.ps1` | Test contact CRUD voor demo tenant |
| `test-sales-invoices.ps1` | Test invoice workflow |
| `test-bank-integration.ps1` | Test bank connections + matching |
| `test-vat-report.ps1` | Test VAT rapportage Q1 2026 |
| `test-audit-security.ps1` | Test audit logging |

---

## 📦 Seeding Details

De demo data wordt **automatisch** aangemaakt bij `dotnet run` in **Development** environment.

### DbSeeder (Basis)
- 3 test users (admin@local.test, accountant@local.test, viewer@local.test)
- 2 test tenants (Bedrijf A, Bedrijf B)
- Grootboekrekeningen per tenant
- Dagboeken per tenant
- 6 contacts per tenant

### DemoSeeder (Demo Scenario)
- 2 demo users (admin@demo.local, accountant@demo.local)
- 1 demo tenant (Demo Company BV)
- 3 demo contacts (Acme, TechStart, Office Supplies)
- 2 demo invoices (1 Posted+Paid, 1 Sent)
- Journal entries voor invoice + betaling
- Bank connection + 2 transactions (1 matched)
- Tenant branding
- Invoice template

**Locatie:** `backend/src/Infrastructure/Data/DemoSeeder.cs`

---

## 🔧 Troubleshooting

### Database verbinding mislukt
```powershell
# Check of PostgreSQL draait
docker ps

# Herstart PostgreSQL
cd infra
docker-compose down
docker-compose up -d
```

### Seeding draait niet
```powershell
# Check environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Herstart API
cd backend/src/Api
dotnet run
```

### API niet bereikbaar
```powershell
# Check poort 7195
netstat -ano | findstr :7195

# Check console output voor errors
```

### Test script faalt
```powershell
# Check of API draait
curl https://localhost:7195/health --insecure

# Check credentials
# admin@demo.local / Admin123!
```

---

## 🎯 Next Steps

1. **Frontend Demo Mode:** Voeg tenant selector toe + demo login button
2. **PDF Preview:** Test invoice PDF rendering via Playwright
3. **Production Seeding:** Verwijder demo seeder voor production builds
4. **API Documentation:** Swagger UI beschikbaar op https://localhost:7195/swagger

---

## 📚 Gerelateerde Documentatie

- [SALES_INVOICES_README.md](../SALES_INVOICES_README.md) - Invoice systeem details
- [BANK_INTEGRATION_README.md](../BANK_INTEGRATION_README.md) - Bank matching logic
- [VAT_REPORT_README.md](../VAT_REPORT_README.md) - VAT rapportage
- [AUDIT_SECURITY_README.md](AUDIT_SECURITY_README.md) - Audit logging
- [CONTACTS_README.md](CONTACTS_README.md) - Contact management

---

## ✅ Checklist: Demo Werkt Perfect Als...

- [ ] `docker-compose up -d` start PostgreSQL
- [ ] `dotnet run` toont demo seeding logs
- [ ] `test-demo-complete.ps1` passed alle 11 stappen
- [ ] Login met `admin@demo.local` werkt
- [ ] Demo Company BV verschijnt in tenants lijst
- [ ] 2 invoices zichtbaar (1 Paid, 1 Sent)
- [ ] VAT report toont €255 total VAT
- [ ] Bank transactions tonen 1 matched, 1 unmatched
- [ ] Audit logs tonen alle acties
- [ ] Swagger UI toont alle endpoints

---

**🎉 Demo Complete! Je hebt nu een volledig werkende MVP met realistische data.**

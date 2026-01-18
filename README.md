# Boekhouding SaaS - Multi-Tenant Accounting Platform

Complete accounting SaaS platform with multi-tenancy, bank integration, invoicing, contacts, VAT reporting, and audit logging.

##  Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ with npm
- SQL Server (LocalDB or full instance)
- PowerShell (for scripts)

### Backend Setup
\\\powershell
cd backend
dotnet restore
dotnet ef database update --project src/Infrastructure --startup-project src/Api
cd src/Api
dotnet run
\\\
Backend runs at: **http://localhost:5001**

### Frontend Setup
\\\powershell
cd frontend
npm install
npm run dev
\\\
Frontend runs at: **http://localhost:3000**

### Database Seed
Run after first database migration:
\\\powershell
cd backend
./test-accounting.ps1  # Seeds demo tenant, user, contacts, invoices
\\\

##  Demo Credentials
- **Email:** admin@demo.local
- **Password:** Admin123!
- **Tenant:** Demo Company BV

---

##  How to Verify the MVP Works

### Automated Testing (Recommended)

**One-Command Verification:**
```powershell
cd backend
.\run-all-tests.ps1 -CleanDb
```

This will:
1. Start Docker infrastructure (PostgreSQL)
2. Drop and recreate database
3. Run migrations
4. Build solution
5. Start API
6. Run smoke tests (11 steps)
7. Report results

**Expected Result:**
```
✅ MVP VERIFICATION COMPLETE - ALL TESTS PASSED!
Passed: 8/8 tests
Duration: ~45 seconds
```

**See:** `FASE_E_TEST_COMPLETE.md` for detailed test documentation.

### Manual Testing

**Quick Smoke Test:**
```powershell
# 1. Start infrastructure
cd infra
docker compose up -d

# 2. Run migrations
cd ../backend
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# 3. Start API
cd src/Api
dotnet run

# 4. Run smoke test (in separate terminal)
cd backend
.\test-mvp-complete.ps1
```

**Individual Feature Tests:**
- `.\test-phase-d-quick.ps1` - Dashboard + Reports (9 tests)
- `.\test-sales-invoices.ps1` - Invoice CRUD + posting
- `.\test-bank-integration.ps1` - Bank sync + matching
- `.\test-vat-report.ps1` - VAT reporting
- `.\test-auth.ps1` - Authentication flow

---

##  10-Step MVP Smoke Test

### 1. Login Flow
- Navigate to http://localhost:3000/login
- Enter demo credentials
- Click \"Login\"
- Verify redirect to dashboard
- Check tenant name in header (\"Demo Company BV\")

### 2. Dashboard Overview
- Verify stats cards show correct counts (invoices, contacts, revenue)
- Check quick action buttons work (Create Invoice, Add Contact, View Reports)

### 3. Contacts CRUD
- Go to Contacts page
- Click \"+ Create Contact\"
- Fill form (Name, Email, Type=Customer)
- Submit  verify appears in list
- Click contact name  edit page
- Update details  Save
- Use search bar  filter by name/email/phone/VAT
- Delete test contact

### 4. Invoice Creation
- Go to Invoices page
- Click \"+ Create Invoice\"
- Select contact from dropdown
- Add line items (description, quantity, unit price, VAT rate)
- Submit  verify appears in list with Draft status
- Use status filter (All/Draft/Sent/Posted/Paid)

### 5. Invoice Posting
- Click draft invoice number
- Click \"Post Invoice\"
- Verify status changes to Posted
- Verify invoice number updated (DEMO-2026-XXXX)

### 6. PDF Generation
- On posted invoice detail page
- Click \"Download PDF\"
- Verify PDF downloads with invoice details, logo, line items

### 7. Bank Integration
- Go to Banking  Connections
- Click \"Connect Bank Account\"
- Enter mock credentials (any values work)
- Click \"Sync Transactions\"
- Go to Banking  Transactions
- Verify transactions loaded

### 8. Transaction Matching
- On Transactions page, find unmatched transaction
- Click \"Match Invoice\" button
- Select posted invoice from dialog
- Verify status changes to Matched
- Click invoice number link  redirects to invoice detail
- Use filter: \"Matched Only\" / \"Unmatched Only\"

### 9. VAT Report
- Go to Reports  VAT Report
- Select date range (e.g., 2026-01-01 to 2026-12-31)
- Click \"Generate Report\"
- Verify breakdown shows Sales VAT, Purchase VAT, Total Payable

### 10. Audit Log
- Go to Audit Log
- Verify all actions logged (login, create invoice, post invoice, match transaction)
- Use action filter (All/Create/Update/Delete)
- Check timestamps, user, tenant, entity details

##  Architecture

### Backend (ASP.NET Core 8.0)
- **Domain Layer:** Entities, Value Objects, Events
- **Application Layer:** CQRS Commands/Queries, DTOs, Validators
- **Infrastructure Layer:** EF Core repositories, integrations
- **API Layer:** Controllers, JWT auth, multi-tenant middleware

### Frontend (Next.js 14 App Router)
- **App Router:** File-based routing with React Server Components
- **API Client:** Centralized typed client (lib/api.ts)
- **Contexts:** Auth, Tenant state management
- **Components:** ProtectedRoute, AppShell layout

##  API Endpoints

| Module | Method | Endpoint | Description |
|--------|--------|----------|-------------|
| **Auth** | POST | /api/auth/login | Login with email/password |
| **Tenants** | GET | /api/tenants | List all tenants for user |
| **Contacts** | GET | /api/contacts | List contacts (paginated) |
| | POST | /api/contacts | Create contact |
| | PUT | /api/contacts/{id} | Update contact |
| | DELETE | /api/contacts/{id} | Delete contact |
| **Invoices** | GET | /api/sales-invoices | List invoices (paginated) |
| | GET | /api/sales-invoices/{id} | Get invoice details |
| | POST | /api/sales-invoices | Create draft invoice |
| | POST | /api/sales-invoices/{id}/post | Post invoice |
| | GET | /api/sales-invoices/{id}/pdf | Generate PDF |
| **Banking** | GET | /api/bank-integration/connections | List bank connections |
| | POST | /api/bank-integration/connect | Connect bank account |
| | POST | /api/bank-integration/sync/{id} | Sync transactions |
| | GET | /api/bank-integration/transactions | List transactions |
| | POST | /api/bank-integration/match | Match transaction to invoice |
| **Reports** | GET | /api/reports/vat | VAT report by date range |
| **Audit** | GET | /api/audit-logs | Audit log (paginated, filterable) |
| **Branding** | GET/PUT | /api/branding | Tenant branding settings |
| **Templates** | GET | /api/invoice-templates | List templates |
| | GET | /api/invoice-templates/{id} | Get template |
| | PUT | /api/invoice-templates/{id} | Update template |
| | POST | /api/invoice-templates/{id}/set-default | Set default template |

##  Environment Variables

### Backend (ppsettings.json)
\\\json
{
  \"ConnectionStrings\": {
    \"DefaultConnection\": \"Server=(localdb)\\\\mssqllocaldb;Database=BoekhoudingSaas;Trusted_Connection=true\"
  },
  \"JwtSettings\": {
    \"Secret\": \"your-256-bit-secret-key-here-min-32-chars\",
    \"Issuer\": \"BoekhoudingAPI\",
    \"Audience\": \"BoekhoudingClient\",
    \"ExpiryMinutes\": 120
  }
}
\\\

### Frontend (.env.local)
\\\
NEXT_PUBLIC_API_URL=http://localhost:5001
\\\

##  Testing

### Backend Tests
\\\powershell
cd backend
./test-auth.ps1           # Auth endpoints
./test-contacts.ps1       # Contacts CRUD
./test-sales-invoices.ps1 # Invoice workflow
./test-bank-integration.ps1 # Banking + matching
./test-vat-report.ps1     # VAT calculations
./test-audit-security.ps1 # Audit logging
\\\

### Frontend Manual Testing
Use the 10-step smoke test above after 
pm run dev.

##  Modules

1. **Authentication:** JWT-based auth with role-based access
2. **Multi-Tenancy:** Tenant isolation at database level (X-Tenant-Id header)
3. **Contacts:** Customer/supplier management with type classification
4. **Sales Invoices:** Draft  Post  PDF workflow with line items
5. **Bank Integration:** Mock bank connection + transaction sync
6. **Transaction Matching:** Link bank transactions to posted invoices
7. **VAT Reporting:** Sales/purchase VAT breakdown by date range
8. **Audit Logging:** Full activity trail with user/tenant/entity tracking
9. **Branding:** Custom logo, colors, company info per tenant
10. **Templates:** Customizable HTML/CSS invoice templates

##  Current Status

 **Phase A:** Database schema, entities, migrations  
 **Phase B:** Backend API with all endpoints functional  
 **Phase C:** Frontend integration with 14 pages  

### Completed Features (Phase C)
-  Auth flow (login, token storage, protected routes)
-  Multi-tenant switcher (dropdown when multiple tenants)
-  Contacts CRUD + search (name/email/phone/VAT)
-  Invoices CRUD + status filter + PDF generation
-  Banking connections + transactions + match dialog
-  Transaction matching with invoice link display
-  VAT report with date range picker
-  Audit log with action filter
-  Branding settings page
-  Template editor (HTML/CSS with set default)

##  Documentation

- **Backend:** See ackend/README.md
- **Frontend:** See rontend/README.md
- **API Specs:** See individual module READMEs in ackend/
- **Quick Guides:** *_QUICKSTART.md files for each feature

##  Contributing

This is a demo project. For production use:
1. Replace mock bank integration with real provider
2. Add proper error handling and logging (e.g., Serilog)
3. Implement comprehensive unit/integration tests
4. Add CI/CD pipeline
5. Configure production environment variables
6. Set up monitoring (Application Insights, etc.)

##  License

MIT License - See LICENSE file

---

**Built with:** ASP.NET Core 8.0, Next.js 14, EF Core 8.0, SQL Server, JWT Authentication

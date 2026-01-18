# Phase C: Frontend Integration - Complete Analysis

**Created:** January 18, 2026  
**Status:** C1 Complete - Frontend Inventory + Contract Check

---

## 📋 C1: Frontend Inventory + Contract Check

### Current Frontend State

**Technology Stack:**
- Next.js 14.2.0 (App Router)
- React 18
- TypeScript 5
- No UI library (vanilla CSS)
- No state management library
- No API client library

**Dependencies:**
```json
{
  "dependencies": {
    "react": "^18",
    "react-dom": "^18",
    "next": "14.2.0"
  }
}
```

### Existing Frontend Pages

| Route | File | Status | Issues |
|-------|------|--------|--------|
| `/login` | `app/login/page.tsx` | ✅ Working | Hardcoded API URL, no error handling, wrong demo credentials |
| `/` | `app/page.tsx` | ⚠️ Partial | Dashboard stub, health check only |
| `/invoices` | `app/invoices/page.tsx` | ✅ Working | Direct fetch, no centralized client |
| `/invoices/new` | `app/invoices/new/page.tsx` | ✅ Working | Direct fetch, no centralized client |
| `/invoices/[id]` | `app/invoices/[id]/page.tsx` | ✅ Working | Direct fetch, post invoice works |
| `/banking/connections` | `app/banking/connections/page.tsx` | ⚠️ Broken | Hardcoded token/tenantId |
| `/banking/transactions` | `app/banking/transactions/page.tsx` | ⚠️ Broken | Hardcoded token/tenantId |
| `/settings/branding` | `app/settings/branding/page.tsx` | ✅ Working | Direct fetch |
| `/settings/templates` | `app/settings/templates/page.tsx` | ✅ Working | Direct fetch |

**Missing Pages:**
- ❌ Contacts list/CRUD
- ❌ Dashboard (proper)
- ❌ VAT Report
- ❌ Audit Log
- ❌ Tenant selector/switcher
- ❌ User profile/settings
- ❌ Journal entries view

### Current Authentication Flow

**Login (app/login/page.tsx):**
```typescript
// Issues:
1. Hardcoded credentials: admin@local.test (WRONG - should be admin@demo.local)
2. localStorage for token (security concern, but acceptable for MVP)
3. No Auth Context
4. No route protection
5. Tenant response handling incorrect (expects array, backend returns single object)
```

**Current login flow:**
```
1. POST /api/auth/login → {token, email, role, userId}
2. GET /api/tenants/my → {id, name, role} (single object, NOT array!)
3. Store in localStorage: token, email, role, tenantId, tenantName
4. Redirect to /
```

**Issues:**
- Line 47-51 in login/page.tsx treats response as array: `tenants[0]`
- Backend returns: `{ id, name, role }` not `[{ id, name, role }]`

### Current API Call Pattern

**Problem: 28+ direct fetch() calls scattered across components!**

**Pattern used (inconsistent):**
```typescript
const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";
const token = localStorage.getItem("token");
const tenantId = localStorage.getItem("tenantId");

const response = await fetch(`${API_URL}/api/salesinvoices`, {
  headers: {
    Authorization: `Bearer ${token}`,
    "X-Tenant-Id": tenantId,
  },
});
```

**Issues:**
- No centralized error handling
- No 401/403 handling
- No retry logic
- Repeated code
- Hard to test
- Hard to maintain

---

## 🔗 Backend API Contract

### Authentication Endpoints

| Method | Endpoint | Request | Response | Status |
|--------|----------|---------|----------|--------|
| POST | `/api/auth/login` | `{email, password}` | `{token, email, role, userId}` | ✅ Works |
| POST | `/api/auth/register` | `{email, password, role?}` | `{userId, email}` | ✅ Exists |

### Tenant Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/tenants/my` | - | `{id, name, role}` | **Single object!** |
| GET | `/api/tenants/{id}` | - | `{id, name, kvK, vatNumber...}` | ✅ Works |
| POST | `/api/tenants` | `{name, kvK?, vatNumber?}` | `{id...}` | ✅ Works |

**Frontend Issue:** login/page.tsx treats `/tenants/my` as array!

### Contacts Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/contacts` | `?page=1&pageSize=25` | `{items[], totalCount, page, pageSize}` | Paginated |
| GET | `/api/contacts/{id}` | - | `Contact` | ✅ Works |
| POST | `/api/contacts` | `CreateContactDto` | `Contact` | ✅ Works |
| PUT | `/api/contacts/{id}` | `UpdateContactDto` | `Contact` | ✅ Works |
| DELETE | `/api/contacts/{id}` | - | `204` | ✅ Works |

**Frontend Status:** ❌ No UI pages exist!

### Sales Invoices Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/salesinvoices` | - | `SalesInvoice[]` | **Array, not paginated!** |
| GET | `/api/salesinvoices/{id}` | - | `SalesInvoice` | ✅ Works |
| POST | `/api/salesinvoices` | `CreateSalesInvoiceDto` | `SalesInvoice` | ✅ Works |
| PUT | `/api/salesinvoices/{id}` | `UpdateSalesInvoiceDto` | `SalesInvoice` | ✅ Works |
| DELETE | `/api/salesinvoices/{id}` | - | `204` | ✅ Works |
| POST | `/api/salesinvoices/{id}/render-pdf` | - | `application/pdf` (blob) | ✅ Works |
| GET | `/api/salesinvoices/{id}/download-pdf` | - | `application/pdf` | ✅ Works |
| POST | `/api/salesinvoices/{id}/post` | - | `SalesInvoice` | ✅ Works (posts to journal) |

**Frontend Status:** ✅ All CRUD works, PDF download works, Post works

### Banking Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/bank/connections` | - | `BankConnection[]` | ✅ Works |
| POST | `/api/bank/connect` | `{provider, redirectUri?}` | `{authorizationUrl}` | Mock provider |
| POST | `/api/bank/sync/{connectionId}` | - | `{transactionsImported}` | ✅ Works |
| GET | `/api/bank/transactions` | `?connectionId=...` | `BankTransaction[]` | ✅ Works |
| POST | `/api/bank/transactions/{id}/match` | `{invoiceId}` | `BankTransaction` | ✅ Works |

**Frontend Status:** ⚠️ Pages exist but broken (hardcoded auth)

### Reports Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/reports/vat` | `?from=2026-01-01&to=2026-03-31` | `VatReportDto` | ✅ Fixed! |
| GET | `/api/reports/ar` | - | `ArReportDto` | ✅ Works |

**Frontend Status:** ❌ No UI pages!

### Audit Log Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/auditlogs` | `?skip=0&take=50&action=...` | `AuditLog[]` | ✅ Works |

**Frontend Status:** ❌ No UI pages!

### Settings Endpoints

| Method | Endpoint | Request | Response | Notes |
|--------|----------|---------|----------|-------|
| GET | `/api/tenantbranding` | - | `TenantBranding` | ✅ Works |
| PUT | `/api/tenantbranding` | `UpdateBrandingDto` | `TenantBranding` | ✅ Works |
| GET | `/api/invoicetemplates` | - | `InvoiceTemplate[]` | ✅ Works |
| GET | `/api/invoicetemplates/{id}` | - | `InvoiceTemplate` | ✅ Works |
| POST | `/api/invoicetemplates` | `CreateTemplateDto` | `InvoiceTemplate` | ✅ Works |
| PUT | `/api/invoicetemplates/{id}` | `UpdateTemplateDto` | `InvoiceTemplate` | ✅ Works |
| DELETE | `/api/invoicetemplates/{id}` | - | `204` | ✅ Works |

**Frontend Status:** ✅ Both pages work

---

## 🔀 DTO Shape Comparison

### ✅ Matches (no changes needed)

**SalesInvoice:**
```typescript
// Frontend (types/invoices.ts)
interface SalesInvoice {
  id: string;
  invoiceNumber: string;
  status: InvoiceStatus; // 0=Draft, 1=Sent, 2=Posted, 3=Paid
  issueDate: string;
  dueDate: string;
  contactId: string;
  contactName: string; // Backend includes this!
  currency: string;
  subtotal: number;
  vatTotal: number;
  total: number;
  lines: SalesInvoiceLine[];
  ...
}
```
✅ **Perfect match with backend DTOs!**

**TenantBranding:**
```typescript
interface TenantBranding {
  id: string;
  tenantId: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  fontFamily?: string;
  footerText?: string;
  ...
}
```
✅ **Perfect match!**

### ⚠️ Mismatches (need fixing)

**1. Tenant Response (CRITICAL):**
```typescript
// Frontend expects (WRONG):
GET /api/tenants/my → Array<{id, name, role}>

// Backend returns (CORRECT):
GET /api/tenants/my → {id, name, role}
```
**Fix:** Update login/page.tsx line 47-51

**2. Banking - Hardcoded Auth:**
```typescript
// Frontend (WRONG):
const token = 'your-token'; // Line 17
const tenantId = 'your-tenant-id'; // Line 18

// Should be:
const token = localStorage.getItem('token');
const tenantId = localStorage.getItem('tenantId');
```
**Fix:** Update banking/transactions/page.tsx + connections/page.tsx

**3. Demo Credentials:**
```typescript
// Frontend login/page.tsx (WRONG):
const [email, setEmail] = useState("admin@local.test");

// Should be (from DemoSeeder):
const [email, setEmail] = useState("admin@demo.local");
```
**Fix:** Update login/page.tsx line 8

---

## 📊 UI to API Mapping Table

| UI Screen | API Endpoints | DTOs | Status |
|-----------|---------------|------|--------|
| **Login** | POST /auth/login<br>GET /tenants/my | LoginRequest → LoginResponse<br>→ TenantSummary | ⚠️ Needs fixes |
| **Dashboard** | GET /health<br>GET /contacts (summary)<br>GET /salesinvoices (recent) | - | ❌ Stub only |
| **Contacts List** | GET /contacts?page=1&pageSize=25 | → PaginatedResponse\<Contact\> | ❌ Missing |
| **Contact Create/Edit** | POST /contacts<br>PUT /contacts/{id} | CreateContactDto<br>UpdateContactDto | ❌ Missing |
| **Invoices List** | GET /salesinvoices | → SalesInvoice[] | ✅ Works |
| **Invoice Create** | POST /salesinvoices<br>GET /contacts (for dropdown) | CreateSalesInvoiceDto | ✅ Works |
| **Invoice Detail** | GET /salesinvoices/{id}<br>POST /{id}/render-pdf<br>POST /{id}/post | SalesInvoice | ✅ Works |
| **Banking Connections** | GET /bank/connections<br>POST /bank/connect<br>POST /bank/sync/{id} | BankConnection[]<br>ConnectRequest | ⚠️ Broken auth |
| **Banking Transactions** | GET /bank/transactions<br>POST /transactions/{id}/match | BankTransaction[]<br>MatchRequest | ⚠️ Broken auth |
| **VAT Report** | GET /reports/vat?from=...&to=... | VatReportDto | ❌ Missing |
| **Audit Log** | GET /auditlogs?skip=0&take=50 | AuditLog[] | ❌ Missing |
| **Branding Settings** | GET /tenantbranding<br>PUT /tenantbranding | TenantBranding<br>UpdateBrandingDto | ✅ Works |
| **Templates Settings** | GET /invoicetemplates<br>POST/PUT/DELETE | InvoiceTemplate[] | ✅ Works |

---

## 🚨 Critical Issues Found

### 1. **No Centralized API Client** (CRITICAL)
- 28+ scattered `fetch()` calls
- Inconsistent error handling
- No 401 redirect
- Repeated token/tenantId logic

### 2. **No Auth Context** (CRITICAL)
- No global auth state
- No route protection
- Direct localStorage access everywhere

### 3. **No Tenant Context** (CRITICAL)
- No tenant switcher
- No multi-tenant support in UI
- tenantId buried in localStorage

### 4. **Wrong Demo Credentials** (BLOCKER)
- Frontend: `admin@local.test`
- Backend demo: `admin@demo.local`
- Will fail on fresh demo!

### 5. **Tenant Response Mismatch** (BLOCKER)
- Frontend treats `/tenants/my` as array
- Backend returns single object
- Causes `Cannot read property 'id' of undefined`

### 6. **Broken Banking Pages** (BLOCKER)
- Hardcoded `token = 'your-token'`
- Hardcoded `tenantId = 'your-tenant-id'`
- Will never work

### 7. **Missing Core Pages** (HIGH)
- No Contacts CRUD
- No VAT Report
- No Audit Log
- No proper Dashboard

---

## ✅ What Works Well

1. **Invoice CRUD**: Complete and working
2. **PDF Generation**: Works perfectly
3. **Post Invoice**: Works, updates status
4. **Settings (Branding/Templates)**: Both work
5. **TypeScript Types**: Well-defined in `types/`
6. **CSS Modules**: Consistent styling approach

---

## 📝 C1 Action Plan

### Immediate Fixes (Pre-C2)
1. ✅ Fix demo credentials: `admin@local.test` → `admin@demo.local`
2. ✅ Fix tenant response: Remove array access in login
3. ✅ Fix banking auth: Remove hardcoded tokens

### C2-C12 Roadmap
- **C2**: Auth Context + route protection
- **C3**: Tenant Context + selector
- **C4**: Central API client (`lib/api.ts`)
- **C5**: Navigation shell + layout
- **C6**: Contacts CRUD pages
- **C7**: Invoice improvements (filters, better UX)
- **C8**: Banking UI fixes + matching flow
- **C9**: VAT Report page
- **C10**: Audit Log page
- **C11**: Settings improvements
- **C12**: End-to-end smoke test

---

## 📋 Next Steps

**Ready for C2 Implementation:**
- [x] C1 Complete
- [ ] C2: Auth flow (login + context + protection)
- [ ] C3: Tenant selection + context
- [ ] C4: Central API client
- ... (continue with C5-C12)

**Estimated Effort:**
- C2-C4 (Foundation): ~2-3 hours
- C5-C11 (Features): ~4-5 hours
- C12 (Testing): ~1 hour
- **Total: ~8 hours of focused work**

---

*Analysis complete. Ready to proceed with fixes and implementation.*

# Audit Log & Security Hardening - Test Checklist

## Test Environment Setup

```powershell
# 1. Start database
cd C:\Users\Gslik\OneDrive\Documents\boekhouding-saas\infra
docker-compose up -d

# 2. Run migration
cd ..\backend
cat migrations\001_Add_AuditLog_Table.sql | docker exec -i boekhouding-postgres psql -U postgres -d boekhouding

# 3. Clean test data
echo "DELETE FROM \"AuditLogs\";" | docker exec -i boekhouding-postgres psql -U postgres -d boekhouding

# 4. Build and start API
dotnet build
dotnet run --project src/Api
```

## 1. Audit Log Tests

### 1.1 Tenant Creation Audit

**Test:** Verify tenant creation is logged

```powershell
# Create tenant
$registerResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    email = "audit-test-$(Get-Random)@example.com"
    password = "Test123!"
    tenantName = "Audit Test BV"
  } | ConvertTo-Json)

$token = $registerResponse.token
$tenantId = $registerResponse.tenantId
$userId = $registerResponse.userId

# Check audit log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs?entityType=Tenant" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Verify
$logs | Should -Not -BeNullOrEmpty
$logs[0].action | Should -Be "Create"
$logs[0].entityType | Should -Be "Tenant"
$logs[0].entityId | Should -Be $tenantId
$logs[0].actor.actorUserId | Should -Be $userId
```

**Expected Result:** ✓ Tenant creation logged with correct details

---

### 1.2 Invoice Template Audit

**Test:** Verify template create/update is logged

```powershell
# Create template
$template = Invoke-RestMethod -Uri "http://localhost:5000/api/invoicetemplates" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    name = "Test Template"
    templateContent = "<html>Test</html>"
    isDefault = $false
  } | ConvertTo-Json)

# Check audit log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs/entity/InvoiceTemplate/$($template.id)" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Verify create log
$createLog = $logs | Where-Object { $_.action -eq "Create" }
$createLog | Should -Not -BeNullOrEmpty
$createLog.diffJson | Should -Not -BeNullOrEmpty

# Update template
Invoke-RestMethod -Uri "http://localhost:5000/api/invoicetemplates/$($template.id)" `
  -Method Put `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    name = "Updated Template"
    templateContent = "<html>Updated</html>"
    isDefault = $true
  } | ConvertTo-Json)

# Check update audit log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs/entity/InvoiceTemplate/$($template.id)" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$updateLog = $logs | Where-Object { $_.action -eq "Update" }
$updateLog | Should -Not -BeNullOrEmpty
$diff = $updateLog.diffJson | ConvertFrom-Json
$diff.before.name | Should -Be "Test Template"
$diff.after.name | Should -Be "Updated Template"
```

**Expected Result:** ✓ Template create and update logged with before/after state

---

### 1.3 Tenant Branding Audit

**Test:** Verify branding changes are logged

```powershell
# Create branding
$branding = Invoke-RestMethod -Uri "http://localhost:5000/api/tenantbranding" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    companyName = "Test Company"
    address = "Test Street 1"
    city = "Amsterdam"
    postalCode = "1000AA"
    country = "Netherlands"
    email = "info@test.com"
  } | ConvertTo-Json)

# Check audit log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs/entity/TenantBranding/$($branding.id)" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$logs[0].action | Should -Be "Create"
$logs[0].diffJson | Should -Contain "Test Company"
```

**Expected Result:** ✓ Branding creation logged

---

### 1.4 Sales Invoice Post Audit

**Test:** Verify invoice posting is logged

```powershell
# Create customer
$customer = Invoke-RestMethod -Uri "http://localhost:5000/api/contacts" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    type = 1
    name = "Test Customer"
    email = "customer@test.com"
  } | ConvertTo-Json)

# Create invoice
$invoice = Invoke-RestMethod -Uri "http://localhost:5000/api/salesinvoices" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    customerId = $customer.id
    invoiceDate = (Get-Date).ToString("yyyy-MM-dd")
    dueDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
    lines = @(
      @{
        description = "Test Product"
        quantity = 1
        unitPrice = 100.00
        vatRate = 21
      }
    )
  } | ConvertTo-Json -Depth 10)

# Post invoice
Invoke-RestMethod -Uri "http://localhost:5000/api/salesinvoices/$($invoice.id)/post" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Check audit log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs/entity/SalesInvoice/$($invoice.id)" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$postLog = $logs | Where-Object { $_.action -eq "Post" }
$postLog | Should -Not -BeNullOrEmpty
$diff = $postLog.diffJson | ConvertFrom-Json
$diff.before.status | Should -Be "Draft"
$diff.after.status | Should -Be "Posted"
```

**Expected Result:** ✓ Invoice posting logged with status change

---

### 1.5 Journal Entry Post/Reverse Audit

**Test:** Verify journal entry post and reverse are logged

```powershell
# Create journal entry
$journalEntry = Invoke-RestMethod -Uri "http://localhost:5000/api/journalentries" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    date = (Get-Date).ToString("yyyy-MM-dd")
    description = "Test Entry"
    lines = @(
      @{ accountId = (Get debit account ID); debit = 100; credit = 0 }
      @{ accountId = (Get credit account ID); debit = 0; credit = 100 }
    )
  } | ConvertTo-Json -Depth 10)

# Post journal entry
Invoke-RestMethod -Uri "http://localhost:5000/api/journalentries/$($journalEntry.id)/post" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Check post log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs/entity/JournalEntry/$($journalEntry.id)" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$postLog = $logs | Where-Object { $_.action -eq "Post" }
$postLog | Should -Not -BeNullOrEmpty

# Reverse journal entry
Invoke-RestMethod -Uri "http://localhost:5000/api/journalentries/$($journalEntry.id)/reverse" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Check reverse log
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs?entityType=JournalEntry" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$reverseLog = $logs | Where-Object { $_.action -eq "Reverse" }
$reverseLog | Should -Not -BeNullOrEmpty
```

**Expected Result:** ✓ Both post and reverse logged

---

### 1.6 Bank Integration Audit

**Test:** Verify bank sync and matching are logged

```powershell
# Create bank connection
$connection = Invoke-RestMethod -Uri "http://localhost:5000/api/bank/connect" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    bankName = "Test Bank"
    accountNumber = "NL12TEST3456789012"
  } | ConvertTo-Json)

# Sync transactions
Invoke-RestMethod -Uri "http://localhost:5000/api/bank/connections/$($connection.id)/sync" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Check sync logs
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs?entityType=BankConnection&action=Sync" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$logs | Should -Not -BeNullOrEmpty
$logs[0].action | Should -Be "Sync"
```

**Expected Result:** ✓ Bank sync logged

---

### 1.7 IP Address & User Agent Capture

**Test:** Verify IP and User Agent are captured

```powershell
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs?take=1" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "User-Agent" = "TestClient/1.0"
  }

$logs[0].ipAddress | Should -Not -BeNullOrEmpty
# User agent capture depends on implementation
```

**Expected Result:** ✓ IP address captured

---

## 2. Rate Limiting Tests

### 2.1 Login Rate Limit

**Test:** Verify login endpoint is rate limited

```powershell
# Attempt 6 logins in quick succession
$results = @()
for ($i = 1; $i -le 6; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
          -Method Post `
          -ContentType "application/json" `
          -Body (@{
            email = "nonexistent@example.com"
            password = "WrongPassword"
          } | ConvertTo-Json) `
          -ErrorAction Stop
        $results += $response.StatusCode
    } catch {
        $results += $_.Exception.Response.StatusCode.Value__
    }
    Start-Sleep -Milliseconds 100
}

# First 5 should be 401 (Unauthorized), 6th should be 429 (Too Many Requests)
$results[5] | Should -Be 429
```

**Expected Result:** ✓ 6th request returns 429 Too Many Requests

---

### 2.2 Register Rate Limit

**Test:** Verify register endpoint is rate limited

```powershell
$results = @()
for ($i = 1; $i -le 6; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/register" `
          -Method Post `
          -ContentType "application/json" `
          -Body (@{
            email = "test$i@example.com"
            password = "Test123!"
            tenantName = "Test$i"
          } | ConvertTo-Json) `
          -ErrorAction Stop
        $results += $response.StatusCode
    } catch {
        $results += $_.Exception.Response.StatusCode.Value__
    }
}

$results[5] | Should -Be 429
```

**Expected Result:** ✓ Rate limit enforced on registration

---

### 2.3 Rate Limit Reset

**Test:** Verify rate limit resets after time window

```powershell
# Trigger rate limit
for ($i = 1; $i -le 5; $i++) {
    try {
        Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
          -Method Post `
          -ContentType "application/json" `
          -Body (@{ email = "test@example.com"; password = "wrong" } | ConvertTo-Json)
    } catch { }
}

# Wait for window to expire (61 seconds)
Start-Sleep -Seconds 61

# Should succeed (or return 401, not 429)
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
      -Method Post `
      -ContentType "application/json" `
      -Body (@{ email = "test@example.com"; password = "wrong" } | ConvertTo-Json)
    $statusCode = $response.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Not -Be 429
```

**Expected Result:** ✓ Rate limit resets after time window

---

## 3. CORS Tests

### 3.1 Valid Origin

**Test:** Verify allowed origin is accepted

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
  -Method Options `
  -Headers @{
    "Origin" = "http://localhost:3000"
    "Access-Control-Request-Method" = "POST"
  }

$response.Headers["Access-Control-Allow-Origin"] | Should -Be "http://localhost:3000"
```

**Expected Result:** ✓ Allowed origin accepted

---

### 3.2 Invalid Origin

**Test:** Verify unauthorized origin is blocked

```powershell
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
      -Method Post `
      -Headers @{
        "Origin" = "http://evil-site.com"
      } `
      -ContentType "application/json" `
      -Body (@{ email = "test@example.com"; password = "test" } | ConvertTo-Json)
    $statusCode = $response.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Be 403
```

**Expected Result:** ✓ Unauthorized origin blocked with 403

---

## 4. Tenant Isolation Tests

### 4.1 Missing X-Tenant-Id Header

**Test:** Verify authenticated requests require X-Tenant-Id

```powershell
# Login to get token
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    email = "existing@example.com"
    password = "Test123!"
  } | ConvertTo-Json)

# Try to access resource without X-Tenant-Id header
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/contacts" `
      -Method Get `
      -Headers @{
        "Authorization" = "Bearer $($loginResponse.token)"
      }
    $statusCode = $response.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Be 400
```

**Expected Result:** ✓ Request blocked with 400 Bad Request

---

### 4.2 Tenant Mismatch

**Test:** Verify X-Tenant-Id must match JWT claim

```powershell
# Login to tenant A
$userA = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    email = "userA@example.com"
    password = "Test123!"
    tenantName = "Tenant A"
  } | ConvertTo-Json)

# Login to tenant B
$userB = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    email = "userB@example.com"
    password = "Test123!"
    tenantName = "Tenant B"
  } | ConvertTo-Json)

# Try to access tenant B with tenant A's token
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/contacts" `
      -Method Get `
      -Headers @{
        "Authorization" = "Bearer $($userA.token)"
        "X-Tenant-Id" = $userB.tenantId
      }
    $statusCode = $response.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Be 403
```

**Expected Result:** ✓ Cross-tenant access blocked with 403

---

### 4.3 Tenant Data Isolation

**Test:** Verify users can only see their own tenant's data

```powershell
# Create data in tenant A
$contactA = Invoke-RestMethod -Uri "http://localhost:5000/api/contacts" `
  -Method Post `
  -Headers @{
    "Authorization" = "Bearer $($userA.token)"
    "X-Tenant-Id" = $userA.tenantId
  } `
  -ContentType "application/json" `
  -Body (@{
    type = 1
    name = "Customer A"
    email = "customerA@test.com"
  } | ConvertTo-Json)

# Try to access from tenant B
$contactsB = Invoke-RestMethod -Uri "http://localhost:5000/api/contacts" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $($userB.token)"
    "X-Tenant-Id" = $userB.tenantId
  }

# Tenant B should not see tenant A's contact
$contactsB.id -contains $contactA.id | Should -Be $false
```

**Expected Result:** ✓ Tenant B cannot see tenant A's data

---

## 5. Content-Type Validation Tests

### 5.1 Valid Content-Type

**Test:** Verify application/json is accepted

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    email = "test@example.com"
    password = "Test123!"
    tenantName = "Test"
  } | ConvertTo-Json)

$response.StatusCode | Should -Be 200
```

**Expected Result:** ✓ JSON content accepted

---

### 5.2 Invalid Content-Type

**Test:** Verify invalid content-type is rejected

```powershell
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/register" `
      -Method Post `
      -ContentType "text/plain" `
      -Body "email=test@example.com&password=Test123!"
    $statusCode = $response.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Be 415
```

**Expected Result:** ✓ Invalid content-type rejected with 415

---

## 6. Authorization Tests

### 6.1 Audit Logs - Accountant Access

**Test:** Verify accountants can access audit logs

```powershell
# Create accountant user (requires admin or update seed data)
# Then test access
$logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs" `
  -Method Get `
  -Headers @{
    "Authorization" = "Bearer $accountantToken"
    "X-Tenant-Id" = $tenantId
  }

$logs | Should -Not -BeNullOrEmpty
```

**Expected Result:** ✓ Accountants can view audit logs

---

### 6.2 Audit Logs - Viewer Denied

**Test:** Verify viewers cannot access audit logs

```powershell
try {
    $logs = Invoke-WebRequest -Uri "http://localhost:5000/api/auditlogs" `
      -Method Get `
      -Headers @{
        "Authorization" = "Bearer $viewerToken"
        "X-Tenant-Id" = $tenantId
      }
    $statusCode = $logs.StatusCode
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
}

$statusCode | Should -Be 403
```

**Expected Result:** ✓ Viewers denied access with 403

---

## Test Summary Checklist

### Audit Log Functionality
- [ ] Tenant creation logged
- [ ] Template create/update logged
- [ ] Branding changes logged
- [ ] Invoice posting logged
- [ ] Invoice payment logged
- [ ] Journal entry post logged
- [ ] Journal entry reverse logged
- [ ] Bank connection logged
- [ ] Bank sync logged
- [ ] Transaction matching logged
- [ ] IP address captured
- [ ] User agent captured
- [ ] DiffJson contains before/after state

### Rate Limiting
- [ ] Login endpoint rate limited (5 req/min)
- [ ] Register endpoint rate limited
- [ ] Refresh endpoint rate limited
- [ ] Rate limit resets after time window
- [ ] Returns HTTP 429 with error message
- [ ] X-Forwarded-For header respected

### CORS Security
- [ ] Allowed origins accepted
- [ ] Unauthorized origins blocked
- [ ] Credentials allowed for allowed origins
- [ ] X-Tenant-Id header exposed
- [ ] Preflight requests handled

### Tenant Isolation
- [ ] Missing X-Tenant-Id header blocked
- [ ] Tenant ID mismatch blocked
- [ ] Cross-tenant data access prevented
- [ ] Global query filters active
- [ ] Audit logs scoped to tenant

### Content Validation
- [ ] application/json accepted
- [ ] multipart/form-data accepted
- [ ] Invalid content-types rejected
- [ ] Empty body validation

### Authorization
- [ ] Accountants can view audit logs
- [ ] Admins can view audit logs
- [ ] Viewers cannot view audit logs
- [ ] Policy enforcement working

## Performance Tests

### Audit Log Performance
```powershell
# Create 100 audit logs and measure query time
Measure-Command {
    $logs = Invoke-RestMethod -Uri "http://localhost:5000/api/auditlogs?take=100" `
      -Method Get `
      -Headers @{
        "Authorization" = "Bearer $token"
        "X-Tenant-Id" = $tenantId
      }
}
```

**Expected:** < 500ms for 100 records

### Rate Limiting Performance
```powershell
# Measure overhead of rate limiting middleware
Measure-Command {
    for ($i = 1; $i -le 4; $i++) {
        Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
          -Method Post `
          -ContentType "application/json" `
          -Body (@{ email = "test@test.com"; password = "wrong" } | ConvertTo-Json)
    }
}
```

**Expected:** < 50ms overhead per request

---

## Cleanup

```powershell
# Stop API (Ctrl+C)

# Clean test data
echo "DELETE FROM \"AuditLogs\"; DELETE FROM \"SalesInvoiceLines\"; DELETE FROM \"SalesInvoices\"; DELETE FROM \"Contacts\" WHERE \"Type\" = 1;" | docker exec -i boekhouding-postgres psql -U postgres -d boekhouding

# Stop database
cd ..\infra
docker-compose down
```

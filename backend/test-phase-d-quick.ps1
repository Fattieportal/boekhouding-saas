# FASE D - Quick Smoke Test
# Tests Dashboard, Filtering, Payments, Reports

Write-Host "===============================" -ForegroundColor Cyan
Write-Host "FASE D: MVP GLUE QUICK TEST" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:5001/api"
$passed = 0
$failed = 0

# Login
Write-Host "Login..." -ForegroundColor Yellow
try {
    $loginBody = @{ email = "admin@demo.local"; password = "Admin123!" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "  [OK] Logged in" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "  [FAIL] Login failed: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
    exit 1
}

# Get Tenant
$authHeaders = @{ "Authorization" = "Bearer $token" }
try {
    $tenant = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Method Get -Headers $authHeaders
    Write-Host "  [OK] Got tenant: $($tenant.name)" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "  [FAIL] Get tenant failed" -ForegroundColor Red
    $failed++
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenant.id
}

# Test Dashboard
Write-Host ""
Write-Host "TEST: Dashboard Endpoint" -ForegroundColor Yellow
try {
    $dashboard = Invoke-RestMethod -Uri "$baseUrl/dashboard?from=2026-01-01&to=2026-01-31" -Method Get -Headers $headers
    if ($dashboard.invoices -and $dashboard.revenue -and $dashboard.bank) {
        Write-Host "  [OK] Dashboard works" -ForegroundColor Green
        Write-Host "       Unpaid: $($dashboard.invoices.unpaidCount)" -ForegroundColor Gray
        Write-Host "       Overdue: $($dashboard.invoices.overdueCount)" -ForegroundColor Gray
        $passed++
    } else {
        Write-Host "  [FAIL] Dashboard missing fields" -ForegroundColor Red
        $failed++
    }
} catch {
    Write-Host "  [FAIL] Dashboard: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Test Invoice Filtering
Write-Host ""
Write-Host "TEST: Invoice Filtering" -ForegroundColor Yellow
try {
    $invoices = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Get -Headers $headers
    Write-Host "  [OK] Got $($invoices.Count) invoices" -ForegroundColor Green
    $passed++
    
    $posted = Invoke-RestMethod -Uri "$baseUrl/salesinvoices?status=2" -Method Get -Headers $headers
    Write-Host "  [OK] Got $($posted.Count) posted invoices" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "  [FAIL] Filtering: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Test Invoice Detail with Payments
Write-Host ""
Write-Host "TEST: Invoice Detail (Payments)" -ForegroundColor Yellow
try {
    if ($invoices.Count -gt 0) {
        $detail = Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$($invoices[0].id)" -Method Get -Headers $headers
        $hasOpenAmount = $detail.PSObject.Properties.Name -contains 'openAmount'
        $hasPayments = $detail.PSObject.Properties.Name -contains 'payments'
        if ($hasOpenAmount -and $hasPayments) {
            Write-Host "  [OK] Invoice has openAmount and payments array" -ForegroundColor Green
            Write-Host "       OpenAmount: $($detail.openAmount)" -ForegroundColor Gray
            Write-Host "       Payments: $($detail.payments.Count)" -ForegroundColor Gray
            $passed++
        } else {
            Write-Host "  [FAIL] Missing openAmount or payments" -ForegroundColor Red
            Write-Host "       Has OpenAmount: $hasOpenAmount" -ForegroundColor Gray
            Write-Host "       Has Payments: $hasPayments" -ForegroundColor Gray
            $failed++
        }
    } else {
        Write-Host "  [SKIP] No invoices to test" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [FAIL] Invoice detail: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Test Bank Transactions
Write-Host ""
Write-Host "TEST: Bank Transactions (Deep Links)" -ForegroundColor Yellow
try {
    $txs = Invoke-RestMethod -Uri "$baseUrl/bank/transactions" -Method Get -Headers $headers
    Write-Host "  [OK] Got $($txs.Count) transactions" -ForegroundColor Green
    $passed++
    
    $matched = $txs | Where-Object { $_.matchedStatus -eq 1 }
    if ($matched) {
        Write-Host "       Matched: $($matched.Count)" -ForegroundColor Gray
        if ($matched[0].invoiceNumber) {
            Write-Host "       Invoice #: $($matched[0].invoiceNumber)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "  [FAIL] Bank transactions: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Test P&L Report
Write-Host ""
Write-Host "TEST: Profit & Loss Report" -ForegroundColor Yellow
try {
    $pl = Invoke-RestMethod -Uri "$baseUrl/reports/profit-loss?from=2026-01-01&to=2026-01-31" -Method Get -Headers $headers
    if ($pl.totalRevenue -ne $null -and $pl.totalExpenses -ne $null) {
        Write-Host "  [OK] P&L Report works" -ForegroundColor Green
        Write-Host "       Revenue: $($pl.totalRevenue)" -ForegroundColor Gray
        Write-Host "       Expenses: $($pl.totalExpenses)" -ForegroundColor Gray
        Write-Host "       Net Income: $($pl.netIncome)" -ForegroundColor Gray
        $passed++
    } else {
        Write-Host "  [FAIL] P&L missing fields" -ForegroundColor Red
        $failed++
    }
} catch {
    Write-Host "  [FAIL] P&L: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Test Balance Sheet
Write-Host ""
Write-Host "TEST: Balance Sheet" -ForegroundColor Yellow
try {
    $bs = Invoke-RestMethod -Uri "$baseUrl/reports/balance-sheet" -Method Get -Headers $headers
    if ($bs.totalAssets -ne $null -and $bs.totalLiabilities -ne $null) {
        Write-Host "  [OK] Balance Sheet works" -ForegroundColor Green
        Write-Host "       Assets: $($bs.totalAssets)" -ForegroundColor Gray
        Write-Host "       Liabilities: $($bs.totalLiabilities)" -ForegroundColor Gray
        Write-Host "       Equity: $($bs.totalEquity)" -ForegroundColor Gray
        $passed++
    } else {
        Write-Host "  [FAIL] Balance Sheet missing fields" -ForegroundColor Red
        $failed++
    }
} catch {
    Write-Host "  [FAIL] Balance Sheet: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Summary
Write-Host ""
Write-Host "===============================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failed -eq 0) {
    Write-Host "[SUCCESS] All Fase D tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAILED] Some tests failed" -ForegroundColor Red
    exit 1
}

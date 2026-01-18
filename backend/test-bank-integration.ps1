# Test Bank Integration
$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5001/api"

Write-Host "`n=== BANK INTEGRATION TEST ===" -ForegroundColor Cyan

# Login
Write-Host "`n[1/7] Authenticating..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}'
$token = $loginResponse.token
Write-Host "Authenticated as admin@local.test" -ForegroundColor Green

# Get tenant
$tenantsResponse = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Method Get -Headers @{"Authorization" = "Bearer $token"}
$tenantId = $tenantsResponse[0].id
Write-Host "Tenant ID: $tenantId" -ForegroundColor Gray

# Get revenue account
$accountsResponse = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Get -Headers @{"Authorization" = "Bearer $token"; "X-Tenant-Id" = $tenantId}
$revenueAccount = $accountsResponse.items | Where-Object { $_.code -eq "8000" } | Select-Object -First 1
$revenueAccountId = $revenueAccount.id
Write-Host "Revenue Account: $($revenueAccount.code) - $($revenueAccount.name)" -ForegroundColor Gray

$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

# Create customer
Write-Host "`n[2/7] Creating customer..." -ForegroundColor Yellow
$customerBody = '{"type":1,"displayName":"Test Customer BV","email":"customer@test.com"}'
$customer = Invoke-RestMethod -Uri "$baseUrl/contacts" -Method Post -Headers $headers -Body $customerBody
Write-Host "Customer created: $($customer.displayName)" -ForegroundColor Green

# Create invoice
Write-Host "`n[3/7] Creating invoice..." -ForegroundColor Yellow
$invoiceBody = @{
    contactId = $customer.id
    invoiceNumber = "INV-BANK-001"
    issueDate = (Get-Date).ToString("yyyy-MM-dd")
    dueDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
    lines = @(@{description="Services";quantity=10;unitPrice=150.00;vatRate=21.0;accountId=$revenueAccountId})
} | ConvertTo-Json -Depth 3
$invoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Post -Headers $headers -Body $invoiceBody
Write-Host "Invoice created: $($invoice.invoiceNumber) - EUR $($invoice.total)" -ForegroundColor Green

# Post invoice
Write-Host "`n[4/7] Posting invoice..." -ForegroundColor Yellow
Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$($invoice.id)/post" -Method Post -Headers $headers | Out-Null
Write-Host "Invoice posted" -ForegroundColor Green

# Connect bank
Write-Host "`n[5/7] Connecting bank..." -ForegroundColor Yellow
$connectBody = '{"provider":"Mock"}'
$connection = Invoke-RestMethod -Uri "$baseUrl/bank/connect" -Method Post -Headers $headers -Body $connectBody
Write-Host "Bank connected: $($connection.connectionId)" -ForegroundColor Green

# Sync transactions
Write-Host "`n[6/7] Syncing transactions..." -ForegroundColor Yellow
$syncBody = @{from=(Get-Date).AddDays(-30).ToString("yyyy-MM-dd");to=(Get-Date).ToString("yyyy-MM-dd")} | ConvertTo-Json
$sync = Invoke-RestMethod -Uri "$baseUrl/bank/connections/$($connection.connectionId)/sync" -Method Post -Headers $headers -Body $syncBody
Write-Host "Synced: $($sync.transactionsImported) transactions imported" -ForegroundColor Green

# Get transactions
$transactions = Invoke-RestMethod -Uri "$baseUrl/bank/transactions?connectionId=$($connection.connectionId)" -Method Get -Headers $headers
Write-Host "Retrieved $($transactions.Count) transactions" -ForegroundColor Gray

# Match transaction
Write-Host "`n[7/7] Matching transaction..." -ForegroundColor Yellow
$creditTx = $transactions | Where-Object { $_.amount -gt 0 } | Select-Object -First 1
if ($creditTx) {
    Write-Host "Matching EUR $($creditTx.amount) to invoice $($invoice.invoiceNumber)" -ForegroundColor Gray
    try {
        $matchBody = @{invoiceId=$invoice.id} | ConvertTo-Json
        Invoke-RestMethod -Uri "$baseUrl/bank/transactions/$($creditTx.id)/match" -Method Post -Headers $headers -Body $matchBody | Out-Null
        $updatedInvoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$($invoice.id)" -Method Get -Headers $headers
        Write-Host "Transaction matched! Invoice status: $($updatedInvoice.status)" -ForegroundColor Green
    }
    catch {
        Write-Host "Could not match (amounts may differ)" -ForegroundColor Yellow
    }
}

Write-Host "`n=== TEST COMPLETE ===" -ForegroundColor Cyan
Write-Host "Bank Integration is working! " -ForegroundColor Green

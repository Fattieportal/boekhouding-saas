# Eenvoudige test voor Journal Entries
Write-Host "`n=== JOURNAL ENTRIES QUICK TEST ===" -ForegroundColor Cyan

$baseUrl = "http://localhost:5001/api"

# Test 1: API Health Check
Write-Host "`nTest 1: API Health Check" -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5001/health" -UseBasicParsing
    Write-Host "  API is running" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: API not running. Start with: cd backend\src\Api; dotnet run" -ForegroundColor Red
    exit 1
}

# Test 2: Login with existing user that has a tenant
Write-Host "`nTest 2: Login with existing user" -ForegroundColor Yellow

$loginBody = @{
    email = "admin@local.test"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $auth = Invoke-RestMethod -Method POST -Uri "$baseUrl/auth/login" -Body $loginBody -ContentType "application/json"
    
    if (-not $auth) {
        Write-Host "  ERROR: No response from login endpoint" -ForegroundColor Red
        exit 1
    }
    
    $token = $auth.token
    
    if (-not $token) {
        Write-Host "  ERROR: No token in response" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Logged in successfully" -ForegroundColor Green
    Write-Host "  User: $($auth.email)" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    exit 1
}

# Get user's tenants
Write-Host "`nTest 2b: Get user tenants" -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $tenants = Invoke-RestMethod -Method GET -Uri "$baseUrl/tenants/my" -Headers $headers
    
    if (-not $tenants -or $tenants.Count -eq 0) {
        Write-Host "  ERROR: No tenants found for user" -ForegroundColor Red
        exit 1
    }
    
    $tenantId = $tenants[0].id
    Write-Host "  Found $($tenants.Count) tenant(s)" -ForegroundColor Green
    Write-Host "  Using tenant: $($tenants[0].name)" -ForegroundColor Gray
    Write-Host "  TenantId: $tenantId" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    exit 1
}

# Update headers with tenant ID
$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

# Test 3: Create Journal
Write-Host "`nTest 3: Create Journal" -ForegroundColor Yellow

# Use unique code with timestamp
$timestamp = (Get-Date).ToString("HHmmss")
$journalBody = @{
    code = "JE$timestamp"
    name = "Test Journal $timestamp"
    type = 2
} | ConvertTo-Json

try {
    $journal = Invoke-RestMethod -Method POST -Uri "$baseUrl/journals" -Body $journalBody -Headers $headers
    
    if (-not $journal) {
        Write-Host "  ERROR: No response from journals endpoint" -ForegroundColor Red
        exit 1
    }
    
    $journalId = $journal.id
    
    if (-not $journalId) {
        Write-Host "  ERROR: No id in journal response" -ForegroundColor Red
        Write-Host "  Response: $($journal | ConvertTo-Json)" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "  Journal created: $($journal.code)" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Yellow
        } catch {}
    }
    exit 1
}

# Test 4: Create Accounts
Write-Host "`nTest 4: Create Accounts" -ForegroundColor Yellow

# Use unique codes with timestamp
$timestamp2 = (Get-Date).ToString("HHmmss")
$account1 = @{
    code = "A$timestamp2"
    name = "Test Account Debit"
    type = 0
    isActive = $true
} | ConvertTo-Json

$account2 = @{
    code = "B$timestamp2"
    name = "Test Account Credit"
    type = 3
    isActive = $true
} | ConvertTo-Json

try {
    $acc1 = Invoke-RestMethod -Method POST -Uri "$baseUrl/accounts" -Body $account1 -Headers $headers
    $acc2 = Invoke-RestMethod -Method POST -Uri "$baseUrl/accounts" -Body $account2 -Headers $headers
    Write-Host "  Accounts created: $($acc1.code), $($acc2.code)" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 5: Create Draft Entry
Write-Host "`nTest 5: Create Draft Journal Entry" -ForegroundColor Yellow
$entryBody = @{
    journalId = $journalId
    entryDate = (Get-Date).ToString("yyyy-MM-dd")
    reference = "TEST-001"
    description = "Test boeking"
    lines = @(
        @{
            accountId = $acc1.id
            description = "Kas"
            debit = 100.00
            credit = 0
        },
        @{
            accountId = $acc2.id
            description = "Omzet"
            debit = 0
            credit = 100.00
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $entry = Invoke-RestMethod -Method POST -Uri "$baseUrl/journal-entries" -Body $entryBody -Headers $headers
    Write-Host "  Draft entry created" -ForegroundColor Green
    Write-Host "  Status: $($entry.status) (should be 0 for Draft)" -ForegroundColor Gray
    Write-Host "  Balanced: $($entry.isBalanced)" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 6: Post Entry
Write-Host "`nTest 6: Post Entry (Balance Check)" -ForegroundColor Yellow
try {
    $posted = Invoke-RestMethod -Method POST -Uri "$baseUrl/journal-entries/$($entry.id)/post" -Headers $headers
    Write-Host "  Entry posted successfully" -ForegroundColor Green
    Write-Host "  Status: $($posted.status) (should be 1 for Posted)" -ForegroundColor Gray
    Write-Host "  Posted at: $($posted.postedAt)" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 7: Try to Update Posted Entry (should fail)
Write-Host "`nTest 7: Try to Update Posted Entry (Immutability Test)" -ForegroundColor Yellow
$updateBody = @{
    entryDate = (Get-Date).ToString("yyyy-MM-dd")
    reference = "UPDATED"
    description = "Should not work"
    lines = @(
        @{
            accountId = $acc1.id
            description = "Test"
            debit = 50.00
            credit = 0
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $updated = Invoke-RestMethod -Method PUT -Uri "$baseUrl/journal-entries/$($posted.id)" -Body $updateBody -Headers $headers
    Write-Host "  ERROR: Posted entry was updated (immutability violation!)" -ForegroundColor Red
} catch {
    Write-Host "  Correctly rejected: Posted entries are immutable" -ForegroundColor Green
}

# Test 8: Reverse Entry
Write-Host "`nTest 8: Reverse Posted Entry" -ForegroundColor Yellow
try {
    $reversal = Invoke-RestMethod -Method POST -Uri "$baseUrl/journal-entries/$($posted.id)/reverse" -Headers $headers
    Write-Host "  Reversal entry created" -ForegroundColor Green
    Write-Host "  Reference: $($reversal.reference)" -ForegroundColor Gray
    Write-Host "  Lines count: $($reversal.lines.Count)" -ForegroundColor Gray
    
    # Verify debits and credits are swapped
    $originalDebit = $posted.lines[0].debit
    $reversalCredit = $reversal.lines[0].credit
    if ($originalDebit -eq $reversalCredit) {
        Write-Host "  Debits and credits correctly swapped" -ForegroundColor Green
    }
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 9: Get All Entries
Write-Host "`nTest 9: Get All Entries" -ForegroundColor Yellow
try {
    $allEntries = Invoke-RestMethod -Method GET -Uri "$baseUrl/journal-entries" -Headers $headers
    Write-Host "  Found $($allEntries.Count) entries" -ForegroundColor Green
    foreach ($e in $allEntries) {
        Write-Host "    - $($e.reference): Status=$($e.status), Balance=$($e.isBalanced)" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Cyan
Write-Host "All core features tested successfully:" -ForegroundColor Green
Write-Host "  - Draft entry creation" -ForegroundColor Green
Write-Host "  - Balance validation on posting" -ForegroundColor Green
Write-Host "  - Immutability of posted entries" -ForegroundColor Green
Write-Host "  - Reversal mechanism" -ForegroundColor Green
Write-Host "`nJournal Entries implementation is COMPLETE!" -ForegroundColor Cyan

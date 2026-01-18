# Test script voor Accounts en Journals API
# Dit script demonstreert CRUD operaties voor accounts en journals

$baseUrl = "http://localhost:5001/api"
$tenantId = "11111111-1111-1111-1111-111111111111"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Accounts & Journals API Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Login als accountant
Write-Host "1. Login als accountant..." -ForegroundColor Yellow
$loginBody = @{
    email = "accountant@local.test"
    password = "Accountant123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post `
    -Body $loginBody -ContentType "application/json"

$token = $loginResponse.token
$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
}

Write-Host "✓ Login succesvol" -ForegroundColor Green
Write-Host ""

# ========================================
# ACCOUNTS TESTS
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ACCOUNTS TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get all accounts
Write-Host "2. Ophalen alle accounts..." -ForegroundColor Yellow
$accounts = Invoke-RestMethod -Uri "$baseUrl/accounts?page=1`&pageSize=20" `
    -Method Get -Headers $headers

Write-Host "✓ Totaal accounts: $($accounts.totalCount)" -ForegroundColor Green
Write-Host "Accounts:" -ForegroundColor White
$accounts.items | Format-Table Code, Name, Type, TypeName, IsActive -AutoSize
Write-Host ""

# Get account by code
Write-Host "3. Ophalen account via code '1100'..." -ForegroundColor Yellow
$account = Invoke-RestMethod -Uri "$baseUrl/accounts/by-code/1100" `
    -Method Get -Headers $headers

Write-Host "✓ Account gevonden:" -ForegroundColor Green
$account | Format-List Code, Name, Type, TypeName, IsActive
Write-Host ""

# Filter accounts by type
Write-Host "4. Filter accounts op type 'Asset' (1)..." -ForegroundColor Yellow
$assetAccounts = Invoke-RestMethod -Uri "$baseUrl/accounts?type=1" `
    -Method Get -Headers $headers

Write-Host "✓ Asset accounts: $($assetAccounts.totalCount)" -ForegroundColor Green
$assetAccounts.items | Format-Table Code, Name, TypeName -AutoSize
Write-Host ""

# Search accounts
Write-Host "5. Zoeken naar 'Bank'..." -ForegroundColor Yellow
$searchResults = Invoke-RestMethod -Uri "$baseUrl/accounts?search=Bank" `
    -Method Get -Headers $headers

Write-Host "✓ Gevonden: $($searchResults.totalCount)" -ForegroundColor Green
$searchResults.items | Format-Table Code, Name, TypeName -AutoSize
Write-Host ""

# Create new account
Write-Host "6. Aanmaken nieuwe account '7000 - Kantoorkosten'..." -ForegroundColor Yellow
$newAccountBody = @{
    code = "7000"
    name = "Kantoorkosten"
    type = 5  # Expense
    isActive = $true
} | ConvertTo-Json

$createdAccount = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Post `
    -Body $newAccountBody -Headers $headers -ContentType "application/json"

Write-Host "✓ Account aangemaakt:" -ForegroundColor Green
$createdAccount | Format-List Id, Code, Name, TypeName
Write-Host ""

# Update account
Write-Host "7. Bijwerken account '7000'..." -ForegroundColor Yellow
$updateAccountBody = @{
    code = "7000"
    name = "Algemene kantoorkosten"
    type = 5  # Expense
    isActive = $true
} | ConvertTo-Json

$updatedAccount = Invoke-RestMethod -Uri "$baseUrl/accounts/$($createdAccount.id)" `
    -Method Put -Body $updateAccountBody -Headers $headers -ContentType "application/json"

Write-Host "✓ Account bijgewerkt:" -ForegroundColor Green
$updatedAccount | Format-List Code, Name, TypeName
Write-Host ""

# ========================================
# JOURNALS TESTS
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JOURNALS TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get all journals
Write-Host "8. Ophalen alle journals..." -ForegroundColor Yellow
$journals = Invoke-RestMethod -Uri "$baseUrl/journals?page=1`&pageSize=20" `
    -Method Get -Headers $headers

Write-Host "✓ Totaal journals: $($journals.totalCount)" -ForegroundColor Green
Write-Host "Journals:" -ForegroundColor White
$journals.items | Format-Table Code, Name, Type, TypeName -AutoSize
Write-Host ""

# Get journal by code
Write-Host "9. Ophalen journal via code 'VRK'..." -ForegroundColor Yellow
$journal = Invoke-RestMethod -Uri "$baseUrl/journals/by-code/VRK" `
    -Method Get -Headers $headers

Write-Host "✓ Journal gevonden:" -ForegroundColor Green
$journal | Format-List Code, Name, Type, TypeName
Write-Host ""

# Filter journals by type
Write-Host "10. Filter journals op type 'Sales' (1)..." -ForegroundColor Yellow
$salesJournals = Invoke-RestMethod -Uri "$baseUrl/journals?type=1" `
    -Method Get -Headers $headers

Write-Host "✓ Sales journals: $($salesJournals.totalCount)" -ForegroundColor Green
$salesJournals.items | Format-Table Code, Name, TypeName -AutoSize
Write-Host ""

# Create new journal
Write-Host "11. Aanmaken nieuwe journal 'KAS - Kas'..." -ForegroundColor Yellow
$newJournalBody = @{
    code = "KAS"
    name = "Kas"
    type = 3  # Bank (we'll use this for cash as well)
} | ConvertTo-Json

$createdJournal = Invoke-RestMethod -Uri "$baseUrl/journals" -Method Post `
    -Body $newJournalBody -Headers $headers -ContentType "application/json"

Write-Host "✓ Journal aangemaakt:" -ForegroundColor Green
$createdJournal | Format-List Id, Code, Name, TypeName
Write-Host ""

# Update journal
Write-Host "12. Bijwerken journal 'KAS'..." -ForegroundColor Yellow
$updateJournalBody = @{
    code = "KAS"
    name = "Kasdagboek"
    type = 3  # Bank
} | ConvertTo-Json

$updatedJournal = Invoke-RestMethod -Uri "$baseUrl/journals/$($createdJournal.id)" `
    -Method Put -Body $updateJournalBody -Headers $headers -ContentType "application/json"

Write-Host "✓ Journal bijgewerkt:" -ForegroundColor Green
$updatedJournal | Format-List Code, Name, TypeName
Write-Host ""

# ========================================
# ERROR HANDLING TESTS
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ERROR HANDLING TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test duplicate account code
Write-Host "13. Proberen dubbele account code aan te maken..." -ForegroundColor Yellow
try {
    $duplicateAccountBody = @{
        code = "1100"  # Already exists
        name = "Duplicate"
        type = 1
        isActive = $true
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Post `
        -Body $duplicateAccountBody -Headers $headers -ContentType "application/json"
    
    Write-Host "✗ FOUT: Dubbele code werd geaccepteerd!" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "✓ Dubbele code correct afgewezen: $($errorResponse.message)" -ForegroundColor Green
}
Write-Host ""

# Test duplicate journal code
Write-Host "14. Proberen dubbele journal code aan te maken..." -ForegroundColor Yellow
try {
    $duplicateJournalBody = @{
        code = "VRK"  # Already exists
        name = "Duplicate"
        type = 1
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "$baseUrl/journals" -Method Post `
        -Body $duplicateJournalBody -Headers $headers -ContentType "application/json"
    
    Write-Host "✗ FOUT: Dubbele code werd geaccepteerd!" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "✓ Dubbele code correct afgewezen: $($errorResponse.message)" -ForegroundColor Green
}
Write-Host ""

# ========================================
# CLEANUP
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CLEANUP" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Delete created account
Write-Host "15. Verwijderen test account..." -ForegroundColor Yellow
Invoke-RestMethod -Uri "$baseUrl/accounts/$($createdAccount.id)" `
    -Method Delete -Headers $headers | Out-Null
Write-Host "✓ Account verwijderd" -ForegroundColor Green
Write-Host ""

# Delete created journal
Write-Host "16. Verwijderen test journal..." -ForegroundColor Yellow
Invoke-RestMethod -Uri "$baseUrl/journals/$($createdJournal.id)" `
    -Method Delete -Headers $headers | Out-Null
Write-Host "✓ Journal verwijderd" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Alle tests succesvol afgerond!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

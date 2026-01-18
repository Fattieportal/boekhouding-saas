# Test script voor Contacts API - Extended version
Write-Host "=== Contacts API Test Script ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5001/api"
$loginUrl = "$baseUrl/auth/login"
$contactsUrl = "$baseUrl/contacts"

# Login als admin
Write-Host "1. Login als admin..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@local.test"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "  User: $($loginResponse.email)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

# Get tenants
Write-Host "  Getting tenants..." -ForegroundColor Gray
try {
    $tenants = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Headers @{ "Authorization" = "Bearer $token" }
    $tenantId = $tenants[0].id
    Write-Host "  TenantId: $tenantId" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to get tenants: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
}

Write-Host ""
Write-Host "2. Get all contacts..." -ForegroundColor Yellow
try {
    $contacts = Invoke-RestMethod -Uri $contactsUrl -Method Get -Headers $headers
    Write-Host "✓ Found $($contacts.totalCount) contacts" -ForegroundColor Green
    $contacts.items | ForEach-Object {
        Write-Host "  - $($_.displayName) ($($_.typeName)) - $($_.email)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to get contacts: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Search contacts (q=Acme)..." -ForegroundColor Yellow
try {
    $searchResults = Invoke-RestMethod -Uri "$contactsUrl`?q=Acme" -Method Get -Headers $headers
    Write-Host "✓ Found $($searchResults.totalCount) contacts matching 'Acme'" -ForegroundColor Green
    $searchResults.items | ForEach-Object {
        Write-Host "  - $($_.displayName) - $($_.email)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Search failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Filter by type (Customer)..." -ForegroundColor Yellow
try {
    $customers = Invoke-RestMethod -Uri "$contactsUrl`?type=1" -Method Get -Headers $headers
    Write-Host "✓ Found $($customers.totalCount) customers" -ForegroundColor Green
    $customers.items | ForEach-Object {
        Write-Host "  - $($_.displayName)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Filter failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "5. Create new contact..." -ForegroundColor Yellow
$newContact = @{
    type = 1
    displayName = "Test Bedrijf BV"
    email = "info@testbedrijf.nl"
    phone = "+31 20 123 9999"
    addressLine1 = "Teststraat 1"
    postalCode = "1234 AB"
    city = "Amsterdam"
    country = "NL"
    vatNumber = "NL999888777B01"
    kvk = "99988877"
    isActive = $true
} | ConvertTo-Json

try {
    $created = Invoke-RestMethod -Uri $contactsUrl -Method Post -Body $newContact -Headers $headers -ContentType "application/json"
    Write-Host "✓ Contact created successfully" -ForegroundColor Green
    Write-Host "  ID: $($created.id)" -ForegroundColor Gray
    Write-Host "  Name: $($created.displayName)" -ForegroundColor Gray
    $createdId = $created.id
} catch {
    Write-Host "✗ Create failed: $_" -ForegroundColor Red
    $createdId = $null
}

if ($createdId) {
    Write-Host ""
    Write-Host "6. Get contact by ID..." -ForegroundColor Yellow
    try {
        $contact = Invoke-RestMethod -Uri "$contactsUrl/$createdId" -Method Get -Headers $headers
        Write-Host "✓ Contact retrieved" -ForegroundColor Green
        Write-Host "  Name: $($contact.displayName)" -ForegroundColor Gray
        Write-Host "  Email: $($contact.email)" -ForegroundColor Gray
        Write-Host "  City: $($contact.city)" -ForegroundColor Gray
    } catch {
        Write-Host "✗ Get by ID failed: $_" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "7. Update contact..." -ForegroundColor Yellow
    $updateContact = @{
        type = 1
        displayName = "Test Bedrijf BV (Updated)"
        email = "updated@testbedrijf.nl"
        phone = "+31 20 123 9999"
        addressLine1 = "Nieuwe Teststraat 100"
        postalCode = "1234 AB"
        city = "Rotterdam"
        country = "NL"
        vatNumber = "NL999888777B01"
        kvk = "99988877"
        isActive = $true
    } | ConvertTo-Json

    try {
        $updated = Invoke-RestMethod -Uri "$contactsUrl/$createdId" -Method Put -Body $updateContact -Headers $headers -ContentType "application/json"
        Write-Host "✓ Contact updated successfully" -ForegroundColor Green
        Write-Host "  Name: $($updated.displayName)" -ForegroundColor Gray
        Write-Host "  City: $($updated.city)" -ForegroundColor Gray
    } catch {
        Write-Host "✗ Update failed: $_" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "8. Delete contact..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$contactsUrl/$createdId" -Method Delete -Headers $headers
        Write-Host "✓ Contact deleted successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ Delete failed: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "9. Test validations (invalid email)..." -ForegroundColor Yellow
$invalidContact = @{
    type = 1
    displayName = "Invalid Contact"
    email = "not-an-email"
    country = "NL"
    isActive = $true
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri $contactsUrl -Method Post -Body $invalidContact -Headers $headers -ContentType "application/json"
    Write-Host "✗ Validation should have failed!" -ForegroundColor Red
} catch {
    Write-Host "✓ Validation correctly rejected invalid email" -ForegroundColor Green
}

Write-Host ""
Write-Host "10. Test validations (missing DisplayName)..." -ForegroundColor Yellow
$missingNameContact = @{
    type = 1
    email = "test@example.nl"
    country = "NL"
    isActive = $true
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri $contactsUrl -Method Post -Body $missingNameContact -Headers $headers -ContentType "application/json"
    Write-Host "✗ Validation should have failed!" -ForegroundColor Red
} catch {
    Write-Host "✓ Validation correctly rejected missing DisplayName" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== All tests completed ===" -ForegroundColor Cyan

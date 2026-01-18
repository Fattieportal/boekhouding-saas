# Eenvoudige test voor Contacts API
Write-Host "=== Contacts API Quick Test ===" -ForegroundColor Cyan

$baseUrl = "http://localhost:5001/api"

# Login
Write-Host "`nLogging in..." -ForegroundColor Yellow
try {
    $login = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body (@{
        email = "admin@local.test"
        password = "Admin123!"
    } | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "Login successful!" -ForegroundColor Green
    Write-Host "User: $($login.email)" -ForegroundColor Gray
} catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Get tenants
Write-Host "Getting tenants..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $($login.token)"
}

try {
    $tenants = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Headers $headers
    $tenantId = $tenants[0].id
    Write-Host "Using tenant: $($tenants[0].name)" -ForegroundColor Gray
} catch {
    Write-Host "Failed to get tenants: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Update headers with tenant ID
$headers = @{
    "Authorization" = "Bearer $($login.token)"
    "X-Tenant-Id" = $tenantId
}

# Test Contacts
Write-Host "`nContacts:" -ForegroundColor Yellow
$contacts = Invoke-RestMethod -Uri "$baseUrl/contacts" -Headers $headers
Write-Host "Total: $($contacts.totalCount)"
$contacts.items | Format-Table DisplayName, TypeName, Email, City -AutoSize

Write-Host "`nCustomers only:" -ForegroundColor Yellow
$customers = Invoke-RestMethod -Uri "$baseUrl/contacts?type=1" -Headers $headers
$customers.items | Format-Table DisplayName, Email, Phone -AutoSize

Write-Host "`nSuppliers only:" -ForegroundColor Yellow
$suppliers = Invoke-RestMethod -Uri "$baseUrl/contacts?type=2" -Headers $headers
$suppliers.items | Format-Table DisplayName, Email, VatNumber -AutoSize

Write-Host "`nSearch 'Acme':" -ForegroundColor Yellow
$search = Invoke-RestMethod -Uri "$baseUrl/contacts?q=Acme" -Headers $headers
$search.items | Format-Table DisplayName, Email, City -AutoSize

Write-Host "Done!" -ForegroundColor Green

# Test Multi-Tenant API
# Dit script demonstreert hoe je de multi-tenant API gebruikt

$baseUrl = "http://localhost:5001"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "MULTI-TENANT API TEST SCRIPT" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Stap 1: Registreer een gebruiker
Write-Host "Stap 1: Registreer gebruiker..." -ForegroundColor Yellow
$registerDto = @{
    email = "tenant-admin@example.com"
    password = "SecurePassword123!"
    role = "Admin"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" `
        -Method Post `
        -Headers $headers `
        -Body $registerDto
    
    Write-Host "[OK] Gebruiker geregistreerd" -ForegroundColor Green
    Write-Host "  Email: $($registerResponse.email)" -ForegroundColor Gray
} catch {
    Write-Host "[INFO] Gebruiker bestaat mogelijk al, probeer login..." -ForegroundColor Gray
}

# Stap 2: Login
Write-Host "`nStap 2: Login gebruiker..." -ForegroundColor Yellow
$loginDto = @{
    email = "tenant-admin@example.com"
    password = "SecurePassword123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
    -Method Post `
    -Headers $headers `
    -Body $loginDto

$token = $loginResponse.token
Write-Host "[OK] Login succesvol" -ForegroundColor Green
Write-Host "  Token: $($token.Substring(0, 50))..." -ForegroundColor Gray

# Voeg JWT token toe aan headers
$authHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

# Stap 3: Maak een tenant aan
Write-Host "`nStap 3: Maak nieuwe tenant aan..." -ForegroundColor Yellow
$createTenantDto = @{
    name = "Acme Corporation"
    kvK = "12345678"
    vatNumber = "NL123456789B01"
} | ConvertTo-Json

$tenantResponse = Invoke-RestMethod -Uri "$baseUrl/api/tenants" `
    -Method Post `
    -Headers $authHeaders `
    -Body $createTenantDto

$tenantId = $tenantResponse.id
Write-Host "[OK] Tenant aangemaakt" -ForegroundColor Green
Write-Host "  Tenant ID: $tenantId" -ForegroundColor Gray
Write-Host "  Naam: $($tenantResponse.name)" -ForegroundColor Gray
Write-Host "  KvK: $($tenantResponse.kvK)" -ForegroundColor Gray
Write-Host "  Rol: $($tenantResponse.role)" -ForegroundColor Gray

# Stap 4: Haal alle tenants op van de gebruiker
Write-Host "`nStap 4: Haal mijn tenants op..." -ForegroundColor Yellow
$myTenants = Invoke-RestMethod -Uri "$baseUrl/api/tenants/my" `
    -Method Get `
    -Headers $authHeaders

Write-Host "[OK] Tenants opgehaald" -ForegroundColor Green
foreach ($tenant in $myTenants) {
    Write-Host "  - $($tenant.name) ($($tenant.id)) - Rol: $($tenant.role)" -ForegroundColor Gray
}

# Stap 5: Maak een contact aan met X-Tenant-Id header
Write-Host "`nStap 5: Maak contact aan binnen tenant..." -ForegroundColor Yellow
$tenantHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
}

$createContactDto = @{
    type = 1  # Customer
    displayName = "Test Klant BV"
    email = "info@klant.nl"
    phone = "0201234567"
    addressLine1 = "Hoofdstraat 123"
    postalCode = "1234AB"
    city = "Amsterdam"
    country = "NL"
    vatNumber = "NL987654321B01"
    kvk = "87654321"
    isActive = $true
} | ConvertTo-Json

try {
    $contactResponse = Invoke-RestMethod -Uri "$baseUrl/api/contacts" `
        -Method Post `
        -Headers $tenantHeaders `
        -Body $createContactDto
    
    Write-Host "[OK] Contact aangemaakt binnen tenant" -ForegroundColor Green
    Write-Host "  Contact ID: $($contactResponse.id)" -ForegroundColor Gray
    Write-Host "  Naam: $($contactResponse.displayName)" -ForegroundColor Gray
} catch {
    Write-Host "[WARN] Fout bij aanmaken contact: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
}

# Stap 6: Test zonder X-Tenant-Id header (moet falen)
Write-Host "`nStap 6: Test zonder X-Tenant-Id header (moet falen)..." -ForegroundColor Yellow
try {
    $failResponse = Invoke-RestMethod -Uri "$baseUrl/api/klanten" `
        -Method Get `
        -Headers $authHeaders
    
    Write-Host "[FAIL] Request zou moeten falen maar slaagde" -ForegroundColor Red
} catch {
    Write-Host "[OK] Request correct afgewezen (400 Bad Request verwacht)" -ForegroundColor Green
    Write-Host "  Fout: $($_.Exception.Message)" -ForegroundColor Gray
}

# Stap 7: Test met ongeldige X-Tenant-Id (moet falen)
Write-Host "`nStap 7: Test met ongeldige tenant ID (moet falen)..." -ForegroundColor Yellow
$invalidTenantHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = [Guid]::NewGuid().ToString()
}

try {
    $failResponse = Invoke-RestMethod -Uri "$baseUrl/api/klanten" `
        -Method Get `
        -Headers $invalidTenantHeaders
    
    Write-Host "[FAIL] Request zou moeten falen maar slaagde" -ForegroundColor Red
} catch {
    Write-Host "[OK] Request correct afgewezen (403 Forbidden verwacht)" -ForegroundColor Green
    Write-Host "  Fout: $($_.Exception.Message)" -ForegroundColor Gray
}

# Samenvatting
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SAMENVATTING" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Token: $token" -ForegroundColor White
Write-Host "Tenant ID: $tenantId" -ForegroundColor White
Write-Host "`nGebruik deze waarden voor verdere API calls:" -ForegroundColor Yellow
Write-Host "  Authorization: Bearer $token" -ForegroundColor Gray
Write-Host "  X-Tenant-Id: $tenantId" -ForegroundColor Gray
Write-Host "`n" -ForegroundColor White

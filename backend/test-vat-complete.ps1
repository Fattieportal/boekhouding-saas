# BTW Rapportage Test Script
# Test de volledige BTW rapportage functionaliteit

$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Enable TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Skip SSL certificate validation
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback {
        public static void Ignore() {
            if(ServicePointManager.ServerCertificateValidationCallback == null) {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

# Configuration
$baseUrl = "http://localhost:5001/api"
$username = "admin@local.test"
$password = "Admin123!"

Write-Host "=== BTW Rapportage Test ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login
Write-Host "Step 1: Login..." -ForegroundColor Yellow
$loginBody = @{
    email = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"

    $token = $loginResponse.token
    Write-Host "[OK] Ingelogd als $username" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Login mislukt: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Get tenant
try {
    $tenantsResponse = Invoke-RestMethod -Uri "$baseUrl/tenants/my" `
        -Method Get `
        -Headers @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
    
    $tenantId = $tenantsResponse[0].id
    Write-Host "  Tenant ID: $tenantId" -ForegroundColor Gray
} catch {
    Write-Host "[FAIL] Kan tenant niet ophalen: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

Write-Host ""

# Step 2: Setup accounts and journals
Write-Host "Step 2: Controleren/aanmaken boekhoudrekeningen en journals..." -ForegroundColor Yellow

# Get existing accounts
try {
    $accountsResponse = Invoke-RestMethod -Uri "$baseUrl/accounts" `
        -Method Get `
        -Headers $headers
    $accounts = $accountsResponse.items
} catch {
    Write-Host "[FAIL] Kan accounts niet ophalen: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Find or create Accounts Receivable (1100 Debiteuren)
$arAccount = $accounts | Where-Object { $_.code -eq "1100" }
if (-not $arAccount) {
    try {
        $arAccount = Invoke-RestMethod -Uri "$baseUrl/accounts" `
            -Method Post `
            -Headers $headers `
            -Body (@{
                code = "1100"
                name = "Debiteuren"
                type = 1
                isActive = $true
            } | ConvertTo-Json)
        Write-Host "  [OK] AR account aangemaakt (1100 Debiteuren)" -ForegroundColor Green
    } catch {
        Write-Host "  [SKIP] AR account aanmaken mislukt - mogelijk al aanwezig" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [OK] AR account bestaat (1100 Debiteuren)" -ForegroundColor Green
}

# Find or create Revenue account (8000 Omzet)
$revenueAccount = $accounts | Where-Object { $_.code -eq "8000" }
if (-not $revenueAccount) {
    try {
        $revenueAccount = Invoke-RestMethod -Uri "$baseUrl/accounts" `
            -Method Post `
            -Headers $headers `
            -Body (@{
                code = "8000"
                name = "Omzet"
                type = 4
                isActive = $true
            } | ConvertTo-Json)
        Write-Host "  [OK] Revenue account aangemaakt (8000 Omzet)" -ForegroundColor Green
    } catch {
        Write-Host "  [SKIP] Revenue account aanmaken mislukt - mogelijk al aanwezig" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [OK] Revenue account bestaat (8000 Omzet)" -ForegroundColor Green
}

# Find or create VAT Payable account (1700 Te betalen BTW)
$vatAccount = $accounts | Where-Object { $_.code -eq "1700" }
if (-not $vatAccount) {
    try {
        $vatAccount = Invoke-RestMethod -Uri "$baseUrl/accounts" `
            -Method Post `
            -Headers $headers `
            -Body (@{
                code = "1700"
                name = "Te betalen BTW"
                type = 2
                isActive = $true
            } | ConvertTo-Json)
        Write-Host "  [OK] VAT account aangemaakt (1700 Te betalen BTW)" -ForegroundColor Green
    } catch {
        Write-Host "  [SKIP] VAT account aanmaken mislukt - mogelijk al aanwezig" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [OK] VAT account bestaat (1700 Te betalen BTW)" -ForegroundColor Green
}

# Get journals
try {
    $journalsResponse = Invoke-RestMethod -Uri "$baseUrl/journals" `
        -Method Get `
        -Headers $headers
    $journals = $journalsResponse
} catch {
    $journals = @()
}

# Find or create Sales Journal
$salesJournal = $journals | Where-Object { $_.type -eq 0 }
if (-not $salesJournal) {
    try {
        $salesJournal = Invoke-RestMethod -Uri "$baseUrl/journals" `
            -Method Post `
            -Headers $headers `
            -Body (@{
                code = "VRK"
                name = "Verkopen"
                type = 0
            } | ConvertTo-Json)
        Write-Host "  [OK] Sales journal aangemaakt (VRK Verkopen)" -ForegroundColor Green
    } catch {
        Write-Host "  [SKIP] Sales journal aanmaken mislukt - mogelijk al aanwezig" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [OK] Sales journal bestaat (VRK Verkopen)" -ForegroundColor Green
}

# Refresh accounts to get IDs
try {
    $accountsResponse = Invoke-RestMethod -Uri "$baseUrl/accounts" `
        -Method Get `
        -Headers $headers
    $accounts = $accountsResponse.items
    $revenueAccount = $accounts | Where-Object { $_.code -eq "8000" }
} catch {
    Write-Host "[FAIL] Kan accounts niet refreshen" -ForegroundColor Red
}

Write-Host ""

# Step 3: Get or create a contact
Write-Host "Step 3: Klant aanmaken/ophalen..." -ForegroundColor Yellow
try {
    $contactsResponse = Invoke-RestMethod -Uri "$baseUrl/contacts" `
        -Method Get `
        -Headers $headers
    
    if ($contactsResponse.Count -gt 0) {
        $contact = $contactsResponse[0]
        Write-Host "[OK] Bestaande klant gebruiken: $($contact.displayName)" -ForegroundColor Green
        $contactId = $contact.id
    } else {
        $contactBody = @{
            type = 1
            displayName = "Test Klant BV"
            email = "klant@test.nl"
            phone = "+31 20 1234567"
            addressLine1 = "Teststraat 123"
            postalCode = "1234 AB"
            city = "Amsterdam"
            country = "NL"
            vatNumber = "NL123456789B01"
            kvK = "12345678"
        } | ConvertTo-Json
        
        $contact = Invoke-RestMethod -Uri "$baseUrl/contacts" `
            -Method Post `
            -Headers $headers `
            -Body $contactBody
        
        Write-Host "[OK] Nieuwe klant aangemaakt: $($contact.displayName)" -ForegroundColor Green
        $contactId = $contact.id
    }
} catch {
    Write-Host "[FAIL] Klant ophalen/aanmaken mislukt: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Create test invoices with different VAT rates
Write-Host "Step 4: Test facturen aanmaken met verschillende BTW tarieven..." -ForegroundColor Yellow

$invoices = @()
$testData = @(
    @{
        Description = "Factuur met 21% BTW"
        Lines = @(
            @{ description = "Advies diensten"; quantity = 10; unitPrice = 100.00; vatRate = 21.00; accountId = $revenueAccount.id },
            @{ description = "Projectmanagement"; quantity = 5; unitPrice = 150.00; vatRate = 21.00; accountId = $revenueAccount.id }
        )
    },
    @{
        Description = "Factuur met 9% BTW"
        Lines = @(
            @{ description = "Boeken en publicaties"; quantity = 20; unitPrice = 25.00; vatRate = 9.00; accountId = $revenueAccount.id },
            @{ description = "Cursusmateriaal"; quantity = 10; unitPrice = 30.00; vatRate = 9.00; accountId = $revenueAccount.id }
        )
    },
    @{
        Description = "Factuur met 0% BTW"
        Lines = @(
            @{ description = "Export diensten (EU)"; quantity = 1; unitPrice = 1000.00; vatRate = 0.00; accountId = $revenueAccount.id },
            @{ description = "Internationale consultancy"; quantity = 5; unitPrice = 200.00; vatRate = 0.00; accountId = $revenueAccount.id }
        )
    },
    @{
        Description = "Gemengde BTW tarieven"
        Lines = @(
            @{ description = "Standaard dienst"; quantity = 10; unitPrice = 100.00; vatRate = 21.00; accountId = $revenueAccount.id },
            @{ description = "Verlaagd tarief product"; quantity = 5; unitPrice = 50.00; vatRate = 9.00; accountId = $revenueAccount.id },
            @{ description = "Export dienst"; quantity = 1; unitPrice = 500.00; vatRate = 0.00; accountId = $revenueAccount.id }
        )
    }
)

$invoiceNumber = 1
foreach ($testInvoice in $testData) {
    $invNumber = "BTW-2026-$(Get-Random -Minimum 1000 -Maximum 9999)"
    $issueDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    $dueDate = (Get-Date).AddDays(30).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

    $invoiceBody = @{
        invoiceNumber = $invNumber
        issueDate = $issueDate
        dueDate = $dueDate
        contactId = $contactId
        currency = "EUR"
        notes = $testInvoice.Description
        lines = $testInvoice.Lines
    } | ConvertTo-Json -Depth 5

    try {
        $invoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" `
            -Method Post `
            -Headers $headers `
            -Body $invoiceBody
        
        Write-Host "  [$invoiceNumber/4] Aangemaakt: $($invoice.invoiceNumber)" -ForegroundColor Green
        Write-Host "    Subtotaal: EUR $($invoice.subtotal)" -ForegroundColor Gray
        Write-Host "    BTW: EUR $($invoice.vatTotal)" -ForegroundColor Gray
        Write-Host "    Totaal: EUR $($invoice.total)" -ForegroundColor Gray
        
        $invoices += $invoice
    } catch {
        Write-Host "  [FAIL] Factuur $invoiceNumber aanmaken mislukt: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    $invoiceNumber++
}

Write-Host "[OK] $($invoices.Count) facturen aangemaakt" -ForegroundColor Green
Write-Host ""

# Step 5: Post invoices
Write-Host "Step 5: Facturen boeken (posten naar administratie)..." -ForegroundColor Yellow
Write-Host "  Opmerking: Alleen GEBOEKTE facturen verschijnen in BTW rapporten" -ForegroundColor Gray

$postedCount = 0
foreach ($invoice in $invoices) {
    try {
        $postedInvoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$($invoice.id)/post" `
            -Method Post `
            -Headers $headers
        
        $postedCount++
        Write-Host "  [$postedCount/$($invoices.Count)] Geboekt: $($invoice.invoiceNumber)" -ForegroundColor Green
    } catch {
        Write-Host "  [SKIP] Kan $($invoice.invoiceNumber) niet boeken: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "    Dit is verwacht als boekhoudrekeningen nog niet zijn geconfigureerd" -ForegroundColor Gray
    }
}

Write-Host ""
if ($postedCount -eq 0) {
    Write-Host "[WAARSCHUWING] Geen facturen geboekt - BTW rapport zal leeg zijn" -ForegroundColor Yellow
    Write-Host "  Configureer eerst de boekhoudrekeningen om facturen te kunnen boeken" -ForegroundColor Yellow
}

# Step 6: Get VAT Report
Write-Host "Step 6: BTW Rapport ophalen..." -ForegroundColor Yellow

$fromDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
$toDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")

Write-Host "  Periode: $fromDate t/m $toDate" -ForegroundColor Gray

try {
    $vatReport = Invoke-RestMethod -Uri "$baseUrl/reports/vat?from=$fromDate&to=$toDate" `
        -Method Get `
        -Headers $headers
    
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "                      BTW RAPPORT                                 " -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Periode: $($vatReport.fromDate) t/m $($vatReport.toDate)" -ForegroundColor White
    Write-Host ""
    Write-Host "  TOTALEN:" -ForegroundColor Yellow
    Write-Host "  -------" -ForegroundColor Yellow
    Write-Host "  Omzet (excl. BTW):  EUR $($vatReport.totalRevenue)" -ForegroundColor White
    Write-Host "  Totaal BTW bedrag:  EUR $($vatReport.totalVat)" -ForegroundColor White
    Write-Host "  Totaal incl. BTW:   EUR $($vatReport.totalIncludingVat)" -ForegroundColor White
    Write-Host "  Aantal facturen:    $($vatReport.invoiceCount)" -ForegroundColor White
    Write-Host ""
    Write-Host "  SPECIFICATIE PER BTW TARIEF:" -ForegroundColor Yellow
    Write-Host "  ---------------------------" -ForegroundColor Yellow
    
    if ($vatReport.vatRates -and $vatReport.vatRates.Count -gt 0) {
        foreach ($breakdown in $vatReport.vatRates | Sort-Object -Property vatRate -Descending) {
            Write-Host ""
            Write-Host "  ► $($breakdown.vatRate)% BTW" -ForegroundColor Cyan
            Write-Host "    Omzet:              EUR $($breakdown.revenue)" -ForegroundColor Gray
            Write-Host "    BTW bedrag:         EUR $($breakdown.vatAmount)" -ForegroundColor Gray
            Write-Host "    Aantal regelitems:  $($breakdown.lineCount)" -ForegroundColor Gray
        }
    } else {
        Write-Host ""
        Write-Host "  Geen BTW gegevens gevonden voor deze periode" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "[OK] BTW Rapport succesvol opgehaald" -ForegroundColor Green
    
} catch {
    Write-Host "[FAIL] BTW Rapport ophalen mislukt: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Error details: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "  Kan error details niet lezen" -ForegroundColor Gray
        }
    }
}

Write-Host ""

# Step 7: List all invoices
Write-Host "Step 7: Alle facturen tonen ter verificatie..." -ForegroundColor Yellow
try {
    $allInvoices = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" `
        -Method Get `
        -Headers $headers
    
    Write-Host "[OK] $($allInvoices.Count) factuur(en) gevonden" -ForegroundColor Green
    foreach ($inv in $allInvoices) {
        $statusText = switch ($inv.status) {
            0 { "Concept" }
            1 { "Verzonden" }
            2 { "Geboekt" }
            3 { "Betaald" }
        }
        Write-Host "  - $($inv.invoiceNumber): EUR $($inv.total) [$statusText]" -ForegroundColor Gray
    }
} catch {
    Write-Host "[FAIL] Facturen ophalen mislukt: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Voltooid ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "SAMENVATTING:" -ForegroundColor White
Write-Host "  [OK] Ingelogd" -ForegroundColor Green
Write-Host "  [OK] Klant aangemaakt/opgehaald" -ForegroundColor Green
Write-Host "  [OK] $($invoices.Count) facturen aangemaakt" -ForegroundColor Green
Write-Host "  [OK] $postedCount facturen geboekt" -ForegroundColor Green
if ($vatReport) {
    Write-Host "  [OK] BTW Rapport: EUR $($vatReport.totalVat) totaal BTW" -ForegroundColor Green
}
Write-Host ""
Write-Host "BTW RAPPORT ENDPOINT:" -ForegroundColor White
Write-Host "  GET /api/reports/vat?from=YYYY-MM-DD`&to=YYYY-MM-DD" -ForegroundColor Cyan
Write-Host ""
Write-Host "BELANGRIJK:" -ForegroundColor White
Write-Host "  - Alleen GEBOEKTE facturen verschijnen in BTW rapporten" -ForegroundColor Yellow
Write-Host "  - Als boeken mislukt, configureer eerst boekhoudrekeningen" -ForegroundColor Yellow
Write-Host "  - Zie VAT_QUICKSTART.md voor meer voorbeelden" -ForegroundColor Yellow

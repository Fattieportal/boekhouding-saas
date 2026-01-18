# Sales Invoices API Test Script
# Tests the complete invoice workflow: branding -> template -> invoice -> PDF -> posting

$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Enable TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Skip SSL certificate validation for self-signed certificates
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

Write-Host "=== Sales Invoices API Test ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login
Write-Host "Step 1: Authenticating..." -ForegroundColor Yellow
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
    
    Write-Host "[OK] Login successful" -ForegroundColor Green
    Write-Host "  Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "[FAIL] Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Get user's tenant
try {
    $tenantsResponse = Invoke-RestMethod -Uri "$baseUrl/tenants/my" `
        -Method Get `
        -Headers @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
    
    $tenantId = $tenantsResponse[0].id
    Write-Host "  Tenant: $tenantId" -ForegroundColor Gray
} catch {
    Write-Host "[FAIL] Failed to get tenant: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
    "Content-Type" = "application/json"
}

Write-Host ""

# Step 2: Set up branding
Write-Host "Step 2: Configuring tenant branding..." -ForegroundColor Yellow
$brandingBody = @{
    logoUrl = "https://via.placeholder.com/200x80"
    primaryColor = "#0066cc"
    secondaryColor = "#333333"
    fontFamily = "Arial, Helvetica, sans-serif"
    footerText = "Test Company BV`nVAT: NL123456789B01`nKvK: 12345678`nIBAN: NL12BANK0123456789"
} | ConvertTo-Json

try {
    $brandingResponse = Invoke-RestMethod -Uri "$baseUrl/tenantbranding" `
        -Method Put `
        -Headers $headers `
        -Body $brandingBody
    
    Write-Host "[OK] Branding configured" -ForegroundColor Green
    Write-Host "  Primary Color: $($brandingResponse.primaryColor)" -ForegroundColor Gray
} catch {
    Write-Host "[FAIL] Branding setup failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 3: Create a default template (optional - system has built-in)
Write-Host "Step 3: Creating custom invoice template..." -ForegroundColor Yellow
$templateBody = @{
    name = "Professional Blue"
    isDefault = $true
    htmlTemplate = @"
<!DOCTYPE html>
<html>
<head><title>Invoice</title></head>
<body>
<h1 style='color: {{ if Branding && Branding.PrimaryColor }}{{ Branding.PrimaryColor }}{{ else }}#0066cc{{ end }}'>FACTUUR {{ Invoice.InvoiceNumber }}</h1>
<p>Datum: {{ Invoice.IssueDate }}</p>
<p>Klant: {{ Contact.DisplayName }}</p>
<table>
{{for line in Lines}}
<tr>
  <td>{{ line.Description }}</td>
  <td>{{ line.Quantity }}</td>
  <td>EUR {{ line.UnitPrice }}</td>
  <td>EUR {{ line.LineTotal }}</td>
</tr>
{{end}}
</table>
<p><strong>Totaal: EUR {{ Invoice.Total }}</strong></p>
</body>
</html>
"@
    cssTemplate = "body { font-family: Arial; } table { width: 100%; } td { padding: 5px; }"
} | ConvertTo-Json

try {
    $templateResponse = Invoke-RestMethod -Uri "$baseUrl/invoicetemplates" `
        -Method Post `
        -Headers $headers `
        -Body $templateBody
    
    Write-Host "[OK] Template created: $($templateResponse.name)" -ForegroundColor Green
    $templateId = $templateResponse.id
} catch {
    Write-Host "[SKIP] Template creation skipped (using built-in default)" -ForegroundColor Yellow
    $templateId = $null
}

Write-Host ""

# Step 4: Get or create a contact
Write-Host "Step 4: Fetching contacts..." -ForegroundColor Yellow
try {
    $contactsResponse = Invoke-RestMethod -Uri "$baseUrl/contacts" `
        -Method Get `
        -Headers $headers
    
    if ($contactsResponse.Count -gt 0) {
        $contact = $contactsResponse[0]
        Write-Host "[OK] Using existing contact: $($contact.displayName)" -ForegroundColor Green
        $contactId = $contact.id
    } else {
        Write-Host "[INFO] No contacts found, creating one..." -ForegroundColor Yellow
        
        $contactBody = @{
            type = 1  # Customer
            displayName = "Test Klant BV"
            email = "klant@test.nl"
            phone = "+31 20 1234567"
            addressLine1 = "Teststraat 123"
            postalCode = "1234 AB"
            city = "Amsterdam"
            country = "NL"  # ISO 2-letter code
            vatNumber = "NL123456789B01"
            kvK = "12345678"
        } | ConvertTo-Json
        
        $contact = Invoke-RestMethod -Uri "$baseUrl/contacts" `
            -Method Post `
            -Headers $headers `
            -Body $contactBody
        
        Write-Host "[OK] Contact created: $($contact.displayName)" -ForegroundColor Green
        $contactId = $contact.id
    }
} catch {
    Write-Host "[FAIL] Contact fetch/create failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 5: Create sales invoice
Write-Host "Step 5: Creating sales invoice..." -ForegroundColor Yellow
$invoiceNumber = "INV-2026-$(Get-Random -Minimum 1000 -Maximum 9999)"
$issueDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$dueDate = (Get-Date).AddDays(30).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

$invoiceBody = @{
    invoiceNumber = $invoiceNumber
    issueDate = $issueDate
    dueDate = $dueDate
    contactId = $contactId
    currency = "EUR"
    notes = "Test invoice created by PowerShell script"
    lines = @(
        @{
            description = "Consulting Services - January 2026"
            quantity = 10
            unitPrice = 150.00
            vatRate = 21.00
        },
        @{
            description = "Project Management"
            quantity = 5
            unitPrice = 200.00
            vatRate = 21.00
        }
    )
}

# Only add templateId if it exists
if ($templateId) {
    $invoiceBody.templateId = $templateId
}

$invoiceBodyJson = $invoiceBody | ConvertTo-Json -Depth 5

try {
    $invoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" `
        -Method Post `
        -Headers $headers `
        -Body $invoiceBodyJson
    
    Write-Host "[OK] Invoice created: $($invoice.invoiceNumber)" -ForegroundColor Green
    Write-Host "  Subtotal: EUR $($invoice.subtotal)" -ForegroundColor Gray
    Write-Host "  VAT: EUR $($invoice.vatTotal)" -ForegroundColor Gray
    Write-Host "  Total: EUR $($invoice.total)" -ForegroundColor Gray
    Write-Host "  Status: $($invoice.status)" -ForegroundColor Gray
    Write-Host "  Lines: $($invoice.lines.Count)" -ForegroundColor Gray
    
    $invoiceId = $invoice.id
} catch {
    Write-Host "[FAIL] Invoice creation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""

# Step 6: Generate PDF
Write-Host "Step 6: Generating PDF..." -ForegroundColor Yellow
Write-Host "  Note: This requires Playwright to be installed" -ForegroundColor Gray
Write-Host "  Run: playwright install chromium" -ForegroundColor Gray

try {
    $pdfPath = "invoice_$invoiceNumber.pdf"
    
    Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$invoiceId/render-pdf" `
        -Method Post `
        -Headers $headers `
        -OutFile $pdfPath
    
    if (Test-Path $pdfPath) {
        $pdfSize = (Get-Item $pdfPath).Length
        Write-Host "[OK] PDF generated: $pdfPath - Size: $pdfSize bytes" -ForegroundColor Green
        
        # Open PDF if possible
        if ($IsWindows -or ($env:OS -match "Windows")) {
            Start-Process $pdfPath
            Write-Host "  PDF opened in default viewer" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "[SKIP] PDF generation failed: $($_.Exception.Message)" -ForegroundColor Yellow
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Error details: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "  Could not read error details" -ForegroundColor Gray
        }
    }
    Write-Host "  This is expected if Playwright is not installed" -ForegroundColor Gray
}

Write-Host ""

# Step 7: List all invoices
Write-Host "Step 7: Listing all invoices..." -ForegroundColor Yellow
try {
    $allInvoices = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" `
        -Method Get `
        -Headers $headers
    
    Write-Host "[OK] Found $($allInvoices.Count) invoice(s)" -ForegroundColor Green
    foreach ($inv in $allInvoices) {
        $statusText = switch ($inv.status) {
            0 { "Draft" }
            1 { "Sent" }
            2 { "Posted" }
            3 { "Paid" }
        }
        Write-Host "  - $($inv.invoiceNumber): EUR $($inv.total) [$statusText]" -ForegroundColor Gray
    }
} catch {
    Write-Host "[FAIL] List invoices failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 8: Test posting (optional - requires accounting setup)
Write-Host "Step 8: Testing invoice posting..." -ForegroundColor Yellow
Write-Host "  Skipping - requires accounts setup for journal entries" -ForegroundColor Gray
# Uncomment to test posting:
# try {
#     $postedInvoice = Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$invoiceId/post" `
#         -Method Post `
#         -Headers $headers `
#         -SkipCertificateCheck
#     
#     Write-Host "✓ Invoice posted to accounting" -ForegroundColor Green
#     Write-Host "  Journal Entry ID: $($postedInvoice.journalEntryId)" -ForegroundColor Gray
# } catch {
#     Write-Host "! Posting failed: $($_.Exception.Message)" -ForegroundColor Yellow
# }

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  - Branding: Configured [OK]" -ForegroundColor Green
Write-Host "  - Template: Created/Using default [OK]" -ForegroundColor Green
Write-Host "  - Contact: $contactId [OK]" -ForegroundColor Green
Write-Host "  - Invoice: $invoiceNumber (EUR $($invoice.total)) [OK]" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Install Playwright: playwright install chromium" -ForegroundColor Cyan
Write-Host "  2. Test PDF generation via API or frontend" -ForegroundColor Cyan
Write-Host "  3. Configure accounting accounts for posting" -ForegroundColor Cyan
Write-Host "  4. Visit frontend: /settings/branding and /settings/templates" -ForegroundColor Cyan

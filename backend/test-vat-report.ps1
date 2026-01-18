# Test script voor BTW rapportage
$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BTW Rapportage Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Enable TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Skip SSL certificate validation for localhost (PowerShell 5.1 compatible)
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

$baseUrl = "http://localhost:5001/api"

# Step 1: Login
Write-Host "[1/6] Logging in..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}'

$token = $loginResponse.token
$headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }

Write-Host "OK Logged in" -ForegroundColor Green
Write-Host ""

# Step 2: Create customer
Write-Host "[2/6] Creating test customer..." -ForegroundColor Yellow
$customerBody = @{ 
    displayName = "BTW Test Klant BV"
    type = 1
    email = "btwtest@example.com"
    vatNumber = "NL123456789B01"
} | ConvertTo-Json

try {
    $customer = Invoke-RestMethod -Uri "$baseUrl/contacts" -Method Post -Headers $headers -Body $customerBody
    Write-Host "OK Customer created: $($customer.displayName)" -ForegroundColor Green
} catch {
    Write-Host "WARN Could not create customer, might already exist" -ForegroundColor Yellow
    # Try to get existing customer
    $allContacts = Invoke-RestMethod -Uri "$baseUrl/contacts" -Method Get -Headers $headers
    $customer = $allContacts | Where-Object { $_.email -eq "btwtest@example.com" } | Select-Object -First 1
    if (-not $customer) {
        Write-Host "ERROR Could not find or create customer" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK Using existing customer: $($customer.displayName)" -ForegroundColor Green
}
Write-Host ""

# Step 3: Create invoices
Write-Host "[3/6] Creating test invoices..." -ForegroundColor Yellow

# Invoice 1: 21% VAT
$invoice1Body = @{ contactId = $customer.id; issueDate = "2026-01-10"; dueDate = "2026-02-10"; lines = @( @{ description = "Consultancy (21%)"; quantity = 10; unitPrice = 100.00; vatRate = 21.00 } ) } | ConvertTo-Json
$invoice1 = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Post -Headers $headers -Body $invoice1Body

# Invoice 2: 9% VAT
$invoice2Body = @{ contactId = $customer.id; issueDate = "2026-01-15"; dueDate = "2026-02-15"; lines = @( @{ description = "Books (9%)"; quantity = 5; unitPrice = 50.00; vatRate = 9.00 } ) } | ConvertTo-Json
$invoice2 = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Post -Headers $headers -Body $invoice2Body

# Invoice 3: 0% VAT
$invoice3Body = @{ contactId = $customer.id; issueDate = "2026-01-17"; dueDate = "2026-02-17"; lines = @( @{ description = "Export (0%)"; quantity = 3; unitPrice = 200.00; vatRate = 0.00 } ) } | ConvertTo-Json
$invoice3 = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Post -Headers $headers -Body $invoice3Body

# Invoice 4: Mixed
$invoice4Body = @{ contactId = $customer.id; issueDate = "2026-01-16"; dueDate = "2026-02-16"; lines = @( @{ description = "Software (21%)"; quantity = 1; unitPrice = 500.00; vatRate = 21.00 }, @{ description = "Docs (9%)"; quantity = 2; unitPrice = 25.00; vatRate = 9.00 } ) } | ConvertTo-Json
$invoice4 = Invoke-RestMethod -Uri "$baseUrl/salesinvoices" -Method Post -Headers $headers -Body $invoice4Body

Write-Host "OK 4 invoices created" -ForegroundColor Green
Write-Host ""

# Step 4: Post invoices
Write-Host "[4/6] Posting invoices..." -ForegroundColor Yellow
$invoices = @($invoice1.id, $invoice2.id, $invoice3.id, $invoice4.id)
foreach ($invoiceId in $invoices) {
    Invoke-RestMethod -Uri "$baseUrl/salesinvoices/$invoiceId/post" -Method Post -Headers $headers | Out-Null
}
Write-Host "OK All invoices posted" -ForegroundColor Green
Write-Host ""

# Step 5: Get VAT Report
Write-Host "[5/6] Fetching BTW report..." -ForegroundColor Yellow
$fromDate = "2026-01-01"
$toDate = "2026-01-31"
$vatReport = Invoke-RestMethod -Uri "$baseUrl/reports/vat?from=$fromDate&to=$toDate" -Method Get -Headers $headers

Write-Host "OK Report retrieved" -ForegroundColor Green
Write-Host ""

# Step 6: Display results
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BTW RAPPORTAGE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Periode: $($vatReport.fromDate.Substring(0,10)) t/m $($vatReport.toDate.Substring(0,10))"
Write-Host "Aantal facturen: $($vatReport.invoiceCount)"
Write-Host ""

foreach ($rate in $vatReport.vatRates) {
    Write-Host "BTW $($rate.vatRate)%:" -ForegroundColor Cyan
    Write-Host "  Omzet: EUR $([math]::Round($rate.revenue, 2))"
    Write-Host "  BTW: EUR $([math]::Round($rate.vatAmount, 2))"
    Write-Host "  Regels: $($rate.lineCount)"
    Write-Host ""
}

Write-Host "TOTALEN:" -ForegroundColor Yellow
Write-Host "  Omzet: EUR $([math]::Round($vatReport.totalRevenue, 2))"
Write-Host "  BTW: EUR $([math]::Round($vatReport.totalVat, 2))"
Write-Host "  Incl BTW: EUR $([math]::Round($vatReport.totalIncludingVat, 2))"
Write-Host ""

# Validation
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VALIDATIE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$expectedRevenue = 2400.00
$expectedVat = 342.00
$expectedTotal = 2742.00

$revenueOK = [math]::Abs($vatReport.totalRevenue - $expectedRevenue) -lt 0.01
$vatOK = [math]::Abs($vatReport.totalVat - $expectedVat) -lt 0.01
$totalOK = [math]::Abs($vatReport.totalIncludingVat - $expectedTotal) -lt 0.01
$invoiceCountOK = $vatReport.invoiceCount -eq 4

if ($revenueOK) { Write-Host "OK Omzet: EUR $([math]::Round($vatReport.totalRevenue, 2))" -ForegroundColor Green } 
else { Write-Host "FAIL Omzet" -ForegroundColor Red }

if ($vatOK) { Write-Host "OK BTW: EUR $([math]::Round($vatReport.totalVat, 2))" -ForegroundColor Green } 
else { Write-Host "FAIL BTW" -ForegroundColor Red }

if ($totalOK) { Write-Host "OK Totaal: EUR $([math]::Round($vatReport.totalIncludingVat, 2))" -ForegroundColor Green } 
else { Write-Host "FAIL Totaal" -ForegroundColor Red }

if ($invoiceCountOK) { Write-Host "OK Invoice count: $($vatReport.invoiceCount)" -ForegroundColor Green } 
else { Write-Host "FAIL Invoice count" -ForegroundColor Red }

Write-Host ""
if ($revenueOK -and $vatOK -and $totalOK -and $invoiceCountOK) {
    Write-Host "OK ALLE TESTS GESLAAGD!" -ForegroundColor Green
} else {
    Write-Host "FAIL SOMMIGE TESTS GEFAALD" -ForegroundColor Red
}

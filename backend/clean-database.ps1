# Clean database script
# This removes all SalesInvoices, InvoiceTemplates, and TenantBrandings to allow clean testing

$connectionString = "Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"

Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.0\System.Data.Common.dll" -ErrorAction SilentlyContinue

# Create SQL commands
$sql = @"
-- Delete all sales invoice data
DELETE FROM "SalesInvoiceLines";
DELETE FROM "SalesInvoices";

-- Delete all template and branding data
DELETE FROM "InvoiceTemplates";
DELETE FROM "TenantBrandings";
DELETE FROM "StoredFiles";

SELECT 'Database cleaned successfully' AS result;
"@

Write-Host "Cleaning database..." -ForegroundColor Yellow
Write-Host "SQL to execute:" -ForegroundColor Gray
Write-Host $sql -ForegroundColor DarkGray
Write-Host ""
Write-Host "This script requires PostgreSQL client (psql) or .NET Npgsql package" -ForegroundColor Red
Write-Host "Please run these SQL commands manually in pgAdmin or another PostgreSQL tool" -ForegroundColor Yellow

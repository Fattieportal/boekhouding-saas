# Quick database cleanup script
# Deletes all TenantBranding records so they can be recreated with proper TenantId

Write-Host "=== Database Cleanup Tool ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will DELETE all TenantBranding records from the database." -ForegroundColor Yellow
Write-Host "This is necessary to fix old records created without TenantId." -ForegroundColor Yellow
Write-Host ""

$connectionString = "Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"

# Load Npgsql if available
try {
    Add-Type -Path "C:\Users\Gslik\.nuget\packages\npgsql\8.0.1\lib\net8.0\Npgsql.dll" -ErrorAction Stop
    
    $conn = New-Object Npgsql.NpgsqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "DELETE FROM ""TenantBrandings"""
    $rows = $cmd.ExecuteNonQuery()
    
    Write-Host "✓ Deleted $rows branding record(s)" -ForegroundColor Green
    
    $cmd.CommandText = "SELECT COUNT(*) FROM ""TenantBrandings"""
    $count = $cmd.ExecuteScalar()
    Write-Host "✓ Remaining brandings: $count" -ForegroundColor Green
    
    $conn.Close()
    
    Write-Host ""
    Write-Host "Database cleaned! Run .\test-sales-invoices.ps1 again." -ForegroundColor Cyan
} catch {
    Write-Host ""
    Write-Host "Could not load Npgsql. Please run this SQL manually in pgAdmin:" -ForegroundColor Red
    Write-Host ""
    Write-Host "DELETE FROM ""TenantBrandings"";" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or use the SQL file: delete-all-brandings.sql" -ForegroundColor Gray
}

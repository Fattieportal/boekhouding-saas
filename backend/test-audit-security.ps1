# Test script voor Audit Log & Security features
# Voert basis smoke tests uit

Write-Host "=== Audit Log & Security Test Script ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000"
$testEmail = "audit-test-$(Get-Random)@example.com"
$testPassword = "Test123!Secure"

# Hulp functie voor pretty output
function Test-Step {
    param($Description, $ScriptBlock)
    Write-Host "► Testing: $Description" -ForegroundColor Yellow
    try {
        & $ScriptBlock
        Write-Host "  ✓ PASSED" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "  ✗ FAILED: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test 1: Registratie en Tenant Creation Audit
$test1 = Test-Step "Tenant creation audit log" {
    Write-Host "  Creating new tenant..." -ForegroundColor Gray
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" `
        -Method Post `
        -ContentType "application/json" `
        -Body (@{
            email = $testEmail
            password = $testPassword
            tenantName = "Audit Test Company BV"
        } | ConvertTo-Json)
    
    $script:token = $registerResponse.token
    $script:tenantId = $registerResponse.tenantId
    $script:userId = $registerResponse.userId
    
    Write-Host "  Tenant ID: $($script:tenantId)" -ForegroundColor Gray
    
    # Wacht even voor audit log processing
    Start-Sleep -Milliseconds 500
    
    # Haal audit logs op
    Write-Host "  Fetching audit logs..." -ForegroundColor Gray
    $logs = Invoke-RestMethod -Uri "$baseUrl/api/auditlogs?entityType=Tenant" `
        -Method Get `
        -Headers @{
            "Authorization" = "Bearer $($script:token)"
            "X-Tenant-Id" = $script:tenantId
        }
    
    if ($logs.Count -eq 0) {
        throw "No audit logs found for tenant creation"
    }
    
    $createLog = $logs | Where-Object { $_.action -eq "Create" -and $_.entityId -eq $script:tenantId } | Select-Object -First 1
    
    if (-not $createLog) {
        throw "Tenant creation not logged"
    }
    
    Write-Host "  Audit log found: Action=$($createLog.action), EntityType=$($createLog.entityType)" -ForegroundColor Gray
}

# Test 2: Rate Limiting op Login
$test2 = Test-Step "Rate limiting on login endpoint" {
    Write-Host "  Attempting 6 rapid login requests..." -ForegroundColor Gray
    
    $results = @()
    for ($i = 1; $i -le 6; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/login" `
                -Method Post `
                -ContentType "application/json" `
                -Body (@{
                    email = "nonexistent-$(Get-Random)@example.com"
                    password = "WrongPassword123"
                } | ConvertTo-Json) `
                -ErrorAction Stop
            $results += @{ attempt = $i; status = $response.StatusCode }
        } catch {
            $statusCode = $_.Exception.Response.StatusCode.Value__
            $results += @{ attempt = $i; status = $statusCode }
        }
        Start-Sleep -Milliseconds 50
    }
    
    Write-Host "  Request results: $($results | ForEach-Object { "$($_.attempt):$($_.status)" } | Join-String -Separator ', ')" -ForegroundColor Gray
    
    # Check of laatste request 429 is
    $lastStatus = $results[-1].status
    if ($lastStatus -ne 429) {
        throw "Expected HTTP 429 on 6th request, got $lastStatus"
    }
    
    Write-Host "  Rate limit triggered at request 6 (HTTP 429)" -ForegroundColor Gray
}

# Test 3: CORS - Invalid Origin
$test3 = Test-Step "CORS blocking unauthorized origin" {
    Write-Host "  Testing request from evil-site.com..." -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/login" `
            -Method Post `
            -Headers @{
                "Origin" = "http://evil-site.com"
            } `
            -ContentType "application/json" `
            -Body (@{
                email = "test@example.com"
                password = "test123"
            } | ConvertTo-Json) `
            -ErrorAction Stop
        
        throw "Request should have been blocked but got status $($response.StatusCode)"
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        if ($statusCode -ne 403) {
            throw "Expected HTTP 403, got $statusCode"
        }
        Write-Host "  Unauthorized origin blocked with HTTP 403" -ForegroundColor Gray
    }
}

# Test 4: Tenant Isolation - Missing Header
$test4 = Test-Step "Tenant isolation - missing X-Tenant-Id header" {
    Write-Host "  Attempting API call without X-Tenant-Id..." -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/contacts" `
            -Method Get `
            -Headers @{
                "Authorization" = "Bearer $($script:token)"
            } `
            -ErrorAction Stop
        
        throw "Request should have been blocked but got status $($response.StatusCode)"
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        if ($statusCode -ne 400 -and $statusCode -ne 403) {
            throw "Expected HTTP 400 or 403, got $statusCode"
        }
        Write-Host "  Request blocked with HTTP $statusCode" -ForegroundColor Gray
    }
}

# Test 5: Invoice Template Creation Audit
$test5 = Test-Step "Invoice template creation audit" {
    Write-Host "  Creating invoice template..." -ForegroundColor Gray
    
    $template = Invoke-RestMethod -Uri "$baseUrl/api/invoicetemplates" `
        -Method Post `
        -Headers @{
            "Authorization" = "Bearer $($script:token)"
            "X-Tenant-Id" = $script:tenantId
        } `
        -ContentType "application/json" `
        -Body (@{
            name = "Test Audit Template"
            templateContent = "<html><body>Test</body></html>"
            isDefault = $false
        } | ConvertTo-Json)
    
    Write-Host "  Template ID: $($template.id)" -ForegroundColor Gray
    
    Start-Sleep -Milliseconds 500
    
    # Check audit log
    $logs = Invoke-RestMethod -Uri "$baseUrl/api/auditlogs/entity/InvoiceTemplate/$($template.id)" `
        -Method Get `
        -Headers @{
            "Authorization" = "Bearer $($script:token)"
            "X-Tenant-Id" = $script:tenantId
        }
    
    $createLog = $logs | Where-Object { $_.action -eq "Create" } | Select-Object -First 1
    
    if (-not $createLog) {
        throw "Template creation not logged"
    }
    
    Write-Host "  Template creation logged successfully" -ForegroundColor Gray
}

# Test 6: Content-Type Validation
$test6 = Test-Step "Content-Type validation" {
    Write-Host "  Attempting POST with invalid content-type..." -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/register" `
            -Method Post `
            -ContentType "text/plain" `
            -Body "invalid body" `
            -ErrorAction Stop
        
        throw "Request should have been blocked but got status $($response.StatusCode)"
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        if ($statusCode -ne 415 -and $statusCode -ne 400) {
            throw "Expected HTTP 415 or 400, got $statusCode"
        }
        Write-Host "  Invalid content-type rejected with HTTP $statusCode" -ForegroundColor Gray
    }
}

# Test 7: Audit Log Query Features
$test7 = Test-Step "Audit log query features" {
    Write-Host "  Testing audit log queries..." -ForegroundColor Gray
    
    # Query met filters
    $startDate = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
    $uri = "$baseUrl/api/auditlogs?startDate=$startDate" + "&take=50"
    $logs = Invoke-RestMethod -Uri $uri `
        -Method Get `
        -Headers @{
            "Authorization" = "Bearer $($script:token)"
            "X-Tenant-Id" = $script:tenantId
        }
    
    if ($logs.Count -eq 0) {
        throw "No audit logs returned"
    }
    
    Write-Host "  Found $($logs.Count) audit log entries" -ForegroundColor Gray
    
    # Check dat logs sorted zijn op timestamp (newest first)
    if ($logs.Count -gt 1) {
        $first = [DateTime]::Parse($logs[0].timestamp)
        $second = [DateTime]::Parse($logs[1].timestamp)
        
        if ($first -lt $second) {
            throw "Audit logs not sorted correctly (newest first)"
        }
    }
    
    Write-Host "  Logs correctly sorted by timestamp (descending)" -ForegroundColor Gray
}

# Samenvatting
Write-Host ""
Write-Host "=== Test Results Summary ===" -ForegroundColor Cyan
Write-Host ""

$results = @{
    "Tenant creation audit" = $test1
    "Rate limiting" = $test2
    "CORS security" = $test3
    "Tenant isolation" = $test4
    "Template audit" = $test5
    "Content-Type validation" = $test6
    "Audit log queries" = $test7
}

$passed = 0
$failed = 0

foreach ($test in $results.GetEnumerator()) {
    $status = if ($test.Value) { "[PASS]"; $passed++ } else { "[FAIL]"; $failed++ }
    $color = if ($test.Value) { "Green" } else { "Red" }
    Write-Host "$status - $($test.Key)" -ForegroundColor $color
}

Write-Host ""
Write-Host "Total: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
Write-Host ""

if ($failed -eq 0) {
    Write-Host "🎉 All security and audit tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some tests failed. Please review the output above." -ForegroundColor Yellow
    exit 1
}

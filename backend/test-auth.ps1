# Test Authentication Endpoints
Write-Host "=== Boekhouding API - Authentication Tests ===" -ForegroundColor Cyan
Write-Host ""

# Wait for server to be ready
Write-Host "Waiting for server to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Test 1: Health Check
Write-Host "`n[TEST 1] Health Check" -ForegroundColor Green
try {
    $health = Invoke-WebRequest -Uri "http://localhost:5001/health" -UseBasicParsing | ConvertFrom-Json
    Write-Host "[OK] Health check passed" -ForegroundColor Green
    Write-Host "  Status: $($health.status)"
} catch {
    Write-Host "[FAIL] Health check failed: $_" -ForegroundColor Red
}

# Test 2: Login as Admin
Write-Host "`n[TEST 2] Login as Admin" -ForegroundColor Green
try {
    $adminLogin = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"Admin123!"}' -UseBasicParsing | ConvertFrom-Json
    
    Write-Host "[OK] Admin login successful" -ForegroundColor Green
    Write-Host "  Email: $($adminLogin.email)"
    Write-Host "  Role: $($adminLogin.role)"
    Write-Host "  Token (first 50 chars): $($adminLogin.token.Substring(0, 50))..."
    Write-Host "  Expires: $($adminLogin.expiresAt)"
    
    $adminToken = $adminLogin.token
} catch {
    Write-Host "[FAIL] Admin login failed: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Get Current User Info
Write-Host "`n[TEST 3] Get Current User (/me endpoint)" -ForegroundColor Green
try {
    $me = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -Headers @{Authorization="Bearer $adminToken"} -UseBasicParsing | ConvertFrom-Json
    
    Write-Host "[OK] /me endpoint successful" -ForegroundColor Green
    Write-Host "  User ID: $($me.userId)"
    Write-Host "  Email: $($me.email)"
    Write-Host "  Role: $($me.role)"
} catch {
    Write-Host "[FAIL] /me endpoint failed: $_" -ForegroundColor Red
}

# Test 4: Login as Accountant
Write-Host "`n[TEST 4] Login as Accountant" -ForegroundColor Green
try {
    $accountantLogin = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"accountant@local.test","password":"Accountant123!"}' -UseBasicParsing | ConvertFrom-Json
    
    Write-Host "[OK] Accountant login successful" -ForegroundColor Green
    Write-Host "  Email: $($accountantLogin.email)"
    Write-Host "  Role: $($accountantLogin.role)"
} catch {
    Write-Host "[FAIL] Accountant login failed: $_" -ForegroundColor Red
}

# Test 5: Login as Viewer
Write-Host "`n[TEST 5] Login as Viewer" -ForegroundColor Green
try {
    $viewerLogin = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"viewer@local.test","password":"Viewer123!"}' -UseBasicParsing | ConvertFrom-Json
    
    Write-Host "[OK] Viewer login successful" -ForegroundColor Green
    Write-Host "  Email: $($viewerLogin.email)"
    Write-Host "  Role: $($viewerLogin.role)"
} catch {
    Write-Host "[FAIL] Viewer login failed: $_" -ForegroundColor Red
}

# Test 6: Register New User
Write-Host "`n[TEST 6] Register New User" -ForegroundColor Green
try {
    $randomEmail = "testuser$(Get-Random -Minimum 1000 -Maximum 9999)@test.com"
    $registerBody = @{
        email = $randomEmail
        password = "Test123!"
        role = "Viewer"
    } | ConvertTo-Json
    
    $register = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/register" -Method POST -ContentType "application/json" -Body $registerBody -UseBasicParsing | ConvertFrom-Json
    
    Write-Host "[OK] Registration successful" -ForegroundColor Green
    Write-Host "  Email: $($register.email)"
    Write-Host "  Role: $($register.role)"
} catch {
    Write-Host "[FAIL] Registration failed: $_" -ForegroundColor Red
}

# Test 7: Invalid Login
Write-Host "`n[TEST 7] Invalid Login (should fail)" -ForegroundColor Green
try {
    $invalid = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@local.test","password":"WrongPassword"}' -UseBasicParsing -ErrorAction Stop
    
    Write-Host "[FAIL] Invalid login should have failed but did not!" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "[OK] Invalid login correctly rejected (401)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Unexpected error: $_" -ForegroundColor Red
    }
}

# Test 8: Unauthorized Access
Write-Host "`n[TEST 8] Unauthorized Access (should fail)" -ForegroundColor Green
try {
    $unauthorized = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/me" -Method GET -UseBasicParsing -ErrorAction Stop
    
    Write-Host "[FAIL] Unauthorized access should have failed but did not!" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "[OK] Unauthorized access correctly rejected (401)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Unexpected error: $_" -ForegroundColor Red
    }
}

Write-Host "`n=== All Tests Completed ===" -ForegroundColor Cyan
Write-Host "`nYou can now test the API at: http://localhost:5001/swagger" -ForegroundColor Yellow


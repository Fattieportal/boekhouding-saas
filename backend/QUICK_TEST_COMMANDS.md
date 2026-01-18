# Quick Test Commands - Accounts & Journals API

## Setup
```powershell
# Login
$login = @{ email = "accountant@local.test"; password = "Accountant123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" -Method Post -Body $login -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($auth.token)"; "X-Tenant-Id" = "11111111-1111-1111-1111-111111111111" }
```

## Accounts

### List all accounts
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts" -Headers $headers | ConvertTo-Json -Depth 5
```

### Get by code
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts/by-code/1100" -Headers $headers
```

### Filter by type (Asset = 1)
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts?type=1" -Headers $headers
```

### Search
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts?search=Bank" -Headers $headers
```

### Create account
```powershell
$newAcc = @{ code = "7000"; name = "Kantoorkosten"; type = 5; isActive = $true } | ConvertTo-Json
$created = Invoke-RestMethod -Uri "http://localhost:5001/api/accounts" -Method Post -Body $newAcc -Headers $headers -ContentType "application/json"
$created
```

### Update account
```powershell
$updateAcc = @{ code = "7000"; name = "Algemene kantoorkosten"; type = 5; isActive = $true } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts/$($created.id)" -Method Put -Body $updateAcc -Headers $headers -ContentType "application/json"
```

### Delete account
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/accounts/$($created.id)" -Method Delete -Headers $headers
```

## Journals

### List all journals
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/journals" -Headers $headers | ConvertTo-Json -Depth 5
```

### Get by code
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/journals/by-code/VRK" -Headers $headers
```

### Filter by type (Sales = 1)
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/journals?type=1" -Headers $headers
```

### Create journal
```powershell
$newJnl = @{ code = "KAS"; name = "Kasdagboek"; type = 3 } | ConvertTo-Json
$createdJ = Invoke-RestMethod -Uri "http://localhost:5001/api/journals" -Method Post -Body $newJnl -Headers $headers -ContentType "application/json"
$createdJ
```

### Update journal
```powershell
$updateJnl = @{ code = "KAS"; name = "Kas"; type = 3 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5001/api/journals/$($createdJ.id)" -Method Put -Body $updateJnl -Headers $headers -ContentType "application/json"
```

### Delete journal
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/journals/$($createdJ.id)" -Method Delete -Headers $headers
```

## Enums Reference

**AccountType:**
- 1 = Asset (Activa)
- 2 = Liability (Passiva)
- 3 = Equity (Eigen vermogen)
- 4 = Revenue (Opbrengsten)
- 5 = Expense (Kosten)

**JournalType:**
- 1 = Sales (Verkoop)
- 2 = Purchase (Inkoop)
- 3 = Bank
- 4 = General (Memoriaal)

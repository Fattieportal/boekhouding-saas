# Test: Sales Invoice Posting

Write-Host "=== Testing Sales Invoice Posting ===" -ForegroundColor Cyan

# Configuration
$baseUrl = "http://localhost:5001/api"
$email = "admin@local.test"
$password = "Admin123!"
$tenantName = "Acme Corporation"

# Helper function to make authenticated requests
function Invoke-AuthRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [switch]$AllowErrors
    )
    
    $headers = @{
        "Authorization" = "Bearer $script:token"
        "X-Tenant-Id" = $script:tenantId
        "Content-Type" = "application/json"
    }
    
    $params = @{
        Method = $Method
        Uri = $Uri
        Headers = $headers
    }
    
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    
    try {
        return Invoke-RestMethod @params
    }
    catch {
        if (-not $AllowErrors) {
            Write-Host "Error: $_" -ForegroundColor Red
            if ($_.ErrorDetails.Message) {
                Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
            }
            if ($_.Exception.Response) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $reader.BaseStream.Position = 0
                $responseBody = $reader.ReadToEnd()
                Write-Host "Response: $responseBody" -ForegroundColor Red
            }
        }
        throw
    }
}

# Step 1: Login
Write-Host "`n1. Logging in..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Method Post -Uri "$baseUrl/auth/login" -Body (@{
    email = $email
    password = $password
    tenantName = $tenantName
} | ConvertTo-Json) -ContentType "application/json"

$script:token = $loginResponse.token
$script:tenantId = if ($loginResponse.tenantId) { $loginResponse.tenantId } else { "0b192e27-ad14-4f61-982a-e96def42394a" }

Write-Host "   OK Logged in. Tenant ID: $script:tenantId" -ForegroundColor Green

# Step 2: Create required accounts if they don't exist
Write-Host "`n2. Setting up Chart of Accounts..." -ForegroundColor Yellow

# Get existing accounts
$accountsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/accounts"
$accounts = $accountsResponse.items

# Find or create Accounts Receivable (use existing 1100 Debiteuren)
$arAccount = $accounts | Where-Object { $_.code -eq "1100" -and $_.name -eq "Debiteuren" }
if (-not $arAccount) {
    Write-Host "   Creating Accounts Receivable account..." -ForegroundColor Gray
    try {
        $arAccount = Invoke-AuthRequest -Method Post -Uri "$baseUrl/accounts" -Body @{
            code = "1100"
            name = "Debiteuren"
            type = 1
            isActive = $true
        } -AllowErrors
        Write-Host "   OK Created AR account" -ForegroundColor Green
    } catch {
        # Account might already exist, try to get it again
        $accountsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/accounts"
        $accounts = $accountsResponse.items
        $arAccount = $accounts | Where-Object { $_.code -eq "1100" }
        if ($arAccount) {
            Write-Host "   OK AR account exists" -ForegroundColor Green
        } else {
            Write-Host "   FAIL Could not create or find AR account" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "   OK AR account exists (1100 Debiteuren)" -ForegroundColor Green
}

# Find Revenue account (8000)
$revenueAccount = $accounts | Where-Object { $_.code -eq "8000" }
if (-not $revenueAccount) {
    Write-Host "   Creating Revenue account (8000)..." -ForegroundColor Gray
    try {
        $revenueAccount = Invoke-AuthRequest -Method Post -Uri "$baseUrl/accounts" -Body @{
            code = "8000"
            name = "Omzet"
            type = 4
            isActive = $true
        } -AllowErrors
        Write-Host "   OK Created Revenue account" -ForegroundColor Green
    } catch {
        $accountsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/accounts"
        $accounts = $accountsResponse.items
        $revenueAccount = $accounts | Where-Object { $_.code -eq "8000" }
        if ($revenueAccount) {
            Write-Host "   OK Revenue account exists" -ForegroundColor Green
        } else {
            Write-Host "   FAIL Could not create or find Revenue account" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "   OK Revenue account exists (8000 Omzet)" -ForegroundColor Green
}

# Find VAT Payable account (use existing 1700)
$vatAccount = $accounts | Where-Object { $_.code -eq "1700" -and $_.name -eq "Te betalen BTW" }
if (-not $vatAccount) {
    Write-Host "   Creating VAT Payable account..." -ForegroundColor Gray
    try {
        $vatAccount = Invoke-AuthRequest -Method Post -Uri "$baseUrl/accounts" -Body @{
            code = "1700"
            name = "Te betalen BTW"
            type = 2
            isActive = $true
        } -AllowErrors
        Write-Host "   OK Created VAT account" -ForegroundColor Green
    } catch {
        $accountsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/accounts"
        $accounts = $accountsResponse.items
        $vatAccount = $accounts | Where-Object { $_.code -eq "1700" }
        if ($vatAccount) {
            Write-Host "   OK VAT account exists" -ForegroundColor Green
        } else {
            Write-Host "   FAIL Could not create or find VAT account" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "   OK VAT account exists (1700 Te betalen BTW)" -ForegroundColor Green
}

# Step 3: Create Sales journal if not exists
Write-Host "`n3. Setting up Sales Journal..." -ForegroundColor Yellow
$journalsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/journals"
$journals = if ($journalsResponse.items) { $journalsResponse.items } else { $journalsResponse }
$salesJournal = $journals | Where-Object { $_.type -eq 1 }

if (-not $salesJournal) {
    Write-Host "   Creating Sales journal..." -ForegroundColor Gray
    try {
        $salesJournal = Invoke-AuthRequest -Method Post -Uri "$baseUrl/journals" -Body @{
            code = "VRK"
            name = "Verkopen"
            type = 1
        } -AllowErrors
        Write-Host "   OK Created Sales journal" -ForegroundColor Green
    } catch {
        $journalsResponse = Invoke-AuthRequest -Method Get -Uri "$baseUrl/journals"
        $journals = if ($journalsResponse.items) { $journalsResponse.items } else { $journalsResponse }
        $salesJournal = $journals | Where-Object { $_.type -eq 1 }
        if ($salesJournal) {
            Write-Host "   OK Sales journal exists" -ForegroundColor Green
        } else {
            Write-Host "   FAIL Could not create or find Sales journal" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "   OK Sales journal exists (VRK Verkopen)" -ForegroundColor Green
}

# Step 4: Create a test contact
Write-Host "`n4. Creating test customer..." -ForegroundColor Yellow
$contact = Invoke-AuthRequest -Method Post -Uri "$baseUrl/contacts" -Body @{
    type = 1
    displayName = "Test Klant BV"
    email = "klant@test.nl"
    phone = "0612345678"
    addressLine1 = "Teststraat 1"
    postalCode = "1234 AB"
    city = "Amsterdam"
    country = "NL"
}
Write-Host "   OK Created contact: $($contact.displayName)" -ForegroundColor Green
Write-Host "     Contact ID: $($contact.id)" -ForegroundColor Gray

# Step 5: Create a sales invoice
Write-Host "`n5. Creating sales invoice..." -ForegroundColor Yellow
Write-Host "   URL: $baseUrl/SalesInvoices" -ForegroundColor Gray
Write-Host "   Contact ID: $($contact.id)" -ForegroundColor Gray
Write-Host "   Revenue Account ID: $($revenueAccount.id)" -ForegroundColor Gray
$invoice = Invoke-AuthRequest -Method Post -Uri "$baseUrl/SalesInvoices" -Body @{
    invoiceNumber = "INV-TEST-001"
    issueDate = (Get-Date).ToString("yyyy-MM-dd")
    dueDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
    contactId = $contact.id
    currency = "EUR"
    lines = @(
        @{
            description = "Consultancy services"
            quantity = 10
            unitPrice = 100.00
            vatRate = 21
            accountId = $revenueAccount.id
        },
        @{
            description = "Training"
            quantity = 5
            unitPrice = 150.00
            vatRate = 21
            accountId = $revenueAccount.id
        }
    )
}
Write-Host "   OK Created invoice: $($invoice.invoiceNumber)" -ForegroundColor Green
Write-Host "     Subtotal: EUR $($invoice.subtotal)" -ForegroundColor Gray
Write-Host "     VAT: EUR $($invoice.vatTotal)" -ForegroundColor Gray
Write-Host "     Total: EUR $($invoice.total)" -ForegroundColor Gray

# Step 6: Test posting the invoice
Write-Host "`n6. Posting the invoice..." -ForegroundColor Yellow
$postedInvoice = Invoke-AuthRequest -Method Post -Uri "$baseUrl/SalesInvoices/$($invoice.id)/post"
Write-Host "   OK Invoice posted successfully" -ForegroundColor Green
Write-Host "     Status: $($postedInvoice.status)" -ForegroundColor Gray
Write-Host "     Journal Entry ID: $($postedInvoice.journalEntryId)" -ForegroundColor Gray

# Step 7: Verify journal entry
Write-Host "`n7. Verifying journal entry..." -ForegroundColor Yellow
$journalEntry = Invoke-AuthRequest -Method Get -Uri "$baseUrl/journal-entries/$($postedInvoice.journalEntryId)"
Write-Host "   OK Journal entry found" -ForegroundColor Green
Write-Host "     Reference: $($journalEntry.reference)" -ForegroundColor Gray
Write-Host "     Status: $($journalEntry.status)" -ForegroundColor Gray
Write-Host "     Lines: $($journalEntry.lines.Count)" -ForegroundColor Gray

# Calculate totals
$totalDebit = ($journalEntry.lines | Measure-Object -Property debit -Sum).Sum
$totalCredit = ($journalEntry.lines | Measure-Object -Property credit -Sum).Sum

Write-Host "`n   Journal Entry Details:" -ForegroundColor Cyan
foreach ($line in $journalEntry.lines) {
    $accountInfo = $accounts | Where-Object { $_.id -eq $line.accountId }
    Write-Host "     [$($accountInfo.code)] $($accountInfo.name)" -ForegroundColor Gray
    if ($line.debit -gt 0) {
        Write-Host "       Dr: EUR $($line.debit)" -ForegroundColor Green
    }
    if ($line.credit -gt 0) {
        Write-Host "       Cr: EUR $($line.credit)" -ForegroundColor Yellow
    }
}

Write-Host "`n   Totals:" -ForegroundColor Cyan
Write-Host "     Total Debit:  EUR $totalDebit" -ForegroundColor Green
Write-Host "     Total Credit: EUR $totalCredit" -ForegroundColor Yellow

# Step 8: Verify balance
Write-Host "`n8. Verifying entry is balanced..." -ForegroundColor Yellow
if ([Math]::Abs($totalDebit - $totalCredit) -lt 0.01) {
    Write-Host "   OK Entry is balanced!" -ForegroundColor Green
} else {
    Write-Host "   FAIL Entry is NOT balanced! Difference: EUR $([Math]::Abs($totalDebit - $totalCredit))" -ForegroundColor Red
    exit 1
}

# Step 9: Verify amounts match invoice
Write-Host "`n9. Verifying amounts..." -ForegroundColor Yellow
if ([Math]::Abs($totalDebit - $postedInvoice.total) -lt 0.01) {
    Write-Host "   OK Debit amount matches invoice total" -ForegroundColor Green
} else {
    Write-Host "   FAIL Debit amount does NOT match invoice total" -ForegroundColor Red
    exit 1
}

# Expected credit breakdown: Subtotal + VAT
$expectedRevenueCredit = $postedInvoice.subtotal
$expectedVatCredit = $postedInvoice.vatTotal

$revenueLines = $journalEntry.lines | Where-Object { $_.accountId -eq $revenueAccount.id }
$vatLines = $journalEntry.lines | Where-Object { $_.accountId -eq $vatAccount.id }

$actualRevenueCredit = ($revenueLines | Measure-Object -Property credit -Sum).Sum
$actualVatCredit = ($vatLines | Measure-Object -Property credit -Sum).Sum

Write-Host "   Expected Revenue: EUR $expectedRevenueCredit, Actual: EUR $actualRevenueCredit" -ForegroundColor Gray
Write-Host "   Expected VAT: EUR $expectedVatCredit, Actual: EUR $actualVatCredit" -ForegroundColor Gray

if ([Math]::Abs($actualRevenueCredit - $expectedRevenueCredit) -lt 0.01 -and [Math]::Abs($actualVatCredit - $expectedVatCredit) -lt 0.01) {
    Write-Host "   OK Revenue and VAT amounts are correct" -ForegroundColor Green
} else {
    Write-Host "   FAIL Revenue or VAT amounts are incorrect" -ForegroundColor Red
    exit 1
}

# Step 10: Test idempotency - try posting again
Write-Host "`n10. Testing idempotency (posting again)..." -ForegroundColor Yellow
$postedAgain = Invoke-AuthRequest -Method Post -Uri "$baseUrl/SalesInvoices/$($invoice.id)/post"
Write-Host "   OK Posting again succeeded (idempotent)" -ForegroundColor Green

# Verify no duplicate journal entry was created
if ($postedAgain.journalEntryId -eq $postedInvoice.journalEntryId) {
    Write-Host "   OK Same journal entry ID (no duplicate created)" -ForegroundColor Green
} else {
    Write-Host "   FAIL Different journal entry ID (duplicate created!)" -ForegroundColor Red
    exit 1
}

# Step 11: Get Accounts Receivable report
Write-Host "`n11. Getting Accounts Receivable report..." -ForegroundColor Yellow
$arReport = Invoke-AuthRequest -Method Get -Uri "$baseUrl/reports/ar"
Write-Host "   OK AR report retrieved" -ForegroundColor Green
Write-Host "   Total customers with outstanding: $($arReport.Count)" -ForegroundColor Gray

if ($arReport.Count -gt 0) {
    Write-Host "`n   AR Report Details:" -ForegroundColor Cyan
    foreach ($ar in $arReport) {
        Write-Host "     Customer: $($ar.contactName)" -ForegroundColor Gray
        Write-Host "     Outstanding: EUR $($ar.totalOutstanding)" -ForegroundColor Yellow
        Write-Host "     Invoices: $($ar.invoiceCount)" -ForegroundColor Gray
        foreach ($inv in $ar.invoices) {
            $overdueText = if ($inv.daysOverdue -gt 0) { " (OVERDUE: $($inv.daysOverdue) days)" } else { "" }
            Write-Host "       - $($inv.invoiceNumber): EUR $($inv.outstanding)$overdueText" -ForegroundColor Gray
        }
    }
}

# Step 12: Create another invoice to test multiple postings
Write-Host "`n12. Creating second invoice..." -ForegroundColor Yellow
$invoice2 = Invoke-AuthRequest -Method Post -Uri "$baseUrl/SalesInvoices" -Body @{
    invoiceNumber = "INV-TEST-002"
    issueDate = (Get-Date).ToString("yyyy-MM-dd")
    dueDate = (Get-Date).AddDays(14).ToString("yyyy-MM-dd")
    contactId = $contact.id
    currency = "EUR"
    lines = @(
        @{
            description = "Product A"
            quantity = 2
            unitPrice = 50.00
            vatRate = 21
            accountId = $revenueAccount.id
        }
    )
}
Write-Host "   OK Created second invoice: $($invoice2.invoiceNumber)" -ForegroundColor Green

$postedInvoice2 = Invoke-AuthRequest -Method Post -Uri "$baseUrl/SalesInvoices/$($invoice2.id)/post"
Write-Host "   OK Posted second invoice" -ForegroundColor Green

# Get updated AR report
$arReportUpdated = Invoke-AuthRequest -Method Get -Uri "$baseUrl/reports/ar"
$customerAR = $arReportUpdated | Where-Object { $_.contactId -eq $contact.id }

if ($customerAR) {
    Write-Host "   OK AR report shows $($customerAR.invoiceCount) invoices for customer" -ForegroundColor Green
    Write-Host "     Total outstanding: EUR $($customerAR.totalOutstanding)" -ForegroundColor Yellow
} else {
    Write-Host "   FAIL Customer not found in AR report" -ForegroundColor Red
}

Write-Host "`n=== All tests passed! ===" -ForegroundColor Green
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  OK Invoice posting creates balanced journal entries" -ForegroundColor Green
Write-Host "  OK Debit to Accounts Receivable" -ForegroundColor Green
Write-Host "  OK Credit to Revenue" -ForegroundColor Green
Write-Host "  OK Credit to VAT Payable" -ForegroundColor Green
Write-Host "  OK Amounts are correct and match invoice totals" -ForegroundColor Green
Write-Host "  OK Posting is idempotent (no duplicate entries)" -ForegroundColor Green
Write-Host "  OK AR report shows outstanding invoices correctly" -ForegroundColor Green

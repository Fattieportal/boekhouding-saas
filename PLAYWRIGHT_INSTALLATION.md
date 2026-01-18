# Playwright Installation Guide

## Issue
When trying to run `playwright install chromium` directly, it may not work because:
1. The `playwright.ps1` script needs to be run from the output directory where the DLL is located
2. PowerShell Core (`pwsh`) may not be installed on all systems

## Solution

### Step 1: Build the API Project
```powershell
cd backend
dotnet build src/Api
```

### Step 2: Install Playwright from the API Output Directory
```powershell
cd src\Api\bin\Debug\net8.0
.\playwright.ps1 install chromium
```

**OR** in one command:
```powershell
cd backend
dotnet build src/Api
cd src\Api\bin\Debug\net8.0; .\playwright.ps1 install chromium
```

## Verification

After installation, you should see:
```
Downloading Chromium 130.0.6723.31 (playwright build v1140)...
Chromium downloaded to C:\Users\{user}\AppData\Local\ms-playwright\chromium-1140
```

## Alternative: Use dotnet tool

You can also install Playwright globally:

```powershell
# Install Playwright CLI tool
dotnet tool install --global Microsoft.Playwright.CLI

# Then install browsers
playwright install chromium
```

## Common Issues

### Issue: "playwright.ps1 cannot be loaded"
**Solution**: Enable script execution
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Issue: "Cannot find Microsoft.Playwright.dll"
**Solution**: Make sure you're in the correct output directory after building:
```powershell
cd backend
dotnet build src/Api
cd src\Api\bin\Debug\net8.0
```

### Issue: "pwsh is not recognized"
**Solution**: Use regular PowerShell (`.\playwright.ps1`) instead of PowerShell Core (`pwsh`)

## Testing PDF Generation

After installing Playwright, test the PDF generation:

```powershell
cd backend
.\test-sales-invoices.ps1
```

The test script will:
1. Create a sales invoice
2. Generate a PDF using Playwright
3. Download and open the PDF

## Production Deployment

For production servers:

### Windows Server
```powershell
# On the server after deployment
cd path\to\published\app
.\playwright.ps1 install chromium
```

### Linux/Docker
```dockerfile
# In your Dockerfile
RUN pwsh playwright.ps1 install chromium
# Or use the official Playwright Docker image
FROM mcr.microsoft.com/playwright/dotnet:v1.48.0-jammy
```

### Azure App Service
```bash
# Add to deployment script
az webapp deployment script \
  --name myapp \
  --resource-group mygroup \
  --command "cd site/wwwroot && ./playwright.ps1 install chromium"
```

## Alternative PDF Renderers

If Playwright installation is problematic, you can swap to a different PDF renderer by implementing the `IPdfRenderer` interface:

### Option 1: QuestPDF (No dependencies)
```csharp
// Pure .NET, no browser needed
Install-Package QuestPDF
```

### Option 2: DinkToPdf (wkhtmltopdf wrapper)
```csharp
// Requires wkhtmltopdf binary
Install-Package DinkToPdf
```

### Option 3: IronPdf (Commercial)
```csharp
// Commercial license required
Install-Package IronPdf
```

Just create a new implementation of `IPdfRenderer` and register it in `DependencyInjection.cs`:

```csharp
// Replace PlaywrightPdfRenderer with your implementation
services.AddScoped<IPdfRenderer, YourPdfRenderer>();
```

## Storage Location

Playwright browsers are installed to:
- **Windows**: `C:\Users\{user}\AppData\Local\ms-playwright`
- **Linux**: `~/.cache/ms-playwright`
- **macOS**: `~/Library/Caches/ms-playwright`

Size: ~140 MB per browser

## Troubleshooting

If PDF generation still fails:

1. **Check Playwright version**
   ```powershell
   cd src\Api\bin\Debug\net8.0
   .\playwright.ps1 --version
   ```

2. **Verify browser installation**
   ```powershell
   ls $env:LOCALAPPDATA\ms-playwright
   ```

3. **Test Playwright directly**
   ```csharp
   var playwright = await Playwright.CreateAsync();
   var browser = await playwright.Chromium.LaunchAsync();
   ```

4. **Check logs**
   - Enable debug logging: `$env:DEBUG="pw:api"`
   - Check application logs for PDF generation errors

## Status

✅ Playwright installed successfully  
✅ Chromium browser downloaded  
✅ Ready for PDF generation  

You can now run `.\test-sales-invoices.ps1` to test the complete invoice workflow including PDF generation!

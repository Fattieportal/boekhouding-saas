# Railway Deployment Guide

## Stap 1: Deploy Backend naar Railway

1. **Ga naar Railway.app**
   - https://railway.app
   - Login met GitHub account

2. **Maak nieuw project**
   - Klik "New Project"
   - Kies "Deploy from GitHub repo"
   - Selecteer: `Fattieportal/boekhouding-saas`

3. **Voeg PostgreSQL database toe**
   - In je project, klik "+ New"
   - Selecteer "Database" → "PostgreSQL"
   - Railway maakt automatisch de database aan

4. **Configureer Environment Variables**
   
   Ga naar je backend service → Variables tab:
   
   ```bash
   # Database (wordt automatisch ingevuld door Railway)
   ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
   
   # JWT Settings (BELANGRIJK: genereer een veilige secret!)
   JwtSettings__Secret=GENEREER_HIER_64_RANDOM_CHARACTERS_ABC123XYZ789
   JwtSettings__Issuer=https://boekhouding-saas-production.up.railway.app
   JwtSettings__Audience=https://jouw-app.vercel.app
   JwtSettings__ExpirationMinutes=60
   JwtSettings__RefreshExpirationDays=30
   
   # ASP.NET Core
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}
   
   # CORS (vervang met je Vercel URL)
   AllowedOrigins__0=https://jouw-app.vercel.app
   AllowedOrigins__1=http://localhost:3000
   ```

5. **Genereer veilige JWT Secret**
   
   In PowerShell:
   ```powershell
   -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
   ```
   
   Kopieer de output en gebruik als `JwtSettings__Secret`

6. **Deploy Settings**
   - Root Directory: `/` (laat leeg, Railway vindt automatisch de .NET app)
   - Start Command: Railway detecteert automatisch
   - Klik "Deploy"

## Stap 2: Database Migratie

Na deployment:

1. **Open Railway Terminal**
   - Ga naar je backend service
   - Klik op "..." → "Terminal"

2. **Run migraties**
   ```bash
   cd backend/src/Api
   dotnet ef database update --project ../Infrastructure
   ```

3. **Seed demo data (optioneel)**
   - Dit gebeurt automatisch bij eerste start van de API
   - Check logs om te zien of seeding succesvol was

## Stap 3: Update Frontend Environment Variable

1. **Kopieer je Railway URL**
   - Bijvoorbeeld: `https://boekhouding-saas-production.up.railway.app`

2. **Update Vercel**
   - Ga naar Vercel Dashboard
   - Project Settings → Environment Variables
   - Update `NEXT_PUBLIC_API_URL`:
     ```
     NEXT_PUBLIC_API_URL=https://jouw-project.up.railway.app
     ```
   - Klik "Save"
   - Redeploy frontend (Deployments → ... → Redeploy)

## Stap 4: Update CORS in Backend

Als je de exacte Vercel URL hebt:

1. Update Railway environment variables:
   ```
   AllowedOrigins__0=https://jouw-echte-app.vercel.app
   ```

2. Redeploy backend

## Stap 5: Test de Deployment

```powershell
# Test API health
curl https://jouw-project.up.railway.app/api/health

# Test login
$body = @{ email = "admin@demo.local"; password = "Admin123!" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://jouw-project.up.railway.app/api/auth/login" -Method Post -Body $body -ContentType "application/json"
```

## Kosten

- **Railway**: $5 gratis credit, daarna ~$5-10/maand
- **Vercel**: Gratis voor hobby projecten
- **Totaal**: ~$5-10/maand

## Troubleshooting

### Database connection errors
- Check of `DATABASE_URL` correct is ingesteld
- Verifieer connection string format: `postgresql://user:password@host:port/database`

### CORS errors
- Update `AllowedOrigins` met je exacte Vercel URL
- Include https:// in de URL

### Playwright/PDF errors
- Railway ondersteunt Playwright
- Check logs of browser dependencies geïnstalleerd zijn
- Mogelijk moet je Nixpacks config aanpassen

### Migration errors
- Run migraties handmatig via Railway terminal
- Check database permissions

## Volgende Stappen

✅ Backend draait op Railway
✅ Frontend draait op Vercel
✅ Database draait op Railway PostgreSQL

Nu kun je:
1. Custom domain toevoegen (via Vercel/Railway settings)
2. SSL certificaat configureren (automatisch via Vercel)
3. Monitoring toevoegen (Railway heeft built-in metrics)
4. Backups configureren voor database

## Support Links

- Railway Docs: https://docs.railway.app
- Vercel Docs: https://vercel.com/docs
- PostgreSQL Guide: https://www.postgresql.org/docs

# Deployment README

## Quick Start - Deploy naar Vercel

### Optie 1: Via Vercel Dashboard (Makkelijkst)

1. **Push naar GitHub**
   ```bash
   git init
   git add .
   git commit -m "Ready for deployment"
   git remote add origin https://github.com/[username]/boekhouding-saas.git
   git push -u origin main
   ```

2. **Deploy op Vercel**
   - Ga naar [vercel.com/new](https://vercel.com/new)
   - Import je GitHub repository
   - Root Directory: `frontend`
   - Framework: Next.js (auto-detect)
   - Klik "Deploy"

3. **Environment Variable toevoegen**
   - Project Settings → Environment Variables
   - Add: `NEXT_PUBLIC_API_URL` = `http://localhost:5001` (tijdelijk)
   - Later vervangen door productie backend URL

### Optie 2: Via Vercel CLI

```powershell
# Installeer Vercel CLI
npm i -g vercel

# Login
vercel login

# Deploy (vanuit frontend folder)
cd frontend
vercel

# Production deploy
vercel --prod
```

## Backend Deployment Opties

### Makkelijkste: Railway.app

1. Ga naar [railway.app](https://railway.app)
2. "New Project" → "Deploy from GitHub repo"
3. Select repository
4. Add PostgreSQL database
5. Set environment variables
6. Deploy!

URL: `https://[jouw-project].up.railway.app`

### Azure App Service

```powershell
# Zie VERCEL_DEPLOYMENT.md voor details
az webapp create --name boekhouding-api --runtime "DOTNETCORE:8.0"
```

### Render.com

1. Ga naar [render.com](https://render.com)
2. New → Web Service
3. Connect GitHub
4. Root: `backend/src/Api`
5. Deploy

## Environment Variables

### Frontend (Vercel)
```
NEXT_PUBLIC_API_URL=https://jouw-backend.railway.app
```

### Backend (Railway/Azure/Render)
```
ConnectionStrings__DefaultConnection=Server=...
JwtSettings__Secret=[64-random-chars]
JwtSettings__Issuer=https://jouw-backend.com
JwtSettings__Audience=https://jouw-frontend.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

## Database

### Optie 1: Supabase (Gratis PostgreSQL)
1. [supabase.com](https://supabase.com) → New Project
2. Copy connection string
3. Use in backend env vars

### Optie 2: Railway PostgreSQL
- Automatically available when deploying to Railway

### Optie 3: Azure SQL
```powershell
az sql db create --name boekhouding-db --service-objective S0
```

## Testing Deployment

```powershell
# Test API
curl https://jouw-backend.railway.app/api/health

# Test frontend
# Open browser: https://jouw-app.vercel.app
```

## Kosten

**Gratis Optie:**
- Vercel: Gratis
- Railway: $5 gratis/maand
- Supabase: Gratis

**Budget Optie (~€18/maand):**
- Vercel: Gratis
- Azure App Service: ~€13/maand
- Azure SQL: ~€5/maand

## Support

Zie [VERCEL_DEPLOYMENT.md](VERCEL_DEPLOYMENT.md) voor gedetailleerde instructies!

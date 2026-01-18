# Vercel Deployment Guide - Boekhouding SaaS

## 🚀 Frontend Deployment naar Vercel

### Stap 1: Voorbereiding

1. **Maak een Vercel account aan**
   - Ga naar https://vercel.com
   - Sign up met je GitHub account (aanbevolen)

2. **Install Vercel CLI** (optioneel, voor lokale preview)
   ```powershell
   npm install -g vercel
   ```

### Stap 2: Project Setup

1. **Push je code naar GitHub**
   ```powershell
   cd c:\Users\Gslik\OneDrive\Documents\boekhouding-saas
   
   # Initialize git (als nog niet gedaan)
   git init
   git add .
   git commit -m "Initial commit - Boekhouding SaaS"
   
   # Create GitHub repo en push
   # (via GitHub website: New Repository)
   git remote add origin https://github.com/[jouw-username]/boekhouding-saas.git
   git branch -M main
   git push -u origin main
   ```

2. **Environment Variables voorbereiden**
   
   De frontend heeft deze environment variable nodig:
   - `NEXT_PUBLIC_API_URL` - URL naar je backend API

### Stap 3: Deploy via Vercel Dashboard

1. **Login op Vercel**
   - Ga naar https://vercel.com/dashboard

2. **Import Project**
   - Klik "Add New..." → "Project"
   - Selecteer je GitHub repository
   - Framework: Next.js (auto-detected)
   - Root Directory: `frontend`

3. **Configure Project**
   ```
   Build Command: npm run build
   Output Directory: .next
   Install Command: npm install
   ```

4. **Environment Variables**
   
   Voeg toe in Vercel project settings:
   ```
   NEXT_PUBLIC_API_URL = https://jouw-backend-api.com
   ```

5. **Deploy!**
   - Klik "Deploy"
   - Vercel zal automatisch bouwen en deployen

### Stap 4: Backend Deployment

⚠️ **Belangrijk**: Je hebt ook een backend nodig! Opties:

#### Optie A: Azure App Service (aanbevolen voor .NET)

```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Login
az login

# Create resource group
az group create --name boekhouding-rg --location westeurope

# Create App Service plan
az appservice plan create --name boekhouding-plan --resource-group boekhouding-rg --sku B1

# Create web app
az webapp create --name boekhouding-api --resource-group boekhouding-rg --plan boekhouding-plan --runtime "DOTNETCORE:8.0"

# Deploy
cd backend/src/Api
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip --resource-group boekhouding-rg --name boekhouding-api --src ./publish.zip
```

Backend URL wordt dan: `https://boekhouding-api.azurewebsites.net`

#### Optie B: Railway.app (makkelijker, gratis tier)

1. Ga naar https://railway.app
2. Connect GitHub repo
3. Selecteer `backend` folder
4. Add PostgreSQL database
5. Set environment variables
6. Deploy!

#### Optie C: Render.com (ook gratis tier)

1. Ga naar https://render.com
2. New → Web Service
3. Connect repository
4. Root: `backend/src/Api`
5. Build: `dotnet publish -c Release -o out`
6. Start: `dotnet out/Api.dll`

### Stap 5: Database Setup

Je hebt ook een productie database nodig:

#### Optie 1: Azure SQL Database
```powershell
az sql server create --name boekhouding-sql --resource-group boekhouding-rg --location westeurope --admin-user sqladmin --admin-password [SterkWachtwoord123!]

az sql db create --resource-group boekhouding-rg --server boekhouding-sql --name boekhouding-db --service-objective S0
```

#### Optie 2: Supabase (gratis PostgreSQL)
1. Ga naar https://supabase.com
2. New Project
3. Kopieer connection string
4. Use in backend environment variables

### Stap 6: Environment Variables voor Backend

Backend needs (in Azure/Railway/Render):

```env
ConnectionStrings__DefaultConnection=Server=...;Database=...;User Id=...;Password=...
JwtSettings__Secret=[random-64-char-string]
JwtSettings__Issuer=https://jouw-backend-api.com
JwtSettings__Audience=https://jouw-frontend.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

### Stap 7: CORS Setup

Update backend `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.WithOrigins("https://jouw-app.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Later in file:
app.UseCors("AllowVercel");
```

### Stap 8: Update Frontend na Backend Deployment

1. Ga naar Vercel Dashboard
2. Project Settings → Environment Variables
3. Update `NEXT_PUBLIC_API_URL` naar je backend URL
4. Redeploy (Deployments → ... → Redeploy)

---

## 🎯 Quick Deploy (als je al alles hebt)

### Via Vercel CLI:

```powershell
cd frontend
vercel

# Follow prompts:
# - Link to existing project? No
# - Project name: boekhouding-saas
# - Which directory: ./
# - Override settings? No

# Production deploy:
vercel --prod
```

---

## 📋 Checklist

- [ ] GitHub repository aangemaakt
- [ ] Code gepushed naar GitHub
- [ ] Backend deployed (Azure/Railway/Render)
- [ ] Database provisioned (Azure SQL/Supabase)
- [ ] Database migrations gedraaid
- [ ] Backend environment variables ingesteld
- [ ] Backend CORS geconfigureerd
- [ ] Frontend deployed op Vercel
- [ ] Frontend environment variables ingesteld
- [ ] Test de deployed app!

---

## 🔧 Troubleshooting

### "Failed to compile"
- Check build logs in Vercel dashboard
- Zorg dat alle dependencies in package.json staan
- Check TypeScript errors

### "API not reachable"
- Check NEXT_PUBLIC_API_URL in Vercel env vars
- Check backend CORS settings
- Check backend is running (visit API URL in browser)

### "Database connection failed"
- Check connection string in backend env vars
- Check database firewall rules (allow Azure services)
- Run migrations: `dotnet ef database update`

---

## 💰 Kosten Overzicht

### Gratis Tier Optie:
- **Vercel**: Gratis (Hobby plan)
- **Railway**: $5/maand gratis credit
- **Supabase**: Gratis (2 projecten)
- **Total**: ~$0-5/maand

### Budget Optie:
- **Vercel**: Gratis
- **Azure App Service**: ~€13/maand (B1 tier)
- **Azure SQL**: ~€5/maand (Basic tier)
- **Total**: ~€18/maand

### Production Ready:
- **Vercel Pro**: $20/maand
- **Azure App Service**: ~€50/maand (S1 tier)
- **Azure SQL**: ~€13/maand (S0 tier)
- **Total**: ~€83/maand

---

## 🚦 Next Steps

1. **Start with Free Tier**
   - Deploy frontend op Vercel (gratis)
   - Deploy backend op Railway (gratis $5 credit)
   - Use Supabase database (gratis)

2. **Test Everything**
   - Create test account
   - Create test invoice
   - Generate PDF
   - Test all features

3. **Monitor**
   - Vercel Analytics
   - Railway logs
   - Supabase dashboard

4. **Scale Up When Needed**
   - Vercel Pro voor custom domain
   - Azure voor enterprise features
   - Add CDN voor performance

---

## 📚 Resources

- [Vercel Documentation](https://vercel.com/docs)
- [Next.js Deployment](https://nextjs.org/docs/deployment)
- [Azure App Service](https://learn.microsoft.com/azure/app-service/)
- [Railway Docs](https://docs.railway.app/)
- [Supabase Docs](https://supabase.com/docs)

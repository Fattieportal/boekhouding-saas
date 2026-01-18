# Boekhouding SaaS - Monorepo

Een moderne boekhoudapplicatie gebouwd met ASP.NET Core Web API en Next.js.

## 📁 Projectstructuur

```
boekhouding-saas/
├── backend/                # ASP.NET Core Web API (.NET 8)
│   ├── src/
│   │   ├── Api/           # Presentation Layer
│   │   ├── Application/   # Application Layer
│   │   ├── Domain/        # Domain Layer
│   │   └── Infrastructure/# Infrastructure Layer
│   └── tests/
├── frontend/              # Next.js TypeScript app
├── infra/                 # Docker compose & infrastructure
└── README.md
```

## 🚀 Aan de slag

### Vereisten

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Database starten (PostgreSQL + pgAdmin)

```powershell
cd infra
docker-compose up -d
```

De database is beschikbaar op:
- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:5050` (email: `admin@admin.com`, wachtwoord: `admin`)

### 2. Backend starten

```powershell
cd backend/src/Api
dotnet restore
dotnet ef database update --project ../Infrastructure
dotnet run
```

De API is beschikbaar op:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5001`
- Swagger UI: `https://localhost:7001/swagger`
- Health check: `https://localhost:7001/health`

### 3. Frontend starten

```powershell
cd frontend
npm install
npm run dev
```

De web app is beschikbaar op:
- `http://localhost:3000`

## 🛠️ Ontwikkeling

### Backend

#### Database migrations maken

```powershell
cd backend/src/Api
dotnet ef migrations add MigrationNaam --project ../Infrastructure --startup-project .
```

#### Database updaten

```powershell
cd backend/src/Api
dotnet ef database update --project ../Infrastructure
```

#### Tests draaien

```powershell
cd backend
dotnet test
```

### Frontend

#### Build

```powershell
cd frontend
npm run build
```

#### Lint

```powershell
cd frontend
npm run lint
```

## 🔧 Configuratie

### Backend

Pas `backend/src/Api/appsettings.Development.json` aan of gebruik omgevingsvariabelen:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"
```

### Frontend

Maak een `.env.local` bestand in de `frontend/` map:

```
NEXT_PUBLIC_API_URL=https://localhost:7001
```

## 📦 Production deployment

### Backend build

```powershell
cd backend/src/Api
dotnet publish -c Release -o ./publish
```

### Frontend build

```powershell
cd frontend
npm run build
npm start
```

## 📝 Licentie

Proprietary

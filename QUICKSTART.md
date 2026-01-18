# 🚀 Quick Start Guide - Boekhouding SaaS

Deze guide helpt je om de complete applicatie lokaal op te zetten en te draaien.

## ✅ Vereisten checklist

Voordat je begint, zorg dat je de volgende tools geïnstalleerd hebt:

- [ ] [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ ] [Node.js 18+](https://nodejs.org/)
- [ ] [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [ ] Een code editor (bijv. Visual Studio Code)

## 📋 Stap voor stap setup

### Stap 1: Database starten

Open een PowerShell terminal en navigeer naar de `infra` map:

```powershell
cd infra
docker-compose up -d
```

✅ **Verificatie**: Ga naar http://localhost:5050 - je zou pgAdmin moeten zien.

### Stap 2: Backend configureren en starten

Open een **nieuwe** PowerShell terminal:

```powershell
# Navigeer naar de API map
cd backend/src/Api

# Restore dependencies (indien nog niet gedaan)
dotnet restore

# Voer database migrations uit
dotnet ef database update --project ../Infrastructure

# Start de API
dotnet run
```

✅ **Verificatie**: 
- Ga naar https://localhost:7001/health - je zou een JSON response moeten zien
- Ga naar https://localhost:7001/swagger - je zou de API documentatie moeten zien

### Stap 3: Frontend starten

Open een **nieuwe** PowerShell terminal:

```powershell
# Navigeer naar de frontend map
cd frontend

# Installeer dependencies (indien nog niet gedaan)
npm install

# Start de development server
npm run dev
```

✅ **Verificatie**: Ga naar http://localhost:3000 - je zou de applicatie moeten zien.

## 🎉 Klaar!

Als alles goed is gegaan, heb je nu:

- ✅ PostgreSQL database draait op poort 5432
- ✅ pgAdmin draait op http://localhost:5050
- ✅ Backend API draait op https://localhost:7001
- ✅ Swagger UI beschikbaar op https://localhost:7001/swagger
- ✅ Frontend web app draait op http://localhost:3000

## 🧪 Test de applicatie

### 1. Maak een test klant via Swagger

1. Ga naar https://localhost:7001/swagger
2. Klik op `POST /api/Klanten`
3. Klik op "Try it out"
4. Vervang de request body met:

```json
{
  "naam": "Test Bedrijf BV",
  "email": "info@testbedrijf.nl",
  "telefoonnummer": "020-1234567",
  "adres": "Teststraat 123",
  "postcode": "1234 AB",
  "plaats": "Amsterdam",
  "btwNummer": "NL123456789B01",
  "kvkNummer": "12345678",
  "isActief": true
}
```

5. Klik op "Execute"

### 2. Verifieer in de frontend

Ga naar http://localhost:3000 en ververs de pagina. Je zou de nieuwe klant moeten zien in de lijst.

### 3. Bekijk in de database

1. Ga naar http://localhost:5050
2. Login met:
   - Email: `admin@admin.com`
   - Password: `admin`
3. Voeg een server toe:
   - Name: `Boekhouding Local`
   - Host: `postgres` (of `host.docker.internal` op Windows/Mac)
   - Port: `5432`
   - Database: `boekhouding`
   - Username: `postgres`
   - Password: `postgres`
4. Browse naar Databases → boekhouding → Schemas → public → Tables → Klanten

## 🛠️ Handige commando's

### Database

```powershell
# Stop database
cd infra
docker-compose down

# Start database opnieuw
docker-compose up -d

# Reset database (verwijdert alle data!)
docker-compose down -v
docker-compose up -d
```

### Backend

```powershell
cd backend/src/Api

# Voer migrations uit
dotnet ef database update --project ../Infrastructure

# Maak een nieuwe migration
dotnet ef migrations add MigrationNaam --project ../Infrastructure

# Verwijder laatste migration
dotnet ef migrations remove --project ../Infrastructure

# Run tests
cd ../..
dotnet test
```

### Frontend

```powershell
cd frontend

# Development mode met hot reload
npm run dev

# Production build
npm run build
npm start

# Lint code
npm run lint
```

## ⚠️ Troubleshooting

### "Cannot connect to database"

- Zorg dat Docker Desktop draait
- Controleer of de database container draait: `docker ps`
- Herstart de database: `cd infra; docker-compose restart`

### "Port already in use"

- Backend (poort 7001/5001): Stop andere .NET applicaties
- Frontend (poort 3000): Stop andere Next.js apps of wijzig de poort met `npm run dev -- -p 3001`
- Database (poort 5432): Wijzig de poort in `infra/docker-compose.yml`

### "SSL certificate error" in browser

- Accepteer het zelf-ondertekende certificaat in je browser
- Of gebruik de HTTP variant: http://localhost:5001

### "npm install" faalt

- Verwijder `node_modules` en `package-lock.json`
- Run `npm install` opnieuw

## 📚 Volgende stappen

- Bekijk de [Backend README](backend/README.md) voor meer details over de API
- Bekijk de [Frontend README](frontend/README.md) voor frontend development
- Bekijk de [Infra README](infra/README.md) voor database configuratie
- Lees de [hoofdlijnen README](README.md) voor projectstructuur

## 💡 Tips

1. **Gebruik meerdere terminals**: Eén voor backend, één voor frontend, één voor ad-hoc commando's
2. **Watch de logs**: Houd de terminals open om errors en warnings te zien
3. **Hot reload**: Wijzigingen in de code worden automatisch herladen
4. **Swagger voor API testing**: Gebruik Swagger UI voor snelle API tests
5. **Database backups**: Maak regelmatig backups van je development data

Veel plezier met bouwen! 🎉

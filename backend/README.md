# Boekhouding SaaS - Backend

ASP.NET Core Web API met Clean Architecture voor de boekhoudapplicatie.

## 🏗️ Architectuur

Het project volgt **Clean Architecture** principes met de volgende lagen:

### Domain Layer (`Domain/`)
- **Verantwoordelijkheid**: Business entities en domain logic
- **Dependencies**: Geen externe dependencies
- **Bevat**: 
  - `Entities/`: Domain entiteiten (Klant, Factuur, FactuurRegel)
  - `Common/`: Gedeelde base classes

### Application Layer (`Application/`)
- **Verantwoordelijkheid**: Business logic en use cases
- **Dependencies**: Domain layer
- **Bevat**:
  - Service interfaces
  - DTOs en mapping configuraties
  - Validation logic

### Infrastructure Layer (`Infrastructure/`)
- **Verantwoordelijkheid**: Data access en externe services
- **Dependencies**: Domain, Application
- **Bevat**:
  - `Data/`: EF Core DbContext en configurations
  - Repository implementaties
  - External service implementaties

### API Layer (`Api/`)
- **Verantwoordelijkheid**: HTTP endpoints en request/response handling
- **Dependencies**: Application, Infrastructure
- **Bevat**:
  - Controllers
  - Middleware
  - API configuratie

## 🗃️ Database Schema

### Klanten
Klantgegevens voor facturatie.

| Kolom | Type | Beschrijving |
|-------|------|--------------|
| Id | Guid | Primary key |
| Naam | string | Bedrijfsnaam (required) |
| Email | string | Email adres |
| Telefoonnummer | string | Telefoonnummer |
| Adres | string | Straat + huisnummer |
| Postcode | string | Postcode |
| Plaats | string | Plaatsnaam |
| BTWNummer | string | BTW identificatienummer |
| KVKNummer | string | KvK nummer |
| IsActief | bool | Actieve klant |
| CreatedAt | DateTime | Aanmaakdatum |
| UpdatedAt | DateTime? | Laatste wijziging |

### Facturen
Factuurgegevens.

| Kolom | Type | Beschrijving |
|-------|------|--------------|
| Id | Guid | Primary key |
| Factuurnummer | string | Uniek factuurnummer |
| Factuurdatum | DateTime | Factuurdatum |
| Vervaldatum | DateTime? | Vervaldatum |
| KlantId | Guid | Foreign key naar Klanten |
| TotaalExclBTW | decimal | Totaal excl. BTW |
| BTWBedrag | decimal | BTW bedrag |
| TotaalInclBTW | decimal | Totaal incl. BTW |
| Status | enum | Concept/Verzonden/Betaald/Vervallen/Geannuleerd |
| Opmerkingen | string | Extra opmerkingen |
| CreatedAt | DateTime | Aanmaakdatum |
| UpdatedAt | DateTime? | Laatste wijziging |

### FactuurRegels
Regels op een factuur.

| Kolom | Type | Beschrijving |
|-------|------|--------------|
| Id | Guid | Primary key |
| FactuurId | Guid | Foreign key naar Facturen |
| Omschrijving | string | Omschrijving |
| Aantal | int | Aantal |
| PrijsPerStuk | decimal | Prijs per stuk |
| BTWPercentage | decimal | BTW percentage |
| TotaalExclBTW | decimal | Totaal excl. BTW |
| BTWBedrag | decimal | BTW bedrag |
| TotaalInclBTW | decimal | Totaal incl. BTW |
| CreatedAt | DateTime | Aanmaakdatum |
| UpdatedAt | DateTime? | Laatste wijziging |

## 🔌 API Endpoints

### Health Check
- `GET /health` - API health status

### Klanten
- `GET /api/klanten` - Haal alle actieve klanten op
- `GET /api/klanten/{id}` - Haal specifieke klant op
- `POST /api/klanten` - Maak nieuwe klant aan
- `PUT /api/klanten/{id}` - Update klant
- `DELETE /api/klanten/{id}` - Deactiveer klant (soft delete)

## 🛠️ Development

### Installatie

```powershell
cd backend/src/Api
dotnet restore
```

### Database Migrations

Maak een nieuwe migration:
```powershell
dotnet ef migrations add MigrationNaam --project ../Infrastructure --startup-project .
```

Voer migrations uit:
```powershell
dotnet ef database update --project ../Infrastructure --startup-project .
```

Verwijder laatste migration:
```powershell
dotnet ef migrations remove --project ../Infrastructure --startup-project .
```

### Run

```powershell
dotnet run
```

De API is beschikbaar op:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001
- Swagger: https://localhost:7001/swagger

### Tests

```powershell
cd backend
dotnet test
```

## 🔧 Configuratie

### Connection String

De connection string kan op 3 manieren worden geconfigureerd:

1. **appsettings.Development.json** (standaard):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"
  }
}
```

2. **Omgevingsvariabele**:
```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"
dotnet run
```

3. **User Secrets** (aanbevolen voor development):
```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=boekhouding;Username=postgres;Password=postgres"
```

### Logging

Pas log levels aan in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## 📦 NuGet Packages

Belangrijke packages:
- `Microsoft.EntityFrameworkCore` - ORM
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI
- `Microsoft.AspNetCore.OpenApi` - OpenAPI support

## 🚀 Production

### Build

```powershell
dotnet publish -c Release -o ./publish
```

### Run in container

Maak een `Dockerfile` in de `backend/` map:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
```

Build en run:
```powershell
docker build -t boekhouding-api .
docker run -p 8080:80 boekhouding-api
```

## 📝 Best Practices

1. **Gebruik async/await**: Alle database operaties zijn async
2. **Validation**: Voeg FluentValidation toe voor input validation
3. **Exception handling**: Implementeer global exception handling middleware
4. **Logging**: Log belangrijke events en errors
5. **DTOs**: Gebruik DTOs in plaats van entities in API responses
6. **Authorization**: Voeg authenticatie en autorisatie toe (JWT)
7. **Rate limiting**: Implementeer rate limiting voor productie
8. **Health checks**: Gebruik health checks voor monitoring

## 🔐 Security Checklist

Voor productie:
- [ ] Implementeer authenticatie (JWT/OAuth)
- [ ] Voeg autorisatie toe (role-based/policy-based)
- [ ] Gebruik HTTPS alleen
- [ ] Configureer CORS correct
- [ ] Valideer alle input
- [ ] Gebruik prepared statements (EF Core doet dit automatisch)
- [ ] Implementeer rate limiting
- [ ] Configureer security headers
- [ ] Gebruik secrets management (Azure Key Vault, etc.)
- [ ] Enable audit logging

## 📚 Verder ontwikkelen

Suggesties voor uitbreiding:
- [ ] MediatR voor CQRS pattern
- [ ] AutoMapper voor object mapping
- [ ] FluentValidation voor validatie
- [ ] Serilog voor structured logging
- [ ] Redis voor caching
- [ ] Hangfire voor background jobs
- [ ] SignalR voor real-time updates
- [ ] Unit en integration tests
- [ ] API versioning
- [ ] GraphQL endpoint

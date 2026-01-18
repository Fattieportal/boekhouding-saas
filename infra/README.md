# Infrastructure

Docker Compose configuratie voor lokale ontwikkeling.

## Services

### PostgreSQL
- **Image**: postgres:16-alpine
- **Port**: 5432
- **Database**: boekhouding
- **User**: postgres
- **Password**: postgres

### pgAdmin
- **Image**: dpage/pgadmin4
- **Port**: 5050
- **URL**: http://localhost:5050
- **Email**: admin@admin.com
- **Password**: admin

## Gebruik

Start alle services:

```powershell
docker-compose up -d
```

Stop alle services:

```powershell
docker-compose down
```

Stop en verwijder volumes (data wordt gewist):

```powershell
docker-compose down -v
```

## pgAdmin configuratie

1. Open http://localhost:5050
2. Login met `admin@admin.com` / `admin`
3. Voeg een nieuwe server toe:
   - Name: Boekhouding Local
   - Host: postgres (of host.docker.internal op Windows/Mac)
   - Port: 5432
   - Database: boekhouding
   - Username: postgres
   - Password: postgres

# Multi-stage build for .NET 8 application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY backend/Boekhouding.sln ./
COPY backend/src/Api/Api.csproj ./src/Api/
COPY backend/src/Application/Application.csproj ./src/Application/
COPY backend/src/Domain/Domain.csproj ./src/Domain/
COPY backend/src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY backend/src ./src

# Build and publish
WORKDIR /src/src/Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Playwright dependencies for PDF generation
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libdbus-1-3 \
    libxkbcommon0 \
    libatspi2.0-0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Api.dll"]

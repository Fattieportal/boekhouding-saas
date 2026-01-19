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

# Install PowerShell for Playwright installation
RUN apt-get update && apt-get install -y wget apt-transport-https software-properties-common \
    && wget -q "https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb" \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y powershell \
    && rm packages-microsoft-prod.deb

# Install Playwright dependencies for PDF generation
RUN apt-get install -y \
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
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxext6 \
    fonts-liberation \
    libappindicator3-1 \
    libglib2.0-0 \
    libgtk-3-0 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Install Playwright browsers (Chromium only for PDF generation)
RUN pwsh /app/playwright.ps1 install chromium --with-deps

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Api.dll"]

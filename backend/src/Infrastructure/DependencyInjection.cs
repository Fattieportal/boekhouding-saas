using Boekhouding.Application.Interfaces;
using Boekhouding.Infrastructure.Data;
using Boekhouding.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boekhouding.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database configuratie - support both Railway DATABASE_URL and ConnectionStrings format
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"] // Railway fallback
            ?? Environment.GetEnvironmentVariable("DATABASE_URL") // Direct env var fallback
            ?? throw new InvalidOperationException("Connection string not found. Set either 'ConnectionStrings:DefaultConnection' or 'DATABASE_URL'.");

        // Railway sometimes includes the variable name in the value - strip if present
        if (connectionString.StartsWith("DATABASE_URL="))
        {
            connectionString = connectionString.Substring("DATABASE_URL=".Length);
            Console.WriteLine("🔧 [Infrastructure] Stripped 'DATABASE_URL=' prefix from connection string");
        }

        // Convert PostgreSQL URL format to Npgsql connection string format
        // From: postgresql://user:password@host:port/database
        // To: Host=host;Port=port;Database=database;Username=user;Password=password
        if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
        {
            try
            {
                var uri = new Uri(connectionString);
                connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]}";
                Console.WriteLine("🔧 [Infrastructure] Converted PostgreSQL URL to Npgsql connection string format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  [Infrastructure] Failed to parse DATABASE_URL: {ex.Message}");
                Console.WriteLine("🔧 [Infrastructure] Using raw DATABASE_URL value as fallback");
            }
        }

        // Log connection string (masking password for security)
        var maskedConnectionString = connectionString.Contains("Password=") 
            ? System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]+", "Password=***")
            : connectionString.Contains(":") && connectionString.Contains("@")
                ? System.Text.RegularExpressions.Regex.Replace(connectionString, @"://[^:]+:([^@]+)@", "://*****:***@")
                : connectionString;
        Console.WriteLine($"🔌 Using connection string: {maskedConnectionString}");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        // Registreer repositories en services hier
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();
        services.AddScoped<ITenantBrandingService, TenantBrandingService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IFinancialReportService, FinancialReportService>();
        
        // Template rendering and PDF generation
        services.AddScoped<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddScoped<IPdfRenderer, PlaywrightPdfRenderer>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        
        // Bank integration
        services.AddDataProtection(); // Voor token encryption
        services.AddScoped<IBankProvider, MockBankProvider>();
        services.AddScoped<IBankService, BankService>();
        
        // Audit logging
        services.AddScoped<IAuditLogService, AuditLogService>();
        
        // VAT & Compliance
        services.AddScoped<IVATService, VATService>();
        services.AddScoped<IYearEndService, YearEndService>();
        
        return services;
    }
}

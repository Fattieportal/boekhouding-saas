using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Boekhouding.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed admin user if not exists
            if (!await context.Users.AnyAsync(u => u.Email == "admin@local.test"))
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@local.test",
                    // Password: Admin123!
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = Role.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded admin user: admin@local.test");
            }

            // Seed test accountant user if in development
            if (!await context.Users.AnyAsync(u => u.Email == "accountant@local.test"))
            {
                var accountantUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "accountant@local.test",
                    // Password: Accountant123!
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Accountant123!"),
                    Role = Role.Accountant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(accountantUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded accountant user: accountant@local.test");
            }

            // Seed test viewer user if in development
            if (!await context.Users.AnyAsync(u => u.Email == "viewer@local.test"))
            {
                var viewerUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "viewer@local.test",
                    // Password: Viewer123!
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Viewer123!"),
                    Role = Role.Viewer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(viewerUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded viewer user: viewer@local.test");
            }

            // Seed UserTenant relationships - Link all users to all tenants for development
            await SeedUserTenantsAsync(context, logger);

            // Seed accounts and journals for all tenants
            await SeedAccountingDataAsync(context, logger);

            // Seed contacts for all tenants
            await SeedContactsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedUserTenantsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get all users
        var users = await context.Users.ToListAsync();
        
        // Get all tenants
        var tenants = await context.Tenants.ToListAsync();

        // For development, link all users to all tenants
        foreach (var user in users)
        {
            foreach (var tenant in tenants)
            {
                // Check if relationship already exists
                var exists = await context.Set<UserTenant>()
                    .AnyAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);

                if (!exists)
                {
                    var userTenant = new UserTenant
                    {
                        UserId = user.Id,
                        TenantId = tenant.Id,
                        Role = user.Role == Role.Admin ? TenantRole.Admin :
                                    user.Role == Role.Accountant ? TenantRole.Accountant :
                                    TenantRole.Viewer,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Set<UserTenant>().Add(userTenant);
                }
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Seeded UserTenant relationships: {users.Count} users x {tenants.Count} tenants");
    }

    private static async Task SeedAccountingDataAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get all tenants - disable query filter to see all tenants
        var tenants = await context.Tenants.ToListAsync();

        foreach (var tenant in tenants)
        {
            // Skip if tenant already has accounts - disable query filter
            var hasAccounts = await context.Accounts
                .IgnoreQueryFilters()
                .AnyAsync(a => a.TenantId == tenant.Id);
            
            if (hasAccounts)
            {
                continue;
            }

            // Seed default accounts (minimal NL chart)
            var accounts = new List<Account>
            {
                // Assets (Activa)
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "1100",
                    Name = "Debiteuren",
                    Type = AccountType.Asset,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "1300",
                    Name = "Bank",
                    Type = AccountType.Asset,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Liabilities (Passiva)
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "1600",
                    Name = "Crediteuren",
                    Type = AccountType.Liability,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "1700",
                    Name = "Te betalen BTW",
                    Type = AccountType.Liability,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Equity (Eigen vermogen)
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "0100",
                    Name = "Eigen vermogen",
                    Type = AccountType.Equity,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Revenue (Opbrengsten)
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "8000",
                    Name = "Omzet",
                    Type = AccountType.Revenue,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Expenses (Kosten)
                new Account
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "4000",
                    Name = "Kosten",
                    Type = AccountType.Expense,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Accounts.AddRange(accounts);

            // Seed default journals
            var journals = new List<Journal>
            {
                new Journal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "VRK",
                    Name = "Verkopen",
                    Type = JournalType.Sales,
                    CreatedAt = DateTime.UtcNow
                },
                new Journal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "INK",
                    Name = "Inkopen",
                    Type = JournalType.Purchase,
                    CreatedAt = DateTime.UtcNow
                },
                new Journal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "BANK",
                    Name = "Bank",
                    Type = JournalType.Bank,
                    CreatedAt = DateTime.UtcNow
                },
                new Journal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Code = "MEM",
                    Name = "Memoriaal",
                    Type = JournalType.General,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Journals.AddRange(journals);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded {accounts.Count} accounts and {journals.Count} journals for tenant {tenant.Name}");
        }
    }

    private static async Task SeedContactsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get all tenants - disable query filter to see all tenants
        var tenants = await context.Tenants.ToListAsync();

        foreach (var tenant in tenants)
        {
            // Skip if tenant already has contacts - disable query filter
            var hasContacts = await context.Contacts
                .IgnoreQueryFilters()
                .AnyAsync(c => c.TenantId == tenant.Id);
            
            if (hasContacts)
            {
                continue;
            }

            // Seed example contacts
            var contacts = new List<Contact>
            {
                // Customers
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Customer,
                    DisplayName = "Acme Corporation",
                    Email = "info@acme.nl",
                    Phone = "+31 20 123 4567",
                    AddressLine1 = "Hoofdstraat 123",
                    AddressLine2 = null,
                    PostalCode = "1012 AB",
                    City = "Amsterdam",
                    Country = "NL",
                    VatNumber = "NL123456789B01",
                    KvK = "12345678",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Customer,
                    DisplayName = "TechStart BV",
                    Email = "contact@techstart.nl",
                    Phone = "+31 30 987 6543",
                    AddressLine1 = "Innovatieweg 45",
                    AddressLine2 = "Unit 2B",
                    PostalCode = "3542 AB",
                    City = "Utrecht",
                    Country = "NL",
                    VatNumber = "NL987654321B01",
                    KvK = "87654321",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Suppliers
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Supplier,
                    DisplayName = "Office Supplies Nederland",
                    Email = "verkoop@officesupplies.nl",
                    Phone = "+31 10 555 1234",
                    AddressLine1 = "Industrieweg 78",
                    AddressLine2 = null,
                    PostalCode = "3011 CD",
                    City = "Rotterdam",
                    Country = "NL",
                    VatNumber = "NL555123456B01",
                    KvK = "55512345",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Supplier,
                    DisplayName = "CloudHost Services",
                    Email = "billing@cloudhost.com",
                    Phone = "+31 70 444 5678",
                    AddressLine1 = "Serverstraat 12",
                    AddressLine2 = null,
                    PostalCode = "2511 AB",
                    City = "Den Haag",
                    Country = "NL",
                    VatNumber = "NL444567890B01",
                    KvK = "44456789",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Both Customer and Supplier
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Both,
                    DisplayName = "Software Solutions Group",
                    Email = "info@softwaregroup.nl",
                    Phone = "+31 40 333 2211",
                    AddressLine1 = "High Tech Campus 5",
                    AddressLine2 = "Building 42",
                    PostalCode = "5656 AE",
                    City = "Eindhoven",
                    Country = "NL",
                    VatNumber = "NL333221100B01",
                    KvK = "33322110",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Individual customer (no VAT/KvK)
                new Contact
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Type = ContactType.Customer,
                    DisplayName = "Jan Jansen",
                    Email = "jan.jansen@email.nl",
                    Phone = "+31 6 12345678",
                    AddressLine1 = "Kerkstraat 56",
                    AddressLine2 = null,
                    PostalCode = "1234 AB",
                    City = "Haarlem",
                    Country = "NL",
                    VatNumber = null,
                    KvK = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Contacts.AddRange(contacts);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded {contacts.Count} contacts for tenant {tenant.Name}");
        }
        
        // Also seed demo data
        await DemoSeeder.SeedDemoDataAsync(scope.ServiceProvider);
    }
}


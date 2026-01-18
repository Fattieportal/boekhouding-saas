using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Boekhouding.Infrastructure.Data;

/// <summary>
/// Demo data seeder voor development environment
/// Maakt een complete demo tenant met realistische data
/// </summary>
public static class DemoSeeder
{
    private static Guid _demoTenantId = Guid.Empty;
    private static Guid _adminUserId = Guid.Empty;
    private static Guid _accountantUserId = Guid.Empty;
    private static Contact _acmeContact = null!;
    private static Contact _techStartContact = null!;

    public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("🌱 Starting Demo Data Seeding...");

            // Check if demo tenant already exists
            var existingDemoTenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Name == "Demo Company BV");

            if (existingDemoTenant != null)
            {
                logger.LogInformation("✅ Demo tenant already exists. Skipping demo seeding.");
                return;
            }

            // B2: Seed demo users
            await SeedDemoUsersAsync(context, logger);

            // B3: Seed demo tenant + branding + template
            await SeedDemoTenantAsync(context, logger);

            // B4: Seed accounting data (accounts + journals) for demo tenant
            await SeedDemoAccountingAsync(context, logger);

            // B5: Seed contacts + invoices
            await SeedDemoContactsAsync(context, logger);
            await SeedDemoInvoicesAsync(context, serviceProvider, logger);

            // B6: Seed bank connection + transactions
            await SeedDemoBankDataAsync(context, logger);

            logger.LogInformation("🎉 Demo Data Seeding Completed!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ An error occurred while seeding demo data.");
            throw;
        }
    }

    private static async Task SeedDemoUsersAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("👥 Seeding demo users...");

        // Admin user
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@demo.local");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@demo.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = Role.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("  ✅ Created admin@demo.local");
        }
        _adminUserId = adminUser.Id;

        // Accountant user
        var accountantUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "accountant@demo.local");
        if (accountantUser == null)
        {
            accountantUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "accountant@demo.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = Role.Accountant,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(accountantUser);
            await context.SaveChangesAsync();
            logger.LogInformation("  ✅ Created accountant@demo.local");
        }
        _accountantUserId = accountantUser.Id;
    }

    private static async Task SeedDemoTenantAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("🏢 Seeding demo tenant...");

        var demoTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Demo Company BV",
            KvK = "12345678",
            VatNumber = "NL123456789B01",
            CreatedAt = DateTime.UtcNow
        };

        context.Tenants.Add(demoTenant);
        await context.SaveChangesAsync();
        _demoTenantId = demoTenant.Id;

        logger.LogInformation($"  ✅ Created tenant: {demoTenant.Name} ({demoTenant.Id})");

        // Link users to tenant
        var userTenants = new[]
        {
            new UserTenant
            {
                UserId = _adminUserId,
                TenantId = _demoTenantId,
                Role = TenantRole.Admin,
                CreatedAt = DateTime.UtcNow
            },
            new UserTenant
            {
                UserId = _accountantUserId,
                TenantId = _demoTenantId,
                Role = TenantRole.Accountant,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Set<UserTenant>().AddRange(userTenants);
        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Linked users to demo tenant");

        // Seed branding
        var branding = new TenantBranding
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            LogoUrl = "https://via.placeholder.com/200x80?text=Demo+Company",
            PrimaryColor = "#1E40AF", // Blue
            SecondaryColor = "#64748B", // Slate
            FontFamily = "Inter, sans-serif",
            FooterText = "Demo Company BV | KvK: 12345678 | BTW: NL123456789B01\nAmsterdam, Nederland | info@democompany.nl | +31 20 123 4567",
            CreatedAt = DateTime.UtcNow
        };

        context.TenantBrandings.Add(branding);
        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Created tenant branding");

        // Seed invoice template
        var template = new InvoiceTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            Name = "Professional Template",
            IsDefault = true,
            HtmlTemplate = GetDefaultHtmlTemplate(),
            CssTemplate = GetDefaultCssTemplate(),
            CreatedAt = DateTime.UtcNow
        };

        context.InvoiceTemplates.Add(template);
        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Created invoice template");
    }

    private static async Task SeedDemoAccountingAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("💰 Seeding demo accounting data...");

        // Seed default accounts (NL chart)
        var accounts = new List<Account>
        {
            // Assets (Activa)
            new Account
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                Code = "1100",
                Name = "Debiteuren",
                Type = AccountType.Asset,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Account
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
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
                TenantId = _demoTenantId,
                Code = "1600",
                Name = "Crediteuren",
                Type = AccountType.Liability,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Account
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
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
                TenantId = _demoTenantId,
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
                TenantId = _demoTenantId,
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
                TenantId = _demoTenantId,
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
                TenantId = _demoTenantId,
                Code = "VRK",
                Name = "Verkopen",
                Type = JournalType.Sales,
                CreatedAt = DateTime.UtcNow
            },
            new Journal
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                Code = "INK",
                Name = "Inkopen",
                Type = JournalType.Purchase,
                CreatedAt = DateTime.UtcNow
            },
            new Journal
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                Code = "BNK",
                Name = "Bank",
                Type = JournalType.Bank,
                CreatedAt = DateTime.UtcNow
            },
            new Journal
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                Code = "MEM",
                Name = "Memoriaal",
                Type = JournalType.General,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Journals.AddRange(journals);
        await context.SaveChangesAsync();

        logger.LogInformation($"  ✅ Created {accounts.Count} accounts and {journals.Count} journals");
    }

    private static async Task SeedDemoContactsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("👥 Seeding demo contacts...");

        var acme = new Contact
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            Type = ContactType.Customer,
            DisplayName = "Acme Corporation",
            Email = "finance@acme.example",
            Phone = "+31 20 555 0001",
            AddressLine1 = "Singel 250",
            PostalCode = "1016 AB",
            City = "Amsterdam",
            Country = "NL",
            VatNumber = "NL999888777B01",
            KvK = "99988877",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var techstart = new Contact
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            Type = ContactType.Customer,
            DisplayName = "TechStart Solutions",
            Email = "billing@techstart.example",
            Phone = "+31 30 555 0002",
            AddressLine1 = "Oudegracht 123",
            PostalCode = "3511 AE",
            City = "Utrecht",
            Country = "NL",
            VatNumber = "NL888777666B01",
            KvK = "88877766",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var supplier = new Contact
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            Type = ContactType.Supplier,
            DisplayName = "Office Supplies Nederland",
            Email = "verkoop@officesupplies.example",
            Phone = "+31 10 555 0003",
            AddressLine1 = "Coolsingel 45",
            PostalCode = "3011 AD",
            City = "Rotterdam",
            Country = "NL",
            VatNumber = "NL777666555B01",
            KvK = "77766655",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var contacts = new[] { acme, techstart, supplier };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Store for later use
        _acmeContact = acme;
        _techStartContact = techstart;

        logger.LogInformation($"  ✅ Created {contacts.Length} demo contacts");
    }

    private static async Task SeedDemoInvoicesAsync(ApplicationDbContext context, IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("📄 Seeding demo invoices...");

        // Invoice 1: Posted (paid via bank)
        var invoice1 = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            InvoiceNumber = "DEMO-2026-0001",
            Status = InvoiceStatus.Posted,
            IssueDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc),
            ContactId = _acmeContact.Id,
            Currency = "EUR",
            Subtotal = 1000.00m,
            VatTotal = 210.00m,
            Total = 1210.00m,
            CreatedAt = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc)
        };

        var invoice1Lines = new[]
        {
            new SalesInvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                InvoiceId = invoice1.Id,
                Description = "Consulting Services - January 2026",
                Quantity = 10,
                UnitPrice = 100.00m,
                VatRate = 21.00m,
                LineSubtotal = 1000.00m,
                LineVatAmount = 210.00m,
                LineTotal = 1210.00m,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Invoice 2: Sent (unpaid)
        var invoice2 = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            InvoiceNumber = "DEMO-2026-0002",
            Status = InvoiceStatus.Sent,
            IssueDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            ContactId = _techStartContact.Id,
            Currency = "EUR",
            Subtotal = 500.00m,
            VatTotal = 45.00m,
            Total = 545.00m,
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var invoice2Lines = new[]
        {
            new SalesInvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                InvoiceId = invoice2.Id,
                Description = "Software Development - 5 hours",
                Quantity = 5,
                UnitPrice = 100.00m,
                VatRate = 9.00m,
                LineSubtotal = 500.00m,
                LineVatAmount = 45.00m,
                LineTotal = 545.00m,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.SalesInvoices.AddRange(invoice1, invoice2);
        context.SalesInvoiceLines.AddRange(invoice1Lines);
        context.SalesInvoiceLines.AddRange(invoice2Lines);
        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Created 2 demo invoices");

        // Post invoice 1 to create journal entries
        await PostInvoiceToAccountingAsync(context, invoice1, logger);
    }

    private static async Task PostInvoiceToAccountingAsync(ApplicationDbContext context, SalesInvoice invoice, ILogger logger)
    {
        logger.LogInformation($"  📖 Posting invoice {invoice.InvoiceNumber} to accounting...");

        // Get journals and accounts (ignore tenant filter in seeder)
        var salesJournal = await context.Journals
            .IgnoreQueryFilters()
            .FirstAsync(j => j.TenantId == _demoTenantId && j.Type == JournalType.Sales);

        var debiteurenAccount = await context.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.TenantId == _demoTenantId && a.Code == "1100");

        var omzetAccount = await context.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.TenantId == _demoTenantId && a.Code == "8000");

        var btwAccount = await context.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.TenantId == _demoTenantId && a.Code == "1700");

        // Create journal entry
        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            JournalId = salesJournal.Id,
            EntryDate = invoice.IssueDate,
            Reference = invoice.InvoiceNumber,
            Description = $"Sales Invoice {invoice.InvoiceNumber}",
            Status = JournalEntryStatus.Posted,
            PostedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var lines = new[]
        {
            new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                EntryId = journalEntry.Id,
                AccountId = debiteurenAccount.Id,
                Description = "Debiteuren",
                Debit = invoice.Total,
                Credit = 0,
                CreatedAt = DateTime.UtcNow
            },
            new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                EntryId = journalEntry.Id,
                AccountId = omzetAccount.Id,
                Description = "Omzet",
                Debit = 0,
                Credit = invoice.Subtotal,
                CreatedAt = DateTime.UtcNow
            },
            new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                EntryId = journalEntry.Id,
                AccountId = btwAccount.Id,
                Description = "BTW 21%",
                Debit = 0,
                Credit = invoice.VatTotal,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.JournalEntries.Add(journalEntry);
        context.JournalLines.AddRange(lines);

        // Link invoice to journal entry
        invoice.JournalEntryId = journalEntry.Id;

        await context.SaveChangesAsync();

        logger.LogInformation($"    ✅ Created journal entry {journalEntry.Reference}");
    }

    private static async Task SeedDemoBankDataAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("🏦 Seeding demo bank data...");

        // Create bank connection
        var bankConnection = new BankConnection
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            Provider = "Mock",
            Status = BankConnectionStatus.Active,
            BankName = "Demo Bank NL",
            IbanMasked = "NL**DEMO****1234",
            ExternalConnectionId = "demo-conn-001",
            AccessTokenEncrypted = "mock-encrypted-token",
            ExpiresAt = DateTime.UtcNow.AddMonths(3),
            LastSyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.BankConnections.Add(bankConnection);
        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Created bank connection");

        // Get posted invoice for matching (ignore tenant filter)
        var postedInvoice = await context.SalesInvoices
            .IgnoreQueryFilters()
            .FirstAsync(i => i.TenantId == _demoTenantId && i.InvoiceNumber == "DEMO-2026-0001");

        // Bank transaction 1: Matched to invoice (paid)
        var transaction1 = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            BankConnectionId = bankConnection.Id,
            ExternalId = "demo-tx-001",
            BookingDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            ValueDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Amount = 1210.00m,
            Currency = "EUR",
            CounterpartyName = "Acme Corporation",
            CounterpartyIban = "NL91ABNA0417164300",
            Description = $"Payment invoice {postedInvoice.InvoiceNumber}",
            MatchedStatus = BankTransactionMatchStatus.MatchedToInvoice,
            MatchedInvoiceId = postedInvoice.Id,
            MatchedAt = new DateTime(2026, 1, 10, 14, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };

        // Bank transaction 2: Unmatched
        var transaction2 = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            BankConnectionId = bankConnection.Id,
            ExternalId = "demo-tx-002",
            BookingDate = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc),
            ValueDate = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc),
            Amount = -125.50m,
            Currency = "EUR",
            CounterpartyName = "Office Supplies B.V.",
            CounterpartyIban = "NL12RABO0123456789",
            Description = "Office supplies purchase",
            MatchedStatus = BankTransactionMatchStatus.Unmatched,
            CreatedAt = DateTime.UtcNow
        };

        context.BankTransactions.AddRange(transaction1, transaction2);

        // Update invoice to Paid status
        postedInvoice.Status = InvoiceStatus.Paid;
        postedInvoice.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("  ✅ Created 2 bank transactions (1 matched, 1 unmatched)");

        // Create journal entry for bank payment
        await CreateBankPaymentJournalEntryAsync(context, transaction1, postedInvoice, logger);
    }

    private static async Task CreateBankPaymentJournalEntryAsync(
        ApplicationDbContext context,
        BankTransaction transaction,
        SalesInvoice invoice,
        ILogger logger)
    {
        logger.LogInformation("  📖 Creating bank payment journal entry...");

        var bankJournal = await context.Journals
            .IgnoreQueryFilters()
            .FirstAsync(j => j.TenantId == _demoTenantId && j.Type == JournalType.Bank);

        var bankAccount = await context.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.TenantId == _demoTenantId && a.Code == "1300");

        var debiteurenAccount = await context.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.TenantId == _demoTenantId && a.Code == "1100");

        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _demoTenantId,
            JournalId = bankJournal.Id,
            EntryDate = transaction.BookingDate,
            Reference = transaction.ExternalId,
            Description = $"Payment for {invoice.InvoiceNumber}",
            Status = JournalEntryStatus.Posted,
            PostedAt = transaction.MatchedAt,
            CreatedAt = transaction.MatchedAt ?? DateTime.UtcNow
        };

        var lines = new[]
        {
            new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                EntryId = journalEntry.Id,
                AccountId = bankAccount.Id,
                Description = "Bank",
                Debit = transaction.Amount,
                Credit = 0,
                CreatedAt = DateTime.UtcNow
            },
            new JournalLine
            {
                Id = Guid.NewGuid(),
                TenantId = _demoTenantId,
                EntryId = journalEntry.Id,
                AccountId = debiteurenAccount.Id,
                Description = "Debiteuren",
                Debit = 0,
                Credit = transaction.Amount,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.JournalEntries.Add(journalEntry);
        context.JournalLines.AddRange(lines);

        transaction.JournalEntryId = journalEntry.Id;

        await context.SaveChangesAsync();

        logger.LogInformation($"    ✅ Created bank payment journal entry");
    }

    private static string GetDefaultHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Invoice {{ Invoice.InvoiceNumber }}</title>
</head>
<body>
    <div class=""header"">
        {{if Branding.LogoUrl}}
        <img src=""{{ Branding.LogoUrl }}"" alt=""Logo"" class=""logo"">
        {{end}}
        <h1>FACTUUR</h1>
    </div>

    <div class=""invoice-info"">
        <div class=""info-block"">
            <strong>Factuurnummer:</strong> {{ Invoice.InvoiceNumber }}<br>
            <strong>Factuurdatum:</strong> {{ Invoice.IssueDate | date.to_string '%d-%m-%Y' }}<br>
            <strong>Vervaldatum:</strong> {{ Invoice.DueDate | date.to_string '%d-%m-%Y' }}
        </div>

        <div class=""info-block"">
            <strong>Aan:</strong><br>
            {{ Contact.DisplayName }}<br>
            {{if Contact.AddressLine1}}{{ Contact.AddressLine1 }}<br>{{end}}
            {{if Contact.PostalCode}}{{ Contact.PostalCode }} {{ Contact.City }}<br>{{end}}
            {{if Contact.VatNumber}}<strong>BTW-nr:</strong> {{ Contact.VatNumber }}{{end}}
        </div>
    </div>

    <table class=""line-items"">
        <thead>
            <tr>
                <th>Omschrijving</th>
                <th class=""right"">Aantal</th>
                <th class=""right"">Prijs</th>
                <th class=""right"">BTW%</th>
                <th class=""right"">Totaal</th>
            </tr>
        </thead>
        <tbody>
            {{for line in Invoice.Lines}}
            <tr>
                <td>{{ line.Description }}</td>
                <td class=""right"">{{ line.Quantity }}</td>
                <td class=""right"">€ {{ line.UnitPrice | math.format '0.00' }}</td>
                <td class=""right"">{{ line.VatRate | math.format '0' }}%</td>
                <td class=""right"">€ {{ line.LineTotal | math.format '0.00' }}</td>
            </tr>
            {{end}}
        </tbody>
    </table>

    <div class=""totals"">
        <div class=""total-line"">
            <span>Subtotaal:</span>
            <span>€ {{ Invoice.Subtotal | math.format '0.00' }}</span>
        </div>
        <div class=""total-line"">
            <span>BTW:</span>
            <span>€ {{ Invoice.TotalVat | math.format '0.00' }}</span>
        </div>
        <div class=""total-line grand-total"">
            <span>Totaal:</span>
            <span>€ {{ Invoice.Total | math.format '0.00' }}</span>
        </div>
    </div>

    <div class=""footer"">
        {{ Branding.FooterText }}
    </div>
</body>
</html>";
    }

    private static string GetDefaultCssTemplate()
    {
        return @"
body {
    font-family: {{ Branding.FontFamily }};
    font-size: 12px;
    line-height: 1.6;
    color: #333;
    max-width: 800px;
    margin: 0 auto;
    padding: 40px;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 40px;
    padding-bottom: 20px;
    border-bottom: 3px solid {{ Branding.PrimaryColor }};
}

.logo {
    max-height: 80px;
}

h1 {
    color: {{ Branding.PrimaryColor }};
    font-size: 32px;
    margin: 0;
}

.invoice-info {
    display: flex;
    justify-content: space-between;
    margin-bottom: 40px;
}

.info-block {
    flex: 1;
}

table.line-items {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 30px;
}

table.line-items thead {
    background-color: {{ Branding.PrimaryColor }};
    color: white;
}

table.line-items th,
table.line-items td {
    padding: 12px;
    text-align: left;
    border-bottom: 1px solid #ddd;
}

table.line-items th.right,
table.line-items td.right {
    text-align: right;
}

.totals {
    margin-left: auto;
    width: 300px;
    margin-bottom: 40px;
}

.total-line {
    display: flex;
    justify-content: space-between;
    padding: 8px 0;
    border-bottom: 1px solid #eee;
}

.total-line.grand-total {
    font-weight: bold;
    font-size: 16px;
    border-top: 2px solid {{ Branding.PrimaryColor }};
    border-bottom: 2px solid {{ Branding.PrimaryColor }};
    margin-top: 10px;
    padding-top: 12px;
    color: {{ Branding.PrimaryColor }};
}

.footer {
    margin-top: 60px;
    padding-top: 20px;
    border-top: 1px solid #ddd;
    font-size: 10px;
    color: {{ Branding.SecondaryColor }};
    text-align: center;
    white-space: pre-line;
}";
    }
}

using Microsoft.EntityFrameworkCore;
using Boekhouding.Infrastructure.Data;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=boekhouding;Username=postgres;Password=postgres");

using var context = new ApplicationDbContext(optionsBuilder.Options, null!);

Console.WriteLine("Cleaning database...");

// Delete all sales invoice related data
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"SalesInvoiceLines\"");
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"SalesInvoices\"");
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"StoredFiles\"");

// Delete all template and branding data  
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"InvoiceTemplates\"");
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"TenantBrandings\"");

Console.WriteLine("✓ Database cleaned successfully!");
Console.WriteLine("You can now run the test script again with a clean slate.");

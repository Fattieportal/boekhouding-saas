using Boekhouding.Application.DTOs.Banking;
using Boekhouding.Application.DTOs.SalesInvoices;
using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Domain.Enums;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly IFileStorage _fileStorage;

    public SalesInvoiceService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ITemplateRenderer templateRenderer,
        IPdfRenderer pdfRenderer,
        IFileStorage fileStorage)
    {
        _context = context;
        _tenantContext = tenantContext;
        _templateRenderer = templateRenderer;
        _pdfRenderer = pdfRenderer;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<SalesInvoiceDto>> GetAllInvoicesAsync(
        InvoiceStatus? status = null,
        bool? overdue = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Contact)
            .AsQueryable();

        // Filter by status
        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        // Filter by date range
        if (from.HasValue)
        {
            query = query.Where(i => i.IssueDate >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(i => i.IssueDate <= to.Value);
        }

        var invoices = await query.ToListAsync();

        // Apply overdue filter (in-memory because it uses computed property)
        if (overdue.HasValue && overdue.Value)
        {
            invoices = invoices.Where(i => i.IsOverdue).ToList();
        }

        return invoices.Select(MapToDto);
    }

    public async Task<SalesInvoiceDto?> GetInvoiceByIdAsync(Guid id)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return null;
        }

        // Load matched payments (bank transactions)
        var payments = await _context.Set<BankTransaction>()
            .Where(t => t.MatchedInvoiceId == id && t.MatchedStatus == BankTransactionMatchStatus.MatchedToInvoice)
            .OrderBy(t => t.BookingDate)
            .ToListAsync();

        var dto = MapToDto(invoice);
        dto.Payments = payments.Select(p => new PaymentTransactionDto
        {
            Id = p.Id,
            BookingDate = p.BookingDate,
            Amount = p.Amount,
            Currency = p.Currency,
            CounterpartyName = p.CounterpartyName,
            Description = p.Description,
            JournalEntryId = p.JournalEntryId,
            MatchedAt = p.MatchedAt ?? DateTime.UtcNow // Fallback, should always have value
        }).ToList();

        return dto;
    }

    public async Task<SalesInvoiceDto> CreateInvoiceAsync(CreateSalesInvoiceDto dto)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");
        
        var invoice = new SalesInvoice
        {
            TenantId = tenantId,
            InvoiceNumber = dto.InvoiceNumber,
            IssueDate = DateTime.SpecifyKind(dto.IssueDate, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            ContactId = dto.ContactId,
            Currency = dto.Currency,
            TemplateId = dto.TemplateId,
            Notes = dto.Notes,
            Status = InvoiceStatus.Draft
        };

        // Add lines
        var lineNumber = 1;
        foreach (var lineDto in dto.Lines)
        {
            var line = new SalesInvoiceLine
            {
                TenantId = tenantId,
                LineNumber = lineNumber++,
                Description = lineDto.Description,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                VatRate = lineDto.VatRate,
                AccountId = lineDto.AccountId
            };

            // Calculate totals
            line.LineSubtotal = line.Quantity * line.UnitPrice;
            line.LineVatAmount = line.LineSubtotal * (line.VatRate / 100);
            line.LineTotal = line.LineSubtotal + line.LineVatAmount;

            invoice.Lines.Add(line);
        }

        // Calculate invoice totals
        invoice.Subtotal = invoice.Lines.Sum(l => l.LineSubtotal);
        invoice.VatTotal = invoice.Lines.Sum(l => l.LineVatAmount);
        invoice.Total = invoice.Lines.Sum(l => l.LineTotal);
        invoice.OpenAmount = invoice.Total; // Initialize as fully unpaid

        _context.Set<SalesInvoice>().Add(invoice);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(invoice).Reference(i => i.Contact).LoadAsync();

        return MapToDto(invoice);
    }

    public async Task<SalesInvoiceDto?> UpdateInvoiceAsync(Guid id, UpdateSalesInvoiceDto dto)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        // Only allow updates if invoice is still in Draft status
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Can only update invoices in Draft status");
        }

        if (dto.InvoiceNumber != null) invoice.InvoiceNumber = dto.InvoiceNumber;
        if (dto.IssueDate.HasValue) invoice.IssueDate = DateTime.SpecifyKind(dto.IssueDate.Value, DateTimeKind.Utc);
        if (dto.DueDate.HasValue) invoice.DueDate = DateTime.SpecifyKind(dto.DueDate.Value, DateTimeKind.Utc);
        if (dto.ContactId.HasValue) invoice.ContactId = dto.ContactId.Value;
        if (dto.Currency != null) invoice.Currency = dto.Currency;
        if (dto.TemplateId.HasValue) invoice.TemplateId = dto.TemplateId;
        if (dto.Notes != null) invoice.Notes = dto.Notes;

        // Update lines if provided
        if (dto.Lines != null)
        {
            // Remove existing lines
            _context.Set<SalesInvoiceLine>().RemoveRange(invoice.Lines);
            invoice.Lines.Clear();

            // Add new lines
            var lineNumber = 1;
            foreach (var lineDto in dto.Lines)
            {
                var line = new SalesInvoiceLine
                {
                    TenantId = invoice.TenantId,
                    InvoiceId = invoice.Id,
                    LineNumber = lineNumber++,
                    Description = lineDto.Description,
                    Quantity = lineDto.Quantity,
                    UnitPrice = lineDto.UnitPrice,
                    VatRate = lineDto.VatRate,
                    AccountId = lineDto.AccountId
                };

                line.LineSubtotal = line.Quantity * line.UnitPrice;
                line.LineVatAmount = line.LineSubtotal * (line.VatRate / 100);
                line.LineTotal = line.LineSubtotal + line.LineVatAmount;

                invoice.Lines.Add(line);
            }

            // Recalculate totals
            invoice.Subtotal = invoice.Lines.Sum(l => l.LineSubtotal);
            invoice.VatTotal = invoice.Lines.Sum(l => l.LineVatAmount);
            invoice.Total = invoice.Lines.Sum(l => l.LineTotal);
            invoice.OpenAmount = invoice.Total; // Reset to fully unpaid on update
        }

        await _context.SaveChangesAsync();

        return MapToDto(invoice);
    }

    public async Task<bool> DeleteInvoiceAsync(Guid id)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return false;

        // Only allow deletion of draft invoices
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Can only delete invoices in Draft status");
        }

        // Delete associated PDF if exists
        if (invoice.PdfFileId.HasValue)
        {
            await _fileStorage.DeleteFileAsync(invoice.PdfFileId.Value);
        }

        _context.Set<SalesInvoice>().Remove(invoice);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<byte[]?> RenderInvoicePdfAsync(Guid id)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Contact)
            .Include(i => i.Template)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        // Get branding for current tenant (or use defaults if none exists)
        var branding = await _context.Set<TenantBranding>()
            .IgnoreQueryFilters()
            .Where(b => b.TenantId == invoice.TenantId)
            .FirstOrDefaultAsync();
        
        // Create default branding if none exists
        var brandingData = branding != null ? new
        {
            LogoUrl = branding.LogoUrl,
            PrimaryColor = (string?)branding.PrimaryColor,
            SecondaryColor = (string?)branding.SecondaryColor,
            FontFamily = (string?)branding.FontFamily,
            FooterText = branding.FooterText
        } : new
        {
            LogoUrl = (string?)null,
            PrimaryColor = (string?)"#0066cc",
            SecondaryColor = (string?)"#333333",
            FontFamily = (string?)"Arial, sans-serif",
            FooterText = (string?)null
        };

        // Get template (use specified template or default)
        InvoiceTemplate? template = invoice.Template;
        if (template == null)
        {
            template = await _context.Set<InvoiceTemplate>()
                .FirstOrDefaultAsync(t => t.IsDefault);
        }

        // If still no template, use a built-in default
        if (template == null)
        {
            template = GetBuiltInDefaultTemplate();
        }

        // Prepare data model for template
        var templateData = new InvoiceTemplateDataDto
        {
            Invoice = new InvoiceDataDto
            {
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate.ToString("dd-MM-yyyy"),
                DueDate = invoice.DueDate.ToString("dd-MM-yyyy"),
                Currency = invoice.Currency,
                Subtotal = invoice.Subtotal.ToString("F2"),
                VatTotal = invoice.VatTotal.ToString("F2"),
                Total = invoice.Total.ToString("F2"),
                Notes = invoice.Notes
            },
            Contact = new ContactDataDto
            {
                DisplayName = invoice.Contact?.DisplayName,
                Email = invoice.Contact?.Email,
                Phone = invoice.Contact?.Phone,
                AddressLine1 = invoice.Contact?.AddressLine1,
                AddressLine2 = invoice.Contact?.AddressLine2,
                PostalCode = invoice.Contact?.PostalCode,
                City = invoice.Contact?.City,
                Country = invoice.Contact?.Country
            },
            Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(l => new LineDataDto
            {
                LineNumber = l.LineNumber,
                Description = l.Description,
                Quantity = l.Quantity.ToString("F2"),
                UnitPrice = l.UnitPrice.ToString("F2"),
                VatRate = l.VatRate.ToString("F0"),
                LineSubtotal = l.LineSubtotal.ToString("F2"),
                LineVatAmount = l.LineVatAmount.ToString("F2"),
                LineTotal = l.LineTotal.ToString("F2")
            }).ToList(),
            Branding = new BrandingDataDto
            {
                LogoUrl = brandingData.LogoUrl,
                PrimaryColor = brandingData.PrimaryColor ?? "#0066cc",
                SecondaryColor = brandingData.SecondaryColor ?? "#333333",
                FontFamily = brandingData.FontFamily ?? "Arial, sans-serif",
                FooterText = brandingData.FooterText
            }
        };

        // Render HTML
        var html = await _templateRenderer.RenderAsync(template.HtmlTemplate, templateData);
        
        // DEBUG: Log the rendered HTML
        Console.WriteLine("=== RENDERED HTML ===");
        Console.WriteLine(html);
        Console.WriteLine("=== END HTML ===");

        // Generate PDF
        var pdfBytes = await _pdfRenderer.GeneratePdfAsync(html, template.CssTemplate);

        // Store PDF
        var fileName = $"Invoice_{invoice.InvoiceNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        var storedFile = await _fileStorage.StoreFileAsync(
            invoice.TenantId,
            fileName,
            "application/pdf",
            pdfBytes,
            "Invoices");

        // Update invoice with PDF reference
        invoice.PdfFileId = storedFile.Id;
        await _context.SaveChangesAsync();

        return pdfBytes;
    }

    public async Task<SalesInvoiceDto?> PostInvoiceAsync(Guid id)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        // Idempotency: if already posted, return existing invoice
        if (invoice.Status == InvoiceStatus.Posted)
        {
            return MapToDto(invoice);
        }

        if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Sent)
        {
            throw new InvalidOperationException("Can only post invoices in Draft or Sent status");
        }

        // Validate invoice has lines and positive total
        if (invoice.Lines == null || invoice.Lines.Count == 0)
        {
            throw new InvalidOperationException("Cannot post invoice without any lines");
        }

        if (invoice.Total <= 0)
        {
            throw new InvalidOperationException("Cannot post invoice with total amount <= 0");
        }

        // Validate invoice totals
        var calculatedSubtotal = invoice.Lines.Sum(l => l.LineSubtotal);
        var calculatedVatTotal = invoice.Lines.Sum(l => l.LineVatAmount);
        var calculatedTotal = invoice.Lines.Sum(l => l.LineTotal);

        if (Math.Abs(invoice.Subtotal - calculatedSubtotal) > 0.01m ||
            Math.Abs(invoice.VatTotal - calculatedVatTotal) > 0.01m ||
            Math.Abs(invoice.Total - calculatedTotal) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Invoice totals validation failed. " +
                $"Expected: Subtotal={calculatedSubtotal:F2}, VAT={calculatedVatTotal:F2}, Total={calculatedTotal:F2}. " +
                $"Actual: Subtotal={invoice.Subtotal:F2}, VAT={invoice.VatTotal:F2}, Total={invoice.Total:F2}");
        }

        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is not set");

        // Generate invoice number if not already set
        if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(tenantId);
        }

        // Get Sales journal
        var salesJournal = await _context.Set<Journal>()
            .FirstOrDefaultAsync(j => j.Type == JournalType.Sales);

        if (salesJournal == null)
        {
            throw new InvalidOperationException("Sales journal not found. Please create a Sales journal first.");
        }

        // Get required accounts
        var accountsReceivableAccount = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == "1100" && a.Type == AccountType.Asset);

        var vatPayableAccount = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == "1700" && a.Type == AccountType.Liability);

        if (accountsReceivableAccount == null)
        {
            throw new InvalidOperationException(
                "Accounts Receivable account (1100) not found. Please create this account first.");
        }

        if (vatPayableAccount == null && invoice.VatTotal > 0)
        {
            throw new InvalidOperationException(
                "VAT Payable account (1700) not found. Please create this account first.");
        }

        // Create journal entry for the invoice
        var journalEntry = new JournalEntry
        {
            TenantId = tenantId,
            JournalId = salesJournal.Id,
            EntryDate = invoice.IssueDate,
            Reference = invoice.InvoiceNumber,
            Description = $"Sales Invoice {invoice.InvoiceNumber} - {invoice.Contact?.DisplayName}",
            Status = JournalEntryStatus.Posted,
            PostedAt = DateTime.UtcNow
        };

        // Add debtor line (debit accounts receivable)
        var debtorLine = new JournalLine
        {
            TenantId = tenantId,
            AccountId = accountsReceivableAccount.Id,
            Description = $"Sales Invoice {invoice.InvoiceNumber}",
            Debit = invoice.Total,
            Credit = 0
        };
        journalEntry.Lines.Add(debtorLine);

        // Group revenue lines by account and VAT rate to consolidate entries
        var revenueGroups = invoice.Lines
            .Where(l => l.AccountId.HasValue)
            .GroupBy(l => new { l.AccountId, l.VatRate })
            .ToList();

        foreach (var group in revenueGroups)
        {
            var accountId = group.Key.AccountId!.Value;
            var subtotal = group.Sum(l => l.LineSubtotal);
            var vatAmount = group.Sum(l => l.LineVatAmount);
            var description = group.Count() == 1 
                ? group.First().Description 
                : $"Sales - {group.Count()} items @ {group.Key.VatRate}% VAT";

            // Add revenue line (credit revenue)
            var revenueLine = new JournalLine
            {
                TenantId = tenantId,
                AccountId = accountId,
                Description = description,
                Debit = 0,
                Credit = subtotal
            };
            journalEntry.Lines.Add(revenueLine);

            // Add VAT line if applicable
            if (vatAmount > 0 && vatPayableAccount != null)
            {
                var vatLine = new JournalLine
                {
                    TenantId = tenantId,
                    AccountId = vatPayableAccount.Id,
                    Description = $"VAT {group.Key.VatRate}% - {description}",
                    Debit = 0,
                    Credit = vatAmount
                };
                journalEntry.Lines.Add(vatLine);
            }
        }

        // Handle lines without account assignment
        var linesWithoutAccount = invoice.Lines.Where(l => !l.AccountId.HasValue).ToList();
        if (linesWithoutAccount.Any())
        {
            throw new InvalidOperationException(
                $"Cannot post invoice: {linesWithoutAccount.Count} line(s) do not have an account assigned. " +
                $"Please assign revenue accounts to all invoice lines.");
        }

        // Final validation: ensure entry is balanced
        var totalDebit = journalEntry.Lines.Sum(l => l.Debit);
        var totalCredit = journalEntry.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Journal entry is not balanced. Debit: {totalDebit:F2}, Credit: {totalCredit:F2}");
        }

        _context.Set<JournalEntry>().Add(journalEntry);
        
        invoice.JournalEntryId = journalEntry.Id;
        invoice.Status = InvoiceStatus.Posted;

        await _context.SaveChangesAsync();

        return MapToDto(invoice);
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice invoice)
    {
        return new SalesInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            ContactId = invoice.ContactId,
            ContactName = invoice.Contact?.DisplayName ?? "",
            Currency = invoice.Currency,
            Subtotal = invoice.Subtotal,
            VatTotal = invoice.VatTotal,
            Total = invoice.Total,
            OpenAmount = invoice.OpenAmount,
            IsUnpaid = invoice.IsUnpaid,
            IsOverdue = invoice.IsOverdue,
            PdfFileId = invoice.PdfFileId,
            TemplateId = invoice.TemplateId,
            Notes = invoice.Notes,
            JournalEntryId = invoice.JournalEntryId,
            Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(l => new SalesInvoiceLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
                LineSubtotal = l.LineSubtotal,
                LineVatAmount = l.LineVatAmount,
                LineTotal = l.LineTotal,
                AccountId = l.AccountId
            }).ToList(),
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }

    private InvoiceTemplate GetBuiltInDefaultTemplate()
    {
        return new InvoiceTemplate
        {
            Name = "Default Template",
            IsDefault = true,
            HtmlTemplate = DefaultTemplates.DefaultHtmlTemplate,
            CssTemplate = DefaultTemplates.DefaultCssTemplate
        };
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid tenantId)
    {
        // Get current year
        var year = DateTime.UtcNow.Year;
        
        // Get the count of invoices for this tenant in this year
        var count = await _context.Set<SalesInvoice>()
            .Where(i => i.TenantId == tenantId && i.CreatedAt.Year == year)
            .CountAsync();
        
        // Generate sequential number: YYYY-NNNN
        var sequentialNumber = count + 1;
        return $"INV-{year}-{sequentialNumber:D4}";
    }
}

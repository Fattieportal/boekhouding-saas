# Quick Implementation Guide - Audit Logging

## How to Add Audit Logging to Your Services

### 1. Inject IAuditLogService

In your service constructor:

```csharp
public class YourService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    
    public YourService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext)
    {
        _context = context;
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
    }
}
```

### 2. Log Actions

#### Create Operation

```csharp
public async Task<MyEntity> CreateAsync(CreateDto dto, Guid userId)
{
    var entity = new MyEntity
    {
        // ... map properties
        TenantId = _tenantContext.TenantId
    };
    
    _context.Add(entity);
    await _context.SaveChangesAsync();
    
    // Log creation
    await _auditLogService.LogAsync(
        tenantId: entity.TenantId,
        actorUserId: userId,
        action: "Create",
        entityType: nameof(MyEntity),
        entityId: entity.Id,
        diff: new { Created = entity } // or relevant subset of properties
    );
    
    return entity;
}
```

#### Update Operation

```csharp
public async Task<MyEntity> UpdateAsync(Guid id, UpdateDto dto, Guid userId)
{
    var entity = await _context.Set<MyEntity>().FindAsync(id);
    
    // Capture old state
    var oldState = new { entity.Name, entity.Status };
    
    // Update
    entity.Name = dto.Name;
    entity.Status = dto.Status;
    entity.UpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    // Log update with diff
    await _auditLogService.LogAsync(
        tenantId: entity.TenantId,
        actorUserId: userId,
        action: "Update",
        entityType: nameof(MyEntity),
        entityId: entity.Id,
        diff: new 
        { 
            Before = oldState,
            After = new { entity.Name, entity.Status }
        }
    );
    
    return entity;
}
```

#### State Change Operation (Post, Reverse, etc.)

```csharp
public async Task PostAsync(Guid id, Guid userId)
{
    var entity = await _context.Set<MyEntity>().FindAsync(id);
    
    var oldStatus = entity.Status;
    entity.Status = EntityStatus.Posted;
    entity.PostedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    // Log state change
    await _auditLogService.LogAsync(
        tenantId: entity.TenantId,
        actorUserId: userId,
        action: "Post",
        entityType: nameof(MyEntity),
        entityId: entity.Id,
        diff: new
        {
            Before = new { Status = oldStatus },
            After = new { Status = entity.Status, PostedAt = entity.PostedAt }
        }
    );
}
```

### 3. In Controllers (with HTTP context)

```csharp
using Boekhouding.Api.Extensions;

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateDto dto)
{
    var userId = GetUserId(); // from claims
    var entity = await _service.CreateAsync(dto, userId);
    
    // Alternative: log in controller with HTTP context info
    await _auditLogService.LogAuditAsync(
        httpContext: HttpContext,
        tenantId: _tenantContext.TenantId,
        actorUserId: userId,
        action: "Create",
        entityType: "MyEntity",
        entityId: entity.Id,
        diff: new { Created = entity }
    );
    
    return Ok(entity);
}
```

### 4. Recommended Actions to Log

#### Always Log:
- **Create** - Entity creation
- **Update** - Entity updates (with before/after)
- **Delete** - Entity deletion (with final state)
- **Post** - Publishing/posting entities
- **Reverse** - Reversing transactions
- **Pay** - Payment operations
- **Sync** - Data synchronization
- **Match** - Data matching operations

#### Entity Types to Log:
- `Tenant`
- `User`
- `InvoiceTemplate`
- `TenantBranding`
- `SalesInvoice`
- `JournalEntry`
- `BankConnection`
- `BankTransaction`
- `Contact` (for customers/suppliers)
- `Account` (chart of accounts changes)

### 5. What to Include in DiffJson

#### For Creates:
```csharp
diff: new { Created = relevantFields }
```

#### For Updates:
```csharp
diff: new 
{ 
    Before = { field1 = oldValue1, field2 = oldValue2 },
    After = { field1 = newValue1, field2 = newValue2 }
}
```

#### For Deletes:
```csharp
diff: new { Deleted = relevantFields }
```

#### For State Changes:
```csharp
diff: new
{
    Before = { Status = oldStatus },
    After = { Status = newStatus, RelatedField = value }
}
```

### 6. Best Practices

1. **Don't log sensitive data** (passwords, tokens, etc.)
2. **Keep DiffJson concise** - only relevant changed fields
3. **Log after successful save** - within same transaction if possible
4. **Use meaningful action names** - "Post", "Reverse", not "Update"
5. **Always include TenantId** - for multi-tenant isolation
6. **Log at service layer** - not in repositories
7. **Handle audit failures gracefully** - don't fail business operation if audit fails

### 7. Error Handling

```csharp
try 
{
    // Business operation
    await _context.SaveChangesAsync();
    
    // Log audit
    await _auditLogService.LogAsync(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Audit logging failed for {Action} on {EntityType}", 
        action, entityType);
    // Don't throw - audit is supplementary
}
```

### 8. Performance Considerations

For high-volume operations:

```csharp
// Option 1: Async fire-and-forget (careful with transaction scope)
_ = Task.Run(async () => 
{
    try 
    {
        await _auditLogService.LogAsync(...);
    }
    catch { /* log error */ }
});

// Option 2: Batch logging for bulk operations
var auditEntries = entities.Select(e => new AuditLogEntry { ... });
await _auditLogService.LogBatchAsync(auditEntries);
```

## Example: Complete Service Implementation

```csharp
public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SalesInvoiceService> _logger;
    
    public async Task<SalesInvoice> PostInvoiceAsync(Guid invoiceId, Guid userId)
    {
        var invoice = await _context.SalesInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
            
        if (invoice == null)
            throw new NotFoundException("Invoice not found");
            
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be posted");
        
        // Capture old state
        var oldState = new 
        { 
            invoice.Status, 
            invoice.PostedAt,
            invoice.InvoiceNumber 
        };
        
        // Perform state change
        invoice.Status = InvoiceStatus.Posted;
        invoice.PostedAt = DateTime.UtcNow;
        invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
        
        // Create journal entries (accounting)
        await CreateJournalEntriesAsync(invoice);
        
        await _context.SaveChangesAsync();
        
        // Audit log
        try 
        {
            await _auditLogService.LogAsync(
                tenantId: invoice.TenantId,
                actorUserId: userId,
                action: "Post",
                entityType: nameof(SalesInvoice),
                entityId: invoice.Id,
                diff: new
                {
                    Before = oldState,
                    After = new 
                    { 
                        invoice.Status, 
                        invoice.PostedAt,
                        invoice.InvoiceNumber 
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit for invoice post: {InvoiceId}", invoiceId);
        }
        
        return invoice;
    }
}
```

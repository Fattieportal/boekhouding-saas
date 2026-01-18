using Boekhouding.Domain.Common;

namespace Boekhouding.Domain.Entities;

/// <summary>
/// Metadata voor opgeslagen bestanden (PDFs, uploads, etc.)
/// </summary>
public class StoredFile : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Originele bestandsnaam
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// MIME type van het bestand
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Bestandsgrootte in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// Relatief pad waar het bestand is opgeslagen
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Type bestand (Invoice, Logo, Document, etc.)
    /// </summary>
    public string FileCategory { get; set; } = string.Empty;
    
    // Navigation
    public Tenant? Tenant { get; set; }
}

using Boekhouding.Domain.Entities;

namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Interface voor bestandsopslag
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Slaat een bestand op
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="fileName">Bestandsnaam</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="content">Bestandsinhoud</param>
    /// <param name="category">Categorie (Invoice, Logo, etc.)</param>
    /// <returns>StoredFile metadata</returns>
    Task<StoredFile> StoreFileAsync(
        Guid tenantId, 
        string fileName, 
        string contentType, 
        byte[] content, 
        string category);
    
    /// <summary>
    /// Haalt een bestand op
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <returns>Bestandsinhoud</returns>
    Task<byte[]> GetFileAsync(Guid fileId);
    
    /// <summary>
    /// Verwijdert een bestand
    /// </summary>
    /// <param name="fileId">File ID</param>
    Task DeleteFileAsync(Guid fileId);
    
    /// <summary>
    /// Controleert of een bestand bestaat
    /// </summary>
    /// <param name="fileId">File ID</param>
    Task<bool> FileExistsAsync(Guid fileId);
}

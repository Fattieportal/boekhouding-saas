using Boekhouding.Application.Interfaces;
using Boekhouding.Domain.Entities;
using Boekhouding.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Lokale file storage implementatie voor development
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly ApplicationDbContext _context;
    private readonly string _storagePath;

    public LocalFileStorage(ApplicationDbContext context)
    {
        _context = context;
        
        // Maak storage directory aan in de backend folder
        var baseDir = Directory.GetCurrentDirectory();
        _storagePath = Path.Combine(baseDir, "storage");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<StoredFile> StoreFileAsync(
        Guid tenantId, 
        string fileName, 
        string contentType, 
        byte[] content, 
        string category)
    {
        // Maak tenant-specifieke directory
        var tenantDir = Path.Combine(_storagePath, tenantId.ToString());
        if (!Directory.Exists(tenantDir))
        {
            Directory.CreateDirectory(tenantDir);
        }

        // Maak categorie directory
        var categoryDir = Path.Combine(tenantDir, category);
        if (!Directory.Exists(categoryDir))
        {
            Directory.CreateDirectory(categoryDir);
        }

        // Genereer unieke bestandsnaam
        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{fileId}{extension}";
        var fullPath = Path.Combine(categoryDir, storedFileName);
        var relativePath = Path.Combine(tenantId.ToString(), category, storedFileName);

        // Schrijf bestand
        await File.WriteAllBytesAsync(fullPath, content);

        // Maak metadata
        var storedFile = new StoredFile
        {
            Id = fileId,
            TenantId = tenantId,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = content.Length,
            StoragePath = relativePath,
            FileCategory = category
        };

        _context.Set<StoredFile>().Add(storedFile);
        await _context.SaveChangesAsync();

        return storedFile;
    }

    public async Task<byte[]> GetFileAsync(Guid fileId)
    {
        var fileMetadata = await _context.Set<StoredFile>()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (fileMetadata == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found in database.");
        }

        var fullPath = Path.Combine(_storagePath, fileMetadata.StoragePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Physical file not found: {fullPath}");
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task DeleteFileAsync(Guid fileId)
    {
        var fileMetadata = await _context.Set<StoredFile>()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (fileMetadata != null)
        {
            var fullPath = Path.Combine(_storagePath, fileMetadata.StoragePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            _context.Set<StoredFile>().Remove(fileMetadata);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> FileExistsAsync(Guid fileId)
    {
        var fileMetadata = await _context.Set<StoredFile>()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (fileMetadata == null) return false;

        var fullPath = Path.Combine(_storagePath, fileMetadata.StoragePath);
        return File.Exists(fullPath);
    }
}

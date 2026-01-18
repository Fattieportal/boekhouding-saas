namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Interface voor het genereren van PDFs
/// </summary>
public interface IPdfRenderer
{
    /// <summary>
    /// Genereert een PDF van HTML content
    /// </summary>
    /// <param name="htmlContent">HTML content</param>
    /// <param name="cssContent">Optionele CSS styling</param>
    /// <returns>PDF als byte array</returns>
    Task<byte[]> GeneratePdfAsync(string htmlContent, string? cssContent = null);
}

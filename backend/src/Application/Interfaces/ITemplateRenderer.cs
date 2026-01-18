namespace Boekhouding.Application.Interfaces;

/// <summary>
/// Interface voor het renderen van templates met data
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Rendert een template met de gegeven data
    /// </summary>
    /// <param name="template">HTML template met placeholders</param>
    /// <param name="data">Data object voor template rendering</param>
    /// <returns>Gerenderde HTML</returns>
    Task<string> RenderAsync(string template, object data);
}

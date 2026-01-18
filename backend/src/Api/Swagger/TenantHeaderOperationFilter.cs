using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Boekhouding.Api.Swagger;

/// <summary>
/// Voegt X-Tenant-Id header toe aan Swagger UI voor endpoints die deze vereisen
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Skip voor auth endpoints, health check en tenant management endpoints
        var path = context.ApiDescription.RelativePath?.ToLower() ?? string.Empty;
        if (path.StartsWith("api/auth") || 
            path.StartsWith("health") || 
            path.StartsWith("api/tenants/my") ||
            path == "api/tenants")
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant ID (GUID) voor multi-tenant isolatie",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
    }
}

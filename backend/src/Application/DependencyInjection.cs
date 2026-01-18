using Microsoft.Extensions.DependencyInjection;

namespace Boekhouding.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registreer Application services hier
        // Bijvoorbeeld: MediatR, AutoMapper, FluentValidation, etc.
        
        return services;
    }
}

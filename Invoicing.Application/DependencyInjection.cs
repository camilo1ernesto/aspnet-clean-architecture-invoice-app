using Invoicing.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(c =>
        {
            c.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            c.AddOpenBehavior(typeof(DomainExceptionTranslationBehavior<,>));
        });
        return services;
    }
}
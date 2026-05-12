using System.Reflection;
using FluentValidation;
using Maemo.Application.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Maemo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR with handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register Engine facades
        services.AddScoped<IDocumentsEngine, DocumentsEngine>();
        services.AddScoped<INcrEngine, NcrEngine>();
        services.AddScoped<IRiskEngine, RiskEngine>();
        services.AddScoped<IAuditEngine, AuditEngine>();
        services.AddScoped<IConsultantEngine, ConsultantEngine>();
        services.AddScoped<ITenantEngine, TenantEngine>();

        return services;
    }
}


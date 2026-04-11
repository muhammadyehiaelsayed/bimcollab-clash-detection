using System.Reflection;
using BimCollab.ClashDetection.Application.Common.Behaviors;
using BimCollab.ClashDetection.Domain.Rules;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BimCollab.ClashDetection.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddClashDetectionRules();

        return services;
    }

    private static IServiceCollection AddClashDetectionRules(this IServiceCollection services)
    {
        var ruleTypes = typeof(IClashDetectionRule).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IClashDetectionRule)));

        foreach (var ruleType in ruleTypes)
        {
            services.AddSingleton(typeof(IClashDetectionRule), ruleType);
        }

        return services;
    }
}

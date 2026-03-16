using CarInsuranceBot.Application.Handlers;
using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Application.Services;
using CarInsuranceBot.Application.StateMachine;
using Microsoft.Extensions.DependencyInjection;


namespace CarInsuranceBot.Application;

public static class ConfigureServices
{
    //Extension method for registering all Application services in the DI container.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<ISessionService, SessionService>();

        services.AddScoped<IPolicyService, PolicyService>();

        services.AddScoped<StartHandler>();
        services.AddScoped<DocumentHandler>();
        services.AddScoped<ConfirmHandler>();
        services.AddScoped<PaymentHandler>();
        services.AddScoped<PolicyHandler>();

        services.AddScoped<BotDispatcher>();

        return services;
    }
}
using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Telegram.Bot;

namespace CarInsuranceBot.Infrastructure;

public static class ConfigureServices
{
    //Extension method for registering all Infrastructure services in the DI container.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotClient>(_ =>
        {
            var token = Environment.GetEnvironmentVariable("BotSettings__Token");

            Console.WriteLine($"Telegram token: '{token}'");

            return new TelegramBotClient(token!);
        });

        services.AddScoped<ChatClient>(_ =>
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(Environment.GetEnvironmentVariable("GroqAiSettings__URL")!)
            };

            var credential = new ApiKeyCredential(
                Environment.GetEnvironmentVariable("GroqAiSettings__ApiKey")!);

            var model = Environment.GetEnvironmentVariable("GroqAiSettings__Model");

            return new ChatClient(model, credential, options);
        });

        services.AddScoped<IAiService, GroqService>();

        services.AddScoped<IMindeeService, MockMindeeService>();

        return services;
    }
}
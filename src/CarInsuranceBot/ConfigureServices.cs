using System.Text.Json;

namespace CarInsuranceBot;

public static class ConfigureServices
{
    //Extension method for registering all Api services in the DI container.
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {

                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.SnakeCaseLower;

                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        return services;
    }
}
using NLog.Extensions.Logging;
using Synapse.Http;
using Synapse.Interfaces;
using Synapse.Services;

/// <summary>
/// The main program class.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        IServiceProvider services = ConfigureServices(args);
        IOrderProcessor? orderProcessor = services.GetService<IOrderProcessor>();
        await orderProcessor!.ProcessOrders();
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured service provider.</returns>
    private static IServiceProvider ConfigureServices(string[] args)
    {
        string? environmentName = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
#if DEBUG
        environmentName = "Local";
#endif
        Console.WriteLine($"Runtime environment: {environmentName}");

        // Build configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) // Add settings from local.settings.json if there are any.
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Get API settings for the environment and bind
        ApiSettings apiSettings = new();
        configuration.GetSection("ApiSettings").Bind(apiSettings);

        // Configure services
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();

        // Configure Logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            logging.AddNLog(configuration);
        });

        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

        services.AddScoped<IOrderRepository>(sp => new OrderRepository(
             sp.GetRequiredService<IHttpClientWrapper>(),
             apiSettings.OrdersApiUrl,
             apiSettings.UpdateApiUrl,
             sp.GetRequiredService<ILogger<OrderRepository>>()
         ));

        services.AddScoped<IAlertService>(sp => new AlertService(
            sp.GetRequiredService<IHttpClientWrapper>(),
            apiSettings.AlertApiUrl,
            sp.GetRequiredService<ILogger<AlertService>>()
        ));
        services.AddScoped<IOrderProcessor, OrderProcessor>();

        return services.BuildServiceProvider();
    }
}

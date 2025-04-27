using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using WebApp;
using WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true);
var storageConnString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

// Configure authentication
builder.Services.AddMsalAuthentication(options =>
{
    var authentication = options.ProviderOptions.Authentication;
    
    builder.Configuration.Bind("AzureAd", authentication);
    
    // Add scopes for API access if needed
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add auth-enabled HttpClient factory for authorized API requests
builder.Services.AddHttpClient("WebAPI", 
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("WebAPI"));

// Register AzureStorageService as a singleton with error handling
builder.Services.AddSingleton<AzureStorageService>(serviceProvider => 
{
    try 
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AzureStorageService>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new AzureStorageService(logger, configuration);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize AzureStorageService. App will use local storage fallback.");
        return null;
    }
});

// Register our Goals Service as a singleton so data persists during the session
builder.Services.AddSingleton<GoalsService>();

// Register UserService for authentication and user profile management
builder.Services.AddScoped<UserService>();

// Add browser logging
builder.Services.AddLogging(logging => 
{
    logging.SetMinimumLevel(LogLevel.Information);
});

await builder.Build().RunAsync();

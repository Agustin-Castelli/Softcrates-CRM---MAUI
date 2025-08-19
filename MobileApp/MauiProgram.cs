using Core.Interfaces;
using Infrastructure.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MobileApp.Data;
using MobileApp.Services;

namespace MobileApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // Configurar HttpClient por nombre
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("http://localhost:5109/api/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Registrar servicios
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IAuthState, AuthState>();
        builder.Services.AddDbContext<SQLiteDbContext>();
        //builder.Services.AddSingleton<ISyncService, SyncService>();

        // Registrar las interfaces con factory
        builder.Services.AddScoped<UserApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new UserApiService(httpClient);
        });

        builder.Services.AddScoped<ClientApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new ClientApiService(httpClient);
        });

        // === Servicios Local (SQLite) ===
        builder.Services.AddScoped<UserLocalService>();
        builder.Services.AddScoped<ClientLocalService>();

        // === Proxy (lo que la UI realmente inyecta) ===
        builder.Services.AddScoped<IUserService, UserProxyService>();
        builder.Services.AddScoped<IClientService, ClientProxyService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

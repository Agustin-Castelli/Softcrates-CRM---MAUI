using Core.Interfaces;
using Infrastructure.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MobileApp.Services;
using Microsoft.Data.Sqlite;
using MobileApp.Data;


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

        // === DEBUGGING ===
        System.Diagnostics.Debug.WriteLine("[MAUI] Iniciando configuración de DB...");

        // Inicializa/copia la DB semilla a AppDataDirectory

        string dbPath;

        try
        {
            // Fuerza que se copie de nuevo
            dbPath = DatabaseInitializer.EnsureDatabase("CaferLitoralCRM.db");
            System.Diagnostics.Debug.WriteLine($"[MAUI] DB inicializada exitosamente en: {dbPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAUI] ERROR EN INICIALIZACIÓN DE DB: {ex}");
            throw;
        }

        builder.Services.AddScoped<SqliteConnection>(_ =>
            new SqliteConnection($"Data Source={dbPath}"));


        // Configurar HttpClient por nombre             ------------------------>       URL PRUEBAS LOCAL: http://localhost:5109/api/ 
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("http://localhost:5109/api/");  //COLOCAR AQUI LA URL - YA SEA PRUEBAS EN LOCAL, CON SERVIDOR DEDICADO EN NGROK O EN PRODUCCION.
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Registrar servicios
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IAuthState, AuthState>();
        builder.Services.AddSingleton<SyncManager>();
        builder.Services.AddSingleton<ClientStateService>(); // Clase para guardar y enviar data del cliente seleccionado en Home.razor a otros componentes
        builder.Services.AddScoped<CartStateService>(); // Clase para guardar y enviar data del pedido seleccionado en Articulos.razor a CrearPedido.razor
        builder.Services.AddScoped<ArticuloSyncService>();
        builder.Services.AddScoped<PedidoSyncService>();
        builder.Services.AddScoped<CrearPedidoSyncService>();

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

        builder.Services.AddScoped<ArticuloApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new ArticuloApiService(httpClient);
        });

        builder.Services.AddScoped<PedidoApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new PedidoApiService(httpClient);
        });

        builder.Services.AddScoped<CrearPedidoApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new CrearPedidoApiService(httpClient);
        });

        // === Servicios Local (SQLite) ===
        builder.Services.AddScoped<UserLocalService>();
        builder.Services.AddScoped<ClientLocalService>();
        builder.Services.AddScoped<ArticuloLocalService>();
        builder.Services.AddScoped<PedidoLocalService>();
        builder.Services.AddScoped<CrearPedidoLocalService>();

        // === Proxy (lo que la UI realmente inyecta) ===
        builder.Services.AddScoped<IUserService, UserProxyService>();
        builder.Services.AddScoped<IClientService, ClientProxyService>();
        builder.Services.AddScoped<IArticuloService, ArticuloProxyService>();
        builder.Services.AddScoped<IPedidoService, PedidoProxyService>();
        builder.Services.AddScoped<ICrearPedidoService, CrearPedidoProxyService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

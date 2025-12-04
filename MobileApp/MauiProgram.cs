using Core.Interfaces;
using Infrastructure.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MobileApp.Services;
using Microsoft.Data.Sqlite;
using MobileApp.Data;
using SQLitePCL;
using Infrastructure.Local;

namespace MobileApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

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

        // Configuracion de SQLite con AddScoped y sin WAL
        //builder.Services.AddScoped<SqliteConnection>(_ =>
        //    new SqliteConnection($"Data Source={dbPath}"));

        // 🔥 CAMBIAR A SINGLETON Y CONFIGURAR WAL
        builder.Services.AddSingleton<SqliteConnection>(sp =>
        {
            var connString = $"Data Source={dbPath}";
            var conn = new SqliteConnection(connString);

            try
            {
                System.Diagnostics.Debug.WriteLine("[MAUI] Configurando SQLite...");

                // Abrir conexión temporalmente para configurar
                conn.Open();

                // 🔥 Habilitar WAL mode (permite múltiples lecturas simultáneas)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode=WAL;";
                    var result = cmd.ExecuteScalar();
                    System.Diagnostics.Debug.WriteLine($"[MAUI] Journal mode configurado: {result}");
                }

                // 🔥 Aumentar timeout para evitar locks
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout=5000;"; // 5 segundos
                    cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("[MAUI] Busy timeout configurado: 5000ms");
                }

                // 🔥 Cerrar la conexión de configuración
                conn.Close();

                System.Diagnostics.Debug.WriteLine("[MAUI] ✅ SQLite configurado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MAUI] ❌ Error configurando SQLite: {ex.Message}");
                throw;
            }

            return conn;
        });


        // Configurar HttpClient por nombre             ------------------------>       URL PRUEBAS LOCAL: http://localhost:5109/api/ 
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("http://localhost:5109/api/");  //COLOCAR AQUI LA URL - YA SEA PRUEBAS EN LOCAL, CON SERVIDOR DEDICADO EN NGROK O EN PRODUCCION.
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Registrar servicios
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<INetworkService, NetworkService>();   // Clase para corroborar conexión del teléfono a Wi-Fi o Datos Móviles.
        builder.Services.AddSingleton<IDialogService, DialogService>();    // Va de la mano con NetworkService, se utiliza para renderizar el cartel de aviso en vez de utilizar JSRuntime.
        builder.Services.AddSingleton<IAuthState, AuthState>();
        builder.Services.AddSingleton<SyncManager>();
        builder.Services.AddSingleton<ClientStateService>(); // Clase para guardar y enviar data del cliente seleccionado en Home.razor a otros componentes
        builder.Services.AddScoped<CartStateService>(); // Clase para guardar y enviar data del pedido seleccionado en Articulos.razor a CrearPedido.razor
        builder.Services.AddScoped<ArticuloSyncService>();
        builder.Services.AddScoped<PedidoSyncService>();
        builder.Services.AddScoped<CrearPedidoSyncService>();
        builder.Services.AddScoped<BocEntSyncService>();
        builder.Services.AddScoped<BonArtCliSyncService>();
        builder.Services.AddScoped<BonClaDetSyncService>();

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
            var conn = provider.GetRequiredService<SqliteConnection>(); // 🔥 AGREGAR ESTA LÍNEA
            return new CrearPedidoApiService(httpClient, conn); // 🔥 PASAR conn
        });

        builder.Services.AddScoped<BocEntApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new BocEntApiService(httpClient);
        });

        builder.Services.AddScoped<BonificacionApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new BonificacionApiService(httpClient);
        });

        builder.Services.AddScoped<BonArtCliApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new BonArtCliApiService(httpClient);
        });

        builder.Services.AddScoped<BonClaDetApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            return new BonClaDetApiService(httpClient);
        });

        // === Servicios Local (SQLite) ===
        builder.Services.AddScoped<UserLocalService>();
        builder.Services.AddScoped<ClientLocalService>();
        builder.Services.AddScoped<ArticuloLocalService>();
        builder.Services.AddScoped<PedidoLocalService>();
        builder.Services.AddScoped<CrearPedidoLocalService>();
        builder.Services.AddScoped<BocEntLocalService>();
        builder.Services.AddScoped<BonificacionLocalService>();

        // === Proxy (lo que la UI realmente inyecta) ===
        builder.Services.AddScoped<IUserService, UserProxyService>();
        builder.Services.AddScoped<IClientService, ClientProxyService>();
        builder.Services.AddScoped<IArticuloService, ArticuloProxyService>();
        builder.Services.AddScoped<IPedidoService, PedidoProxyService>();
        builder.Services.AddScoped<ICrearPedidoService, CrearPedidoProxyService>();
        builder.Services.AddScoped<IBocEntService, BocEntProxyService>();
        builder.Services.AddScoped<IBonificacionService, BonificacionProxyService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

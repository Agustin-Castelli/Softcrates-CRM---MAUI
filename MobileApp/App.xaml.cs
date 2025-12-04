using MobileApp.Services;

namespace MobileApp
{
    public partial class App : Application
    {
        private readonly SyncManager _syncManager;
        private readonly CrearPedidoSyncService _crearPedidoSync;
        private bool _syncCompleted = false;

        public App(SyncManager syncManager, CrearPedidoSyncService crearPedidoSync)
        {
            InitializeComponent();
            _syncManager = syncManager;
            _crearPedidoSync = crearPedidoSync;
            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            if (_syncCompleted) return; // Ya sincronizó

            try
            {
                System.Diagnostics.Debug.WriteLine("[APP] 🚀 Iniciando sincronización...");

                // 1. Sincronizar artículos y pedidos desde SQL Server hacia SQLite
                await _syncManager.RunAllAsync();
                System.Diagnostics.Debug.WriteLine("[APP] ✅ Artículos y pedidos sincronizados");

                // 2. Enviar pedidos pendientes desde SQLite hacia SQL Server
                await _crearPedidoSync.SyncPedidosPendientesAsync();
                System.Diagnostics.Debug.WriteLine("[APP] ✅ Pedidos pendientes enviados");

                _syncCompleted = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] ❌ Error en sincronización: {ex.Message}");
            }
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Resetear el flag para que sincronice cuando vuelvas a abrir la app
            _syncCompleted = false;
            System.Diagnostics.Debug.WriteLine("[APP] 💤 App suspendida, flag de sync reseteado");
        }

        protected override void OnResume()
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine("[APP] 👋 App resumida");
        }
    }
}


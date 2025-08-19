//using Core.Interfaces;
//using MobileApp.Data;
//using MobileApp.Models;

//namespace MobileApp.Services
//{
//    public class SyncService : ISyncService
//    {
//        private readonly SQLiteDbContext _dbContext;
//        private readonly IConnectivityService _connectivityService;

//        public SyncService(SQLiteDbContext dbContext, IConnectivityService connectivityService)
//        {
//            _dbContext = dbContext;
//            _connectivityService = connectivityService;

//            _connectivityService.ConnectivityChanged += async (_, isConnected) =>
//            {
//                if (isConnected)
//                {
//                    await SyncPendingOrdersAsync();
//                }
//            };
//        }

//        public async Task SyncPendingOrdersAsync()
//        {
//            var pendientes = _dbContext.PendingOrders
//                .Where(o => !o.Sincronizado)
//                .ToList();

//            foreach (var order in pendientes)
//            {
//                try
//                {
//                    // Acá deberías usar un servicio HttpClient para enviar al backend.
//                    // Simulación:
//                    await Task.Delay(100); // simular HTTP call

//                    // Si se envió correctamente:
//                    order.Sincronizado = true;
//                }
//                catch
//                {
//                    // Loggear o dejarlo pendiente
//                }
//            }

//            await _dbContext.SaveChangesAsync();
//        }
//    }
//}


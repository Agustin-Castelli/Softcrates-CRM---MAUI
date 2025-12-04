using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class SyncManager
    {
        private readonly ArticuloSyncService _articuloSync;
        private readonly PedidoSyncService _pedidoSync;
        private readonly BocEntSyncService _bocEntSync;
        private readonly BonArtCliSyncService _bonArtCliSync;
        private readonly BonClaDetSyncService _bonClaDetSync;

        public SyncManager(ArticuloSyncService articuloSync, 
                           PedidoSyncService pedidoSync, 
                           BocEntSyncService bocEntSync, 
                           BonArtCliSyncService bonArtCliSync, 
                           BonClaDetSyncService bonClaDetSync)
        {
            _articuloSync = articuloSync;
            _pedidoSync = pedidoSync;
            _bocEntSync = bocEntSync;
            _bonArtCliSync = bonArtCliSync;
            _bonClaDetSync = bonClaDetSync;
        }

        public async Task RunAllAsync(CancellationToken ct = default)
        {
            await _articuloSync.SyncAsync(ct);
            await _pedidoSync.SyncAsync(ct);
            await _bocEntSync.SyncAsync(ct);
            await _bonArtCliSync.SyncAsync(ct);
            await _bonClaDetSync.SyncAsync(ct);
        }
    }

}

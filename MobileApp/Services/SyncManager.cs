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

        public SyncManager(ArticuloSyncService articuloSync, PedidoSyncService pedidoSync)
        {
            _articuloSync = articuloSync;
            _pedidoSync = pedidoSync;
        }

        public async Task RunAllAsync(CancellationToken ct = default)
        {
            await _articuloSync.SyncAsync(ct);
            await _pedidoSync.SyncAsync(ct);
        }
    }

}

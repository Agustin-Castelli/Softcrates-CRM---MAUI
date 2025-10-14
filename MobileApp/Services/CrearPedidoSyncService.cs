using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Infrastructure.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class CrearPedidoSyncService
    {
        private readonly CrearPedidoApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<CrearPedidoSyncService> _logger;
        private readonly object _syncLock = new();

        public CrearPedidoSyncService(
            CrearPedidoApiService apiService,
            SqliteConnection conn,
            ILogger<CrearPedidoSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        public async Task SyncPedidosPendientesAsync()
        {
            lock (_syncLock)
            {
                // prevenimos que se ejecute en paralelo
            }

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                // 1. Traer cabeceras pendientes
                var cabeceras = await _conn.QueryAsync<CrearPedidoSyncDTO>(@"
            SELECT 
                CodTipCbt,
                CemCbt,
                NroCbt,
                CodCli,
                Estado,
                Csid
            FROM PedWebCab
            WHERE Estado = 'Pendiente'
        ");

                var pedidos = new List<CrearPedidoDTO>();

                foreach (var cab in cabeceras)
                {
                    // 2. Traer detalles asociados
                    var detalles = await _conn.QueryAsync<CrearPedidoDetalleDTO>(@"
                SELECT 
                    CodArt,
                    DesArtAmp AS DesArt,
                    CanArt AS Cantidad,
                    PreArt AS PrecioUnitario
                FROM PedWebArt
                WHERE Csid = @Csid
                ORDER BY Secuencia
            ", new { cab.Csid });

                    // 3. Mapear a DTO limpio para la API
                    pedidos.Add(new CrearPedidoDTO
                    {
                        CodTipCbt = cab.CodTipCbt,
                        CemCbt = cab.CemCbt,
                        CodCli = cab.CodCli,
                        Detalles = detalles.ToList()
                    });
                }

                if (pedidos.Any())
                {
                    // 4. Enviar todos a la API en un solo request
                    var result = await _apiService.CrearPedidosMasivoAsync(pedidos);

                    if (result != null && result.Any())
                    {
                        // 5. Si se sincronizó, marcar todos como Sincronizado
                        await _conn.ExecuteAsync(@"
                    UPDATE PedWebCab
                    SET Estado = 'Sincronizado'
                    WHERE Estado = 'Pendiente'
                ");

                        _logger.LogInformation($"[SyncPedidos] {pedidos.Count} pedidos sincronizados correctamente.");
                    }
                    else
                    {
                        _logger.LogWarning("[SyncPedidos] La API no devolvió pedidos sincronizados.");
                    }
                }
                else
                {
                    _logger.LogInformation("[SyncPedidos] No hay pedidos pendientes para sincronizar.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SyncPedidos] Error al sincronizar pedidos.");
            }
        }


    }

}

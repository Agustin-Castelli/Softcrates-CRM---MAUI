using Core.DTOs;
using Core.DTOs.Core.DTOs;
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
        private readonly PedidoSyncService _pedidoSyncService; // 🔥 NUEVO
        private readonly object _syncLock = new();

        public CrearPedidoSyncService(
            CrearPedidoApiService apiService,
            SqliteConnection conn,
            ILogger<CrearPedidoSyncService> logger,
            PedidoSyncService pedidoSyncService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
            _pedidoSyncService = pedidoSyncService;
        }

        // 🔥 Clases intermedias para mapear desde SQLite (que devuelve long/int)
        private class PedidoPendienteSQLite
        {
            public int CodTipCbt { get; set; }      // SQLite devuelve int, lo convertiremos a short
            public int CemCbt { get; set; }         // SQLite devuelve int, lo convertiremos a short
            public int NroCbt { get; set; }
            public int CodCli { get; set; }
            public int Confirmado { get; set; }
            public int NroBocEnt { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string Csid { get; set; } = string.Empty;
        }

        private class DetallePendienteSQLite
        {
            public string CodArt { get; set; } = string.Empty;
            public string DesArt { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public async Task SyncPedidosPendientesAsync()
        {
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                _logger?.LogWarning("[SyncPedidos] Ya hay una sincronización en curso, saltando...");
                return;
            }

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                var cabecerasSQLite = await _conn.QueryAsync<PedidoPendienteSQLite>(@"
            SELECT 
                CodTipCbt, CemCbt, NroCbt, CodCli, NroBocEnt, Confirmado, Estado, Csid
            FROM PedWebCab
            WHERE Estado = 'Pendiente' AND OrigenPedido = 'Local'
        ");

                if (!cabecerasSQLite.Any())
                {
                    _logger?.LogInformation("[SyncPedidos] No hay pedidos pendientes para sincronizar.");
                    return;
                }

                _logger?.LogInformation("[SyncPedidos] Encontrados {count} pedidos pendientes", cabecerasSQLite.Count());

                var pedidos = new List<CrearPedidoDTO>();
                var csidsOriginales = new List<string>();

                foreach (var cabSQLite in cabecerasSQLite)
                {
                    _logger?.LogInformation("[SyncPedidos] Procesando pedido local con Csid={csid}", cabSQLite.Csid);
                    csidsOriginales.Add(cabSQLite.Csid);

                    var detallesSQLite = await _conn.QueryAsync<DetallePendienteSQLite>(@"
                SELECT CodArt, DesArtAmp AS DesArt, CanArt AS Cantidad, PreArt AS PrecioUnitario
                FROM PedWebArt
                WHERE Csid = @Csid
                ORDER BY Secuencia
            ", new { cabSQLite.Csid });

                    pedidos.Add(new CrearPedidoDTO
                    {
                        CodTipCbt = (short)cabSQLite.CodTipCbt,
                        CemCbt = (short)cabSQLite.CemCbt,
                        CodCli = cabSQLite.CodCli,
                        NroBocEnt = cabSQLite.NroBocEnt,
                        Confirmado = cabSQLite.Confirmado,
                        Detalles = detallesSQLite.Select(d => new CrearPedidoDetalleDTO
                        {
                            CodArt = d.CodArt,
                            DesArt = d.DesArt,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario
                        }).ToList()
                    });
                }

                _logger?.LogInformation("[SyncPedidos] Enviando {count} pedidos a la API...", pedidos.Count);

                var resultado = await _apiService.CrearPedidosMasivoAsync(pedidos);

                if (resultado != null && resultado.Any())
                {
                    _logger?.LogInformation("[SyncPedidos] API confirmó {count} pedidos, borrando locales...", resultado.Count());
                    _logger?.LogInformation("[SyncPedidos] Csids a borrar: {csids}", string.Join(", ", csidsOriginales));

                    // Borrar detalles primero
                    var deletedArt = await _conn.ExecuteAsync(@"DELETE FROM PedWebArt WHERE Csid IN @Csids;", new { Csids = csidsOriginales });
                    _logger?.LogInformation("[SyncPedidos] Detalles borrados: {count}", deletedArt);

                    // Borrar cabeceras
                    var deletedCab = await _conn.ExecuteAsync(@"DELETE FROM PedWebCab WHERE Csid IN @Csids;", new { Csids = csidsOriginales });
                    _logger?.LogInformation("[SyncPedidos] Cabeceras borradas: {count}", deletedCab);

                    _logger?.LogInformation("[SyncPedidos] ✅ {count} pedidos sincronizados y eliminados de SQLite.", csidsOriginales.Count);

                    // 🔥 CERRAR CONEXIÓN ANTES de llamar a PedidoSyncService
                    if (_conn.State == ConnectionState.Open)
                    {
                        await _conn.CloseAsync();
                        _logger?.LogInformation("[SyncPedidos] Conexión cerrada antes de sincronización de bajada");
                    }

                    // 🔥 AHORA SÍ llamar a PedidoSyncService (que abrirá su propia conexión)
                    _logger?.LogInformation("[SyncPedidos] Ejecutando sincronización de bajada...");
                    await _pedidoSyncService.SyncAsync();
                }
                else
                {
                    _logger?.LogWarning("[SyncPedidos] ⚠️ La API no devolvió pedidos sincronizados.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[SyncPedidos] Error al sincronizar pedidos.");
            }
            finally
            {
                // 🔥 Asegurar que siempre se cierra la conexión
                if (_conn.State == ConnectionState.Open)
                {
                    await _conn.CloseAsync();
                }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }


    }

}

using Core.DTOs; // Ajustá el namespace si corresponde
using Core.DTOs.Core.DTOs;
using Dapper;
using Infrastructure.Api; // para ArticuloApiService
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class SyncResult
    {
        public int TotalFetched { get; set; }
        public int InsertedOrUpdatedArtic { get; set; }
        public int InsertedOrUpdatedPreVen { get; set; }
        public int Failed { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool Success => Failed == 0;

        // Factory methods para simplificar el retorno
        public static SyncResult Ok(string message) => new SyncResult
        {
            Message = message
        };

        public static SyncResult Fail(string message) => new SyncResult
        {
            Failed = 1,
            Message = message
        };
    }

    public class ArticuloSyncService
    {
        private readonly ArticuloApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<ArticuloSyncService> _logger;
        private readonly object _syncLock = new();

        public ArticuloSyncService(
            ArticuloApiService apiService,
            SqliteConnection conn,
            ILogger<ArticuloSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza artículos desde la API hacia las tablas Artic y PreVen de SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            // evitemos sincronizaciones concurrentes
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de artículos...");

                var result = new SyncResult();

                // 1) Obtener artículos desde la API (ArticuloDTO con Precio)
                IEnumerable<ArticuloDTO> listado;
                try
                {
                    listado = await _apiService.GetAll();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SYNC] Error al obtener artículos desde API");
                    return new SyncResult { Message = $"Error al obtener artículos: {ex.Message}" };
                }

                var lista = listado?.ToList() ?? new List<ArticuloDTO>();
                result.TotalFetched = lista.Count;
                if (!lista.Any())
                {
                    result.Message = "No se encontraron artículos en la API";
                    return result;
                }

                // 2) Abrir conexión SQLite y transacción
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);



                // --- INICIO: BLOQUE DIAGNÓSTICO (pegar aquí) ---
                try
                {
                    // Loguear connection string (para ver qué fichero se está usando)
                    _logger?.LogInformation("[SYNC] ConnectionString: {cs}", _conn.ConnectionString);

                    // Listar tablas existentes en la DB
                    var tables = (await _conn.QueryAsync<string>(
                        "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")).ToList();

                    if (tables == null || tables.Count == 0)
                        _logger?.LogWarning("[SYNC] No se encontraron tablas en la DB (sqlite_master vacío)");
                    else
                        _logger?.LogInformation("[SYNC] Tablas en DB: {tables}", string.Join(", ", tables));

                    // Mostrar esquema de la tabla Artic (si existe)
                    var artSchema = await _conn.QueryFirstOrDefaultAsync<string>(
                        "SELECT sql FROM sqlite_master WHERE type='table' AND name = @name;", new { name = "Artic" });

                    _logger?.LogInformation("[SYNC] Schema Artic: {schema}", artSchema ?? "<no existe tabla Artic>");
                }
                catch (Exception exDiag)
                {
                    _logger?.LogError(exDiag, "[SYNC] Error diagnosticando DB antes de iniciar transacción");
                    // opcional: devolver un resultado informativo y abortar la sync para evitar daños
                    return new SyncResult { Message = $"Error diagnostico DB: {exDiag.Message}" };
                }
                // --- FIN: BLOQUE DIAGNÓSTICO ---



                using var transaction = _conn.BeginTransaction();

                try
                {
                    // Preparar sentencias upsert (SQLite >= 3.24: INSERT ... ON CONFLICT DO UPDATE)
                    const string upsertArtic = @"
                                                    INSERT INTO Artic (CodArt, DesArt)
                                                    VALUES (@CodArt, @DesArt)
                                                    ON CONFLICT(CodArt) DO UPDATE SET
                                                        DesArt = excluded.DesArt;";

                    const string upsertPreVen = @"
                                                    INSERT INTO PreVen (CodList, Codart, codunimed, CodMon, Precio, FecUltMod, FecIng, UsrIng, WksIng, fecexp)
                                                    VALUES (@CodList, @CodArt, @CodUni, @CodMon, @Precio, @FecUltMod, @FecIng, @UsrIng, @WksIng, @FecExp)
                                                    ON CONFLICT(CodList, Codart, codunimed) DO UPDATE SET
                                                        Precio = excluded.Precio,
                                                        FecUltMod = excluded.FecUltMod,
                                                        FecIng = excluded.FecIng,
                                                        UsrIng = excluded.UsrIng;";

                    int countArtic = 0;
                    int countPreVen = 0;

                    // Valores por defecto para tabla PreVen (ajustalos si los necesitás distintos)
                    const short defaultCodList = 1;
                    const string defaultCodUni = "UN"; // 2 chars
                    const short defaultCodMon = 1;
                    var now = DateTime.UtcNow;

                    foreach (var art in lista)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pArtic = new
                        {
                            CodArt = art.CodArt?.Trim(),
                            DesArt = art.DesArt?.Trim()
                        };

                        await _conn.ExecuteAsync(upsertArtic, pArtic, transaction: transaction);
                        countArtic++;

                        var pPre = new
                        {
                            CodList = defaultCodList,
                            CodArt = art.CodArt?.Trim(),
                            CodUni = defaultCodUni,
                            CodMon = defaultCodMon,
                            Precio = art.Precio,
                            FecUltMod = now,
                            FecIng = now,
                            UsrIng = "sync",
                            WksIng = "MOB",
                            FecExp = (DateTime?)null
                        };

                        await _conn.ExecuteAsync(upsertPreVen, pPre, transaction: transaction);
                        countPreVen++;
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedArtic = countArtic;
                    result.InsertedOrUpdatedPreVen = countPreVen;
                    result.Message = $"Sincronización completada. Artículos procesados: {countArtic}, Precios: {countPreVen}";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                }
                catch (Exception exTx)
                {
                    try { transaction.Rollback(); } catch { }
                    _logger?.LogError(exTx, "[SYNC] Error dentro de la transacción, rollback ejecutado.");
                    result.Failed = lista.Count; // indicativo
                    result.Message = $"Error al insertar en SQLite: {exTx.Message}";
                }

                return result;
            }
            finally
            {
                try
                {
                    if (_conn.State == ConnectionState.Open)
                        await _conn.CloseAsync();
                }
                catch { }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }
    }






    public class PedidoSyncService
    {
        private readonly PedidoApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<PedidoSyncService> _logger;
        private readonly object _syncLock = new();

        public PedidoSyncService(
            PedidoApiService apiService,
            SqliteConnection conn,
            ILogger<PedidoSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza PedWebCab y PedWebArt desde la API hacia SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización de pedidos ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de pedidos desde SQL Server...");

                var result = new SyncResult();

                // 1. Obtener TODOS los pedidos desde SQL Server
                IEnumerable<PedidoDTO> listado;
                try
                {
                    listado = await _apiService.GetAllPedidos();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SYNC] Error al obtener pedidos desde API");
                    return new SyncResult { Message = $"Error al obtener pedidos: {ex.Message}" };
                }

                var lista = listado?.ToList() ?? new List<PedidoDTO>();
                result.TotalFetched = lista.Count;

                if (!lista.Any())
                {
                    result.Message = "No se encontraron pedidos en SQL Server";
                    return result;
                }

                // 2. Abrir conexión SQLite
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);

                // 3. Obtener los csid que YA EXISTEN en SQLite con origen 'Servidor'
                var csidsExistentes = (await _conn.QueryAsync<string>(
                    "SELECT csid FROM PedWebCab WHERE OrigenPedido = 'Servidor';"
                )).ToHashSet();

                _logger?.LogInformation("[SYNC] Csids existentes en SQLite: {csids}", string.Join(", ", csidsExistentes));
                _logger?.LogInformation("[SYNC] Pedidos ya sincronizados en SQLite: {count}", csidsExistentes.Count);

                // 4. Filtrar solo los pedidos NUEVOS (que no están en SQLite)
                var pedidosNuevos = lista.Where(p => !csidsExistentes.Contains(p.Csid)).ToList();

                _logger?.LogInformation("[SYNC] Csids nuevos desde servidor: {csids}", string.Join(", ", pedidosNuevos.Select(p => p.Csid)));

                if (!pedidosNuevos.Any())
                {
                    result.Message = "No hay nuevos pedidos para sincronizar";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                    return result;
                }

                _logger?.LogInformation("[SYNC] Se sincronizarán {count} pedidos nuevos", pedidosNuevos.Count);

                using var transaction = _conn.BeginTransaction();

                try
                {
                    const string insertCabecera = @"
INSERT OR IGNORE INTO PedWebCab (
    codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli, codiva, codsuc, codlist, codclabon,
    codven, codtra, codconvta, codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
    impnetgracbt, impivacbt, imptotcbt, obscbt, autped, usringreso, fecingreso, usraut, fecaut,
    nrobocent, pedwebinc, obscomunicado, fecpedwebinc, AutPedSec, UsrAutSec, FecAutSec, Confirmado,
    Estado, OrigenPedido
) VALUES (
    @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli, @CodIva, @CodSuc, @CodList, @CodClaBon,
    @CodVen, @CodTra, @CodConVta, @CodMon, @ImpCot, @ImpGraCbt, @PorDesCbt, @ImpDesCbt, @PorRecCbt, @ImpRecCbt,
    @ImpNetGraCbt, @ImpIvaCbt, @ImpTotCbt, @ObsCbt, @AutPed, @UsrIngreso, @FecIngreso, @UsrAut, @FecAut,
    @NroBocEnt, @PedWebInc, @ObsComunicado, @FecPedWebInc, @AutPedSec, @UsrAutSec, @FecAutSec, @Confirmado,
    @Estado, @OrigenPedido
);";

                    const string insertDetalle = @"
INSERT OR IGNORE INTO PedWebArt (
    csid, secuencia, codart, codivaart, codclabon, desartamp, fecentart, canart, preart,
    impbonart, impgraart, impdesart, imprecart, impnetgraart, impivaart
) VALUES (
    @Csid, @Secuencia, @CodArt, @CodIvaArt, @CodClaBon, @DesArtAmp, @FecEntArt, @CanArt, @PreArt,
    @ImpBonArt, @ImpGraArt, @ImpDesArt, @ImpRecArt, @ImpNetGraArt, @ImpIvaArt
);";

                    int countCabeceras = 0;
                    int countDetalles = 0;

                    foreach (var pedido in pedidosNuevos)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pCab = new
                        {
                            pedido.CodTipCbt,
                            pedido.CemCbt,
                            pedido.NroCbt,
                            pedido.Csid,
                            pedido.FecCbt,
                            pedido.FecEntCbt,
                            pedido.CodCli,
                            pedido.CodIva,
                            pedido.CodSuc,
                            pedido.CodList,
                            pedido.CodClaBon,
                            pedido.CodVen,
                            pedido.CodTra,
                            pedido.CodConVta,
                            pedido.CodMon,
                            pedido.ImpCot,
                            pedido.ImpGraCbt,
                            pedido.PorDesCbt,
                            pedido.ImpDesCbt,
                            pedido.PorRecCbt,
                            pedido.ImpRecCbt,
                            pedido.ImpNetGraCbt,
                            pedido.ImpIvaCbt,
                            pedido.ImpTotCbt,
                            pedido.ObsCbt,
                            pedido.AutPed,
                            UsrIngreso = "sync",
                            FecIngreso = DateTime.UtcNow,
                            pedido.UsrAut,
                            pedido.FecAut,
                            pedido.NroBocEnt,
                            pedido.PedWebInc,
                            pedido.ObsComunicado,
                            pedido.FecPedWebInc,
                            pedido.AutPedSec,
                            pedido.UsrAutSec,
                            pedido.FecAutSec,
                            pedido.Confirmado,
                            Estado = "Sincronizado",      // Viene del servidor, ya está sincronizado
                            OrigenPedido = "Servidor"     // MARCA IMPORTANTE
                        };

                        await _conn.ExecuteAsync(insertCabecera, pCab, transaction: transaction);
                        countCabeceras++;

                        if (pedido.Detalles != null && pedido.Detalles.Any())
                        {
                            foreach (var det in pedido.Detalles)
                            {
                                var pDet = new
                                {
                                    det.Csid,
                                    det.Secuencia,
                                    det.CodArt,
                                    det.CodIvaArt,
                                    det.CodClaBon,
                                    det.DesArtAmp,
                                    det.FecEntArt,
                                    det.CanArt,
                                    det.PreArt,
                                    det.ImpBonArt,
                                    det.ImpGraArt,
                                    det.ImpDesArt,
                                    det.ImpRecArt,
                                    det.ImpNetGraArt,
                                    det.ImpIvaArt
                                };

                                await _conn.ExecuteAsync(insertDetalle, pDet, transaction: transaction);
                                countDetalles++;
                            }
                        }
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedArtic = countCabeceras;
                    result.InsertedOrUpdatedPreVen = countDetalles;
                    result.Message = $"Sincronización completada. Cabeceras nuevas: {countCabeceras}, Detalles: {countDetalles}";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                }
                catch (Exception exTx)
                {
                    try { transaction.Rollback(); } catch { }
                    _logger?.LogError(exTx, "[SYNC] Error dentro de la transacción, rollback ejecutado.");
                    result.Failed = pedidosNuevos.Count;
                    result.Message = $"Error al insertar en SQLite: {exTx.Message}";
                }

                return result;
            }
            finally
            {
                try
                {
                    if (_conn.State == ConnectionState.Open)
                        await _conn.CloseAsync();
                }
                catch { }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }
    }






    

    public class BocEntSyncService
    {
        private readonly BocEntApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<BocEntSyncService> _logger;
        private readonly object _syncLock = new();

        public BocEntSyncService(
            BocEntApiService apiService,
            SqliteConnection conn,
            ILogger<BocEntSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza BocEnt desde la API hacia SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización de bocas de entrega ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de bocas de entrega...");

                var result = new SyncResult();

                // 1. Obtener todas las bocas desde la API
                IEnumerable<BocaEntregaDTO> listado;
                try
                {
                    listado = await _apiService.GetAll();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SYNC] Error al obtener bocas desde API");
                    return new SyncResult { Message = $"Error al obtener bocas: {ex.Message}" };
                }

                var lista = listado?.ToList() ?? new List<BocaEntregaDTO>();
                result.TotalFetched = lista.Count;

                if (!lista.Any())
                {
                    result.Message = "No se encontraron bocas de entrega en la API";
                    return result;
                }

                // 2. Abrir conexión SQLite
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);

                using var transaction = _conn.BeginTransaction();

                try
                {
                    // 3. Limpiar tabla antes de insertar (estrategia: reemplazar todo)
                    await _conn.ExecuteAsync("DELETE FROM BocEnt;", transaction: transaction);
                    _logger?.LogInformation("[SYNC] Tabla BocEnt limpiada");

                    // 4. Insertar todas las bocas
                    const string insertBoca = @"
                    INSERT INTO BocEnt (codcli, nrobocent, nombocent, dombocent)
                    VALUES (@CodCli, @NroBocEnt, @NomBocEnt, @DomBocEnt);";

                    int count = 0;

                    foreach (var boca in lista)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pBoca = new
                        {
                            boca.CodCli,
                            boca.NroBocEnt,
                            boca.NomBocEnt,
                            boca.DomBocEnt
                        };

                        await _conn.ExecuteAsync(insertBoca, pBoca, transaction: transaction);
                        count++;
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedArtic = count; // reutilizamos el campo
                    result.Message = $"Sincronización completada. Bocas procesadas: {count}";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                }
                catch (Exception exTx)
                {
                    try { transaction.Rollback(); } catch { }
                    _logger?.LogError(exTx, "[SYNC] Error dentro de la transacción, rollback ejecutado.");
                    result.Failed = lista.Count;
                    result.Message = $"Error al insertar en SQLite: {exTx.Message}";
                }

                return result;
            }
            finally
            {
                try
                {
                    if (_conn.State == ConnectionState.Open)
                        await _conn.CloseAsync();
                }
                catch { }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }
    }

    public class BonArtCliSyncService
    {
        private readonly BonArtCliApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<BonArtCliSyncService> _logger;
        private readonly object _syncLock = new();

        public BonArtCliSyncService(
            BonArtCliApiService apiService,
            SqliteConnection conn,
            ILogger<BonArtCliSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza BonArtCli desde la API hacia SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización de BonArtCli ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de BonArtCli...");

                var result = new SyncResult();

                // 1. Obtener todas las asignaciones desde la API
                IEnumerable<BonArtCliDTO> listado;
                try
                {
                    listado = await _apiService.GetAll();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SYNC] Error al obtener BonArtCli desde API");
                    return new SyncResult { Message = $"Error al obtener BonArtCli: {ex.Message}" };
                }

                var lista = listado?.ToList() ?? new List<BonArtCliDTO>();
                result.TotalFetched = lista.Count;

                if (!lista.Any())
                {
                    result.Message = "No se encontraron asignaciones de bonificaciones en la API";
                    return result;
                }

                // 2. Abrir conexión SQLite
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);

                using var transaction = _conn.BeginTransaction();

                try
                {
                    // 3. Limpiar tabla antes de insertar (estrategia: reemplazar todo)
                    await _conn.ExecuteAsync("DELETE FROM BonArtCli;", transaction: transaction);
                    _logger?.LogInformation("[SYNC] Tabla BonArtCli limpiada");

                    // 4. Insertar todas las asignaciones
                    const string insertBonArtCli = @"
                    INSERT INTO BonArtCli (CodCli, CodArt, CodClaBon, inactivo)
                    VALUES (@CodCli, @CodArt, @CodClaBon, @Inactivo);";

                    int count = 0;

                    foreach (var bon in lista)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pBon = new
                        {
                            bon.CodCli,
                            bon.CodArt,
                            bon.CodClaBon,
                            Inactivo = bon.Inactivo ? 1 : 0 // SQLite usa 0/1 para boolean
                        };

                        await _conn.ExecuteAsync(insertBonArtCli, pBon, transaction: transaction);
                        count++;
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedArtic = count;
                    result.Message = $"Sincronización completada. BonArtCli procesados: {count}";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                }
                catch (Exception exTx)
                {
                    try { transaction.Rollback(); } catch { }
                    _logger?.LogError(exTx, "[SYNC] Error dentro de la transacción, rollback ejecutado.");
                    result.Failed = lista.Count;
                    result.Message = $"Error al insertar en SQLite: {exTx.Message}";
                }

                return result;
            }
            finally
            {
                try
                {
                    if (_conn.State == ConnectionState.Open)
                        await _conn.CloseAsync();
                }
                catch { }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }
    }



    public class BonClaDetSyncService
    {
        private readonly BonClaDetApiService _apiService;
        private readonly SqliteConnection _conn;
        private readonly ILogger<BonClaDetSyncService> _logger;
        private readonly object _syncLock = new();

        public BonClaDetSyncService(
            BonClaDetApiService apiService,
            SqliteConnection conn,
            ILogger<BonClaDetSyncService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza BonClaDet desde la API hacia SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización de BonClaDet ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de BonClaDet...");

                var result = new SyncResult();

                // 1. Obtener todos los detalles desde la API
                IEnumerable<BonClaDetDTO> listado;
                try
                {
                    listado = await _apiService.GetAll();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SYNC] Error al obtener BonClaDet desde API");
                    return new SyncResult { Message = $"Error al obtener BonClaDet: {ex.Message}" };
                }

                var lista = listado?.ToList() ?? new List<BonClaDetDTO>();
                result.TotalFetched = lista.Count;

                if (!lista.Any())
                {
                    result.Message = "No se encontraron detalles de bonificaciones en la API";
                    return result;
                }

                // 2. Abrir conexión SQLite
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);

                using var transaction = _conn.BeginTransaction();

                try
                {
                    // 3. Limpiar tabla antes de insertar (estrategia: reemplazar todo)
                    await _conn.ExecuteAsync("DELETE FROM BonClaDet;", transaction: transaction);
                    _logger?.LogInformation("[SYNC] Tabla BonClaDet limpiada");

                    // 4. Insertar todos los detalles
                    const string insertBonClaDet = @"
                    INSERT INTO BonClaDet (
                        CodClaBon, Secuencia, TipEsc, ValEscDes, ValEscHas, 
                        PorBonImp, PorBonCan, disfac
                    ) VALUES (
                        @CodClaBon, @Secuencia, @TipEsc, @ValEscDes, @ValEscHas, 
                        @PorBonImp, @PorBonCan, @DisFac
                    );";

                    int count = 0;

                    foreach (var det in lista)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pDet = new
                        {
                            det.CodClaBon,
                            det.Secuencia,
                            det.TipEsc,
                            det.ValEscDes,
                            det.ValEscHas,
                            det.PorBonImp,
                            det.PorBonCan,
                            det.DisFac
                        };

                        await _conn.ExecuteAsync(insertBonClaDet, pDet, transaction: transaction);
                        count++;
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedPreVen = count;
                    result.Message = $"Sincronización completada. BonClaDet procesados: {count}";
                    _logger?.LogInformation("[SYNC] {Message}", result.Message);
                }
                catch (Exception exTx)
                {
                    try { transaction.Rollback(); } catch { }
                    _logger?.LogError(exTx, "[SYNC] Error dentro de la transacción, rollback ejecutado.");
                    result.Failed = lista.Count;
                    result.Message = $"Error al insertar en SQLite: {exTx.Message}";
                }

                return result;
            }
            finally
            {
                try
                {
                    if (_conn.State == ConnectionState.Open)
                        await _conn.CloseAsync();
                }
                catch { }

                System.Threading.Monitor.Exit(_syncLock);
            }
        }
    }
}

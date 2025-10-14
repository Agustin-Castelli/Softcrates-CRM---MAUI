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
            // Evitar sincronizaciones concurrentes
            if (!System.Threading.Monitor.TryEnter(_syncLock))
            {
                return new SyncResult { Message = "Sincronización de pedidos ya en curso" };
            }

            try
            {
                _logger?.LogInformation("[SYNC] Iniciando sincronización de pedidos...");

                var result = new SyncResult();

                // 1) Obtener pedidos desde la API
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
                    result.Message = "No se encontraron pedidos en la API";
                    return result;
                }

                // 2) Abrir conexión SQLite y transacción
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(ct);

                // Diagnóstico: listar tablas y esquema
                try
                {
                    _logger?.LogInformation("[SYNC] ConnectionString: {cs}", _conn.ConnectionString);
                    var tables = (await _conn.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")).ToList();
                    _logger?.LogInformation("[SYNC] Tablas en DB: {tables}", tables == null ? "<none>" : string.Join(", ", tables));
                    var schemaCab = await _conn.QueryFirstOrDefaultAsync<string>("SELECT sql FROM sqlite_master WHERE type='table' AND name = @name;", new { name = "PedWebCab" });
                    var schemaArt = await _conn.QueryFirstOrDefaultAsync<string>("SELECT sql FROM sqlite_master WHERE type='table' AND name = @name;", new { name = "PedWebArt" });
                    _logger?.LogInformation("[SYNC] Schema PedWebCab: {schema}", schemaCab ?? "<no existe PedWebCab>");
                    _logger?.LogInformation("[SYNC] Schema PedWebArt: {schema}", schemaArt ?? "<no existe PedWebArt>");
                }
                catch (Exception exDiag)
                {
                    _logger?.LogWarning(exDiag, "[SYNC] Error diagnóstico DB (continuando de todas formas): {msg}", exDiag.Message);
                }

                using var transaction = _conn.BeginTransaction();

                try
                {
                    // Upsert para PedWebCab
                    const string upsertCabecera = @"
INSERT INTO PedWebCab (
    codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli, codiva, codsuc, codlist, codclabon,
    codven, codtra, codconvta, codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
    impnetgracbt, impivacbt, imptotcbt, obscbt, autped, usringreso, fecingreso, usraut, fecaut,
    nrobocent, pedwebinc, obscomunicado, fecpedwebinc, AutPedSec, UsrAutSec, FecAutSec
) VALUES (
    @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli, @CodIva, @CodSuc, @CodList, @CodClaBon,
    @CodVen, @CodTra, @CodConVta, @CodMon, @ImpCot, @ImpGraCbt, @PorDesCbt, @ImpDesCbt, @PorRecCbt, @ImpRecCbt,
    @ImpNetGraCbt, @ImpIvaCbt, @ImpTotCbt, @ObsCbt, @AutPed, @UsrIngreso, @FecIngreso, @UsrAut, @FecAut,
    @NroBocEnt, @PedWebInc, @ObsComunicado, @FecPedWebInc, @AutPedSec, @UsrAutSec, @FecAutSec
)
ON CONFLICT(csid) DO UPDATE SET
    codtipcbt = excluded.codtipcbt,
    cemcbt = excluded.cemcbt,
    nrocbt = excluded.nrocbt,
    feccbt = excluded.feccbt,
    fecentcbt = excluded.fecentcbt,
    codcli = excluded.codcli,
    codiva = excluded.codiva,
    codsuc = excluded.codsuc,
    codlist = excluded.codlist,
    codclabon = excluded.codclabon,
    codven = excluded.codven,
    codtra = excluded.codtra,
    codconvta = excluded.codconvta,
    codmon = excluded.codmon,
    impcot = excluded.impcot,
    impgracbt = excluded.impgracbt,
    pordescbt = excluded.pordescbt,
    impdescbt = excluded.impdescbt,
    porreccbt = excluded.porreccbt,
    impreccbt = excluded.impreccbt,
    impnetgracbt = excluded.impnetgracbt,
    impivacbt = excluded.impivacbt,
    imptotcbt = excluded.imptotcbt,
    obscbt = excluded.obscbt,
    autped = excluded.autped,
    usringreso = excluded.usringreso,
    fecingreso = excluded.fecingreso,
    usraut = excluded.usraut,
    fecaut = excluded.fecaut,
    nrobocent = excluded.nrobocent,
    pedwebinc = excluded.pedwebinc,
    obscomunicado = excluded.obscomunicado,
    fecpedwebinc = excluded.fecpedwebinc,
    AutPedSec = excluded.AutPedSec,
    UsrAutSec = excluded.UsrAutSec,
    FecAutSec = excluded.FecAutSec;";

                    // Upsert para PedWebArt
                    const string upsertDetalle = @"
INSERT INTO PedWebArt (
    csid, secuencia, codart, codivaart, codclabon, desartamp, fecentart, canart, preart,
    impbonart, impgraart, impdesart, imprecart, impnetgraart, impivaart
) VALUES (
    @Csid, @Secuencia, @CodArt, @CodIvaArt, @CodClaBon, @DesArtAmp, @FecEntArt, @CanArt, @PreArt,
    @ImpBonArt, @ImpGraArt, @ImpDesArt, @ImpPrecArt, @ImpNetGraArt, @ImpIvaArt
)
ON CONFLICT(csid, secuencia) DO UPDATE SET
    codart = excluded.codart,
    codivaart = excluded.codivaart,
    codclabon = excluded.codclabon,
    desartamp = excluded.desartamp,
    fecentart = excluded.fecentart,
    canart = excluded.canart,
    preart = excluded.preart,
    impbonart = excluded.impbonart,
    impgraart = excluded.impgraart,
    impdesart = excluded.impdesart,
    imprecart = excluded.imprecart,
    impnetgraart = excluded.impnetgraart,
    impivaart = excluded.impivaart;";

                    int countCabeceras = 0;
                    int countDetalles = 0;

                    foreach (var pedido in lista)
                    {
                        ct.ThrowIfCancellationRequested();

                        var pCab = new
                        {
                            CodTipCbt = pedido.CodTipCbt,
                            CemCbt = pedido.CemCbt,
                            NroCbt = pedido.NroCbt,
                            Csid = pedido.Csid,
                            FecCbt = pedido.FecCbt,
                            FecEntCbt = pedido.FecEntCbt,
                            CodCli = pedido.CodCli,
                            CodIva = pedido.CodIva,
                            CodSuc = pedido.CodSuc,
                            CodList = pedido.CodList,
                            CodClaBon = pedido.CodClaBon,
                            CodVen = pedido.CodVen,
                            CodTra = pedido.CodTra,
                            CodConVta = pedido.CodConVta,
                            CodMon = pedido.CodMon,
                            ImpCot = pedido.ImpCot,
                            ImpGraCbt = pedido.ImpGraCbt,
                            PorDesCbt = pedido.PorDesCbt,
                            ImpDesCbt = pedido.ImpDesCbt,
                            PorRecCbt = pedido.PorRecCbt,
                            ImpRecCbt = pedido.ImpRecCbt,
                            ImpNetGraCbt = pedido.ImpNetGraCbt,
                            ImpIvaCbt = pedido.ImpIvaCbt,
                            ImpTotCbt = pedido.ImpTotCbt,
                            ObsCbt = pedido.ObsCbt,
                            AutPed = pedido.AutPed,
                            UsrIngreso = pedido.UsrIngreso,
                            FecIngreso = pedido.FecIngreso,
                            UsrAut = pedido.UsrAut,
                            FecAut = pedido.FecAut,
                            NroBocEnt = pedido.NroBocEnt,
                            PedWebInc = pedido.PedWebInc,
                            ObsComunicado = pedido.ObsComunicado,
                            FecPedWebInc = pedido.FecPedWebInc,
                            AutPedSec = pedido.AutPedSec,
                            UsrAutSec = pedido.UsrAutSec,
                            FecAutSec = pedido.FecAutSec
                        };

                        await _conn.ExecuteAsync(upsertCabecera, pCab, transaction: transaction);
                        countCabeceras++;

                        if (pedido.Detalles != null && pedido.Detalles.Any())
                        {
                            foreach (var det in pedido.Detalles)
                            {
                                var pDet = new
                                {
                                    Csid = det.Csid,
                                    Secuencia = det.Secuencia,
                                    CodArt = det.CodArt,
                                    CodIvaArt = det.CodIvaArt,
                                    CodClaBon = det.CodClaBon,
                                    DesArtAmp = det.DesArtAmp,
                                    FecEntArt = det.FecEntArt,
                                    CanArt = det.CanArt,
                                    PreArt = det.PreArt,
                                    ImpBonArt = det.ImpBonArt,
                                    ImpGraArt = det.ImpGraArt,
                                    ImpDesArt = det.ImpDesArt,
                                    ImpPrecArt = det.ImpPrecArt,
                                    ImpNetGraArt = det.ImpNetGraArt,
                                    ImpIvaArt = det.ImpIvaArt
                                };

                                await _conn.ExecuteAsync(upsertDetalle, pDet, transaction: transaction);
                                countDetalles++;
                            }
                        }
                    }

                    transaction.Commit();

                    result.InsertedOrUpdatedArtic = countCabeceras;    // reutilizando propiedades de SyncResult
                    result.InsertedOrUpdatedPreVen = countDetalles;
                    result.Message = $"Sincronización completada. Cabeceras: {countCabeceras}, Detalles: {countDetalles}";
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

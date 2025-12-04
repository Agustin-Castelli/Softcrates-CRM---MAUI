using Core.DTOs;
using Core.DTOs.Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace MobileApp.Services
{
    public class PedidoLocalService : IPedidoService
    {
        private readonly SqliteConnection _conn;

        public PedidoLocalService(SqliteConnection conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        public async Task<PedidoDTO> ObtenerPedidoPorCodigo(string codigo)
        {
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            var cab = await _conn.QueryFirstOrDefaultAsync<PedidoDTO>(
                @"
SELECT
    codtipcbt   AS CodTipCbt,
    cemcbt      AS CemCbt,
    nrocbt      AS NroCbt,
    csid        AS Csid,
    feccbt      AS FecCbt,
    fecentcbt   AS FecEntCbt,
    codcli      AS CodCli,
    codiva      AS CodIva,
    codsuc      AS CodSuc,
    codlist     AS CodList,
    codclabon   AS CodClaBon,
    codven      AS CodVen,
    codtra      AS CodTra,
    codconvta   AS CodConVta,
    codmon      AS CodMon,
    impcot      AS ImpCot,
    impgracbt   AS ImpGraCbt,
    pordescbt   AS PorDesCbt,
    impdescbt   AS ImpDesCbt,
    porreccbt   AS PorRecCbt,
    impreccbt    AS ImpRecCbt,
    impnetgracbt AS ImpNetGraCbt,
    impivacbt   AS ImpIvaCbt,
    imptotcbt   AS ImpTotCbt,
    obscbt      AS ObsCbt,
    autped      AS AutPed,
    usringreso  AS UsrIngreso,
    fecingreso  AS FecIngreso,
    usraut      AS UsrAut,
    fecaut      AS FecAut,
    nrobocent   AS NroBocEnt,
    pedwebinc   AS PedWebInc,
    obscomunicado AS ObsComunicado,
    fecpedwebinc AS FecPedWebInc,
    autpedsec   AS AutPedSec,
    usrautsec   AS UsrAutSec,
    fecautsec   AS FecAutSec,
    confirmado  AS Confirmado
FROM PedWebCab
WHERE csid = @Csid
", new { Csid = codigo });

            if (cab == null)
                return null;

            var detalles = await _conn.QueryAsync<PedidoDetalleDTO>(
                @"
SELECT
    csid        AS Csid,
    secuencia   AS Secuencia,
    codart      AS CodArt,
    codivaart   AS CodIvaArt,
    codclabon   AS CodClaBon,
    desartamp   AS DesArtAmp,
    fecentart   AS FecEntArt,
    canart      AS CanArt,
    preart      AS PreArt,
    impbonart   AS ImpBonArt,
    impgraart   AS ImpGraArt,
    impdesart   AS ImpDesArt,
    imprecart  AS ImpRecArt,
    impnetgraart AS ImpNetGraArt,
    impivaart   AS ImpIvaArt
FROM PedWebArt
WHERE csid = @Csid
ORDER BY secuencia
", new { Csid = codigo });

            cab.Detalles = detalles.ToList();
            return cab;
        }

        public async Task<IEnumerable<PedidoDTO>> ObtenerHistorialPedidos(int codCli, int pageNumber = 1, int pageSize = 50)
        {
            System.Diagnostics.Debug.WriteLine($"[PedidoLocalService] === INICIO ObtenerHistorialPedidos === Cliente={codCli}, Página={pageNumber}, Tamaño={pageSize}");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                // 🔹 Calcular desplazamiento
                int offset = (pageNumber - 1) * pageSize;

                // 🔹 Traer solo los pedidos correspondientes a la página actual
                var cabeceras = await _conn.QueryAsync<PedidoDTO>(
                    @"
                SELECT
                    codtipcbt   AS CodTipCbt,
                    cemcbt      AS CemCbt,
                    nrocbt      AS NroCbt,
                    csid        AS Csid,
                    feccbt      AS FecCbt,
                    fecentcbt   AS FecEntCbt,
                    codcli      AS CodCli,
                    codiva      AS CodIva,
                    codsuc      AS CodSuc,
                    codlist     AS CodList,
                    codclabon   AS CodClaBon,
                    codven      AS CodVen,
                    codtra      AS CodTra,
                    codconvta   AS CodConVta,
                    codmon      AS CodMon,
                    impcot      AS ImpCot,
                    impgracbt   AS ImpGraCbt,
                    pordescbt   AS PorDesCbt,
                    impdescbt   AS ImpDesCbt,
                    porreccbt   AS PorRecCbt,
                    impreccbt   AS ImpRecCbt,
                    impnetgracbt AS ImpNetGraCbt,
                    impivacbt   AS ImpIvaCbt,
                    imptotcbt   AS ImpTotCbt,
                    obscbt      AS ObsCbt,
                    autped      AS AutPed,
                    usringreso  AS UsrIngreso,
                    fecingreso  AS FecIngreso,
                    usraut      AS UsrAut,
                    fecaut      AS FecAut,
                    nrobocent   AS NroBocEnt,
                    pedwebinc   AS PedWebInc,
                    obscomunicado AS ObsComunicado,
                    fecpedwebinc AS FecPedWebInc,
                    autpedsec   AS AutPedSec,
                    usrautsec   AS UsrAutSec,
                    fecautsec   AS FecAutSec,
                    confirmado  AS Confirmado
                FROM PedWebCab
                WHERE codcli = @CodCli
                ORDER BY feccbt DESC
                LIMIT @PageSize OFFSET @Offset;
            ", new { CodCli = codCli, PageSize = pageSize, Offset = offset });

                var lista = cabeceras.ToList();

                System.Diagnostics.Debug.WriteLine($"[PedidoLocalService] ✅ Cabeceras encontradas para CodCli={codCli}: {lista.Count}");

                if (!lista.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[PedidoLocalService] ⚠️ No se encontraron pedidos en esta página.");
                    return lista;
                }

                // 🔹 Traer detalles solo de los pedidos visibles
                var csidList = lista.Select(c => c.Csid).ToList();

                var detalles = await _conn.QueryAsync<PedidoDetalleDTO>(
                    @"
                SELECT
                    csid        AS Csid,
                    secuencia   AS Secuencia,
                    codart      AS CodArt,
                    codivaart   AS CodIvaArt,
                    codclabon   AS CodClaBon,
                    desartamp   AS DesArtAmp,
                    fecentart   AS FecEntArt,
                    canart      AS CanArt,
                    preart      AS PreArt,
                    impbonart   AS ImpBonArt,
                    impgraart   AS ImpGraArt,
                    impdesart   AS ImpDesArt,
                    imprecart   AS ImpRecArt,
                    impnetgraart AS ImpNetGraArt,
                    impivaart   AS ImpIvaArt
                FROM PedWebArt
                WHERE csid IN @CsidList
                ORDER BY csid, secuencia;
            ", new { CsidList = csidList });

                var detallesAgrupados = detalles.GroupBy(d => d.Csid).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var cab in lista)
                {
                    if (detallesAgrupados.TryGetValue(cab.Csid, out var dets))
                        cab.Detalles = dets;
                    else
                        cab.Detalles = new List<PedidoDetalleDTO>();
                }

                System.Diagnostics.Debug.WriteLine($"[PedidoLocalService] 🔹 Página {pageNumber}: {lista.Count} pedidos cargados con sus detalles.");
                return lista;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PedidoLocalService] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();
                System.Diagnostics.Debug.WriteLine("[PedidoLocalService] Conexión cerrada");
            }
        }


        public async Task<IEnumerable<PedidoDTO>> GetAllPedidos()
        {
            // opcional: implementar si lo necesitás
            return Enumerable.Empty<PedidoDTO>();
        }
    }
}



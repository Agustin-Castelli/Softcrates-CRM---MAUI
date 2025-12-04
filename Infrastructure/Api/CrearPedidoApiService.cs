using Core.DTOs;
using Core.DTOs.Core.DTOs;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Infrastructure.Api
{
    // Services/CrearPedidoApiService.cs
    public class CrearPedidoApiService : ICrearPedidoService
    {
        private readonly HttpClient _http;
        private readonly SqliteConnection _conn; // 🔥 AGREGAR

        public CrearPedidoApiService(HttpClient http, SqliteConnection conn) // 🔥 AGREGAR
        {
            _http = http;
            _conn = conn; // 🔥 AGREGAR
        }

        public async Task<bool> CrearPedidoAsync(CrearPedidoDTO pedido)
        {
            // 1. Crear en la API (SQL Server)
            var response = await _http.PostAsJsonAsync("Pedido/CrearPedido", pedido);

            if (!response.IsSuccessStatusCode)
                return false;

            // 2. 🔥 Obtener el pedido creado desde la API (con el Csid generado)
            var pedidoCreado = await response.Content.ReadFromJsonAsync<PedidoDTO>();

            // Agrega esto:
            var jsonRaw = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"JSON RAW: {jsonRaw}");
            System.Diagnostics.Debug.WriteLine($"Csid deserializado: '{pedidoCreado?.Csid ?? "NULL"}'");

            if (pedidoCreado == null)
                return false;

            // 3. 🔥 Guardarlo también en SQLite
            await GuardarEnSQLite(pedidoCreado);

            return true;
        }

        private async Task GuardarEnSQLite(PedidoDTO pedido)
        {
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            using var tx = _conn.BeginTransaction();

            try
            {
                // 🔥 Crear objeto anónimo con los valores correctos
                var cabecera = new
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
                    pedido.UsrIngreso,
                    pedido.FecIngreso,
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
                    Estado = "Sincronizado",
                    OrigenPedido = "Servidor"
                };

                await _conn.ExecuteAsync(@"
            INSERT OR REPLACE INTO PedWebCab (
                codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli,
                codiva, codsuc, codlist, codclabon, codven, codtra, codconvta,
                codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
                impnetgracbt, impivacbt, imptotcbt,
                obscbt, autped, usringreso, fecingreso, usraut, fecaut,
                nrobocent, pedwebinc, obscomunicado, fecpedwebinc, autpedsec, usrautsec, fecautsec, Confirmado,
                Estado, OrigenPedido
            ) VALUES (
                @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli,
                @CodIva, @CodSuc, @CodList, @CodClaBon, @CodVen, @CodTra, @CodConVta,
                @CodMon, @ImpCot, @ImpGraCbt, @PorDesCbt, @ImpDesCbt, @PorRecCbt, @ImpRecCbt,
                @ImpNetGraCbt, @ImpIvaCbt, @ImpTotCbt,
                @ObsCbt, @AutPed, @UsrIngreso, @FecIngreso, @UsrAut, @FecAut,
                @NroBocEnt, @PedWebInc, @ObsComunicado, @FecPedWebInc, @AutPedSec, @UsrAutSec, @FecAutSec, @Confirmado,
                @Estado, @OrigenPedido
            );
        ", cabecera, transaction: tx);

                System.Diagnostics.Debug.WriteLine($"[CrearPedidoApiService] ✅ Cabecera {pedido.Csid} guardada");

                // Insertar detalles
                if (pedido.Detalles != null && pedido.Detalles.Any())
                {
                    foreach (var det in pedido.Detalles)
                    {
                        // 🔥 Objeto anónimo con mapeo correcto
                        var detalle = new
                        {
                            det.Csid,
                            det.Secuencia,
                            det.CodArt,
                            DesArtAmp = det.DesArtAmp,
                            CanArt = det.CanArt,
                            PreArt = det.PreArt,
                            CodIvaArt = det.CodIvaArt,
                            CodClaBon = det.CodClaBon,
                            FecEntArt = det.FecEntArt,
                            ImpBonArt = det.ImpBonArt,
                            ImpGraArt = det.ImpGraArt,
                            ImpDesArt = det.ImpDesArt,
                            ImpRecArt = det.ImpRecArt,
                            ImpNetGraArt = det.ImpNetGraArt,
                            ImpIvaArt = det.ImpIvaArt
                        };

                        await _conn.ExecuteAsync(@"
                    INSERT OR REPLACE INTO PedWebArt (
                        csid, secuencia, codart, desartamp, canart, preart,
                        codivaart, codclabon, fecentart, impbonart, impgraart,
                        impdesart, imprecart, impnetgraart, impivaart
                    ) VALUES (
                        @Csid, @Secuencia, @CodArt, @DesArtAmp, @CanArt, @PreArt,
                        @CodIvaArt, @CodClaBon, @FecEntArt, @ImpBonArt, @ImpGraArt,
                        @ImpDesArt, @ImpRecArt, @ImpNetGraArt, @ImpIvaArt
                    );
                ", detalle, transaction: tx);
                    }

                    System.Diagnostics.Debug.WriteLine($"[CrearPedidoApiService] ✅ {pedido.Detalles.Count} detalles guardados");
                }

                tx.Commit();
                System.Diagnostics.Debug.WriteLine($"[CrearPedidoApiService] ✅ Pedido {pedido.Csid} guardado completamente en SQLite");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                System.Diagnostics.Debug.WriteLine($"[CrearPedidoApiService] ❌ Error guardando en SQLite: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<CrearPedidoDTO>> CrearPedidosMasivoAsync(IEnumerable<CrearPedidoDTO> pedidos)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Pedido/SyncPedidos", pedidos);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<IEnumerable<CrearPedidoDTO>>();
                return result ?? Enumerable.Empty<CrearPedidoDTO>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al sincronizar pedidos pendientes: {ex}");
                throw;
            }
        }
    }

}

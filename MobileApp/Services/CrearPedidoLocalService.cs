using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using MobileApp.Services;
using System.Data;

public class CrearPedidoLocalService : ICrearPedidoService
{
    private readonly SqliteConnection _conn;
    private readonly BonificacionLocalService _bonificacionService;

    public CrearPedidoLocalService(
        SqliteConnection conn,
        BonificacionLocalService bonificacionService)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
        _bonificacionService = bonificacionService ?? throw new ArgumentNullException(nameof(bonificacionService));
    }

    public async Task<bool> CrearPedidoAsync(CrearPedidoDTO dto)
    {
        try
        {
            // 🔥 1. CALCULAR BONIFICACIONES ANTES DE LA TRANSACCIÓN
            var bonificaciones = new Dictionary<string, (decimal Porcentaje, short CodClaBon)>();

            foreach (var det in dto.Detalles)
            {
                var porcentaje = await _bonificacionService.ObtenerPorcentajeBonificacionAsync(
                    dto.CodCli,
                    det.CodArt,
                    det.Cantidad,
                    det.PrecioUnitario
                );

                var bonifInfo = await _bonificacionService.ObtenerBonificacionBaseAsync(dto.CodCli, det.CodArt);
                var codClaBon = bonifInfo?.CodClaBon ?? (short)0;

                bonificaciones[det.CodArt] = (porcentaje, codClaBon);

                System.Diagnostics.Debug.WriteLine(
                    $"[CrearPedidoLocalService] Bonificación precalculada - Art {det.CodArt}: {porcentaje}%, CodClaBon: {codClaBon}"
                );
            }

            // 🔥 2. AHORA SÍ, ABRIR CONEXIÓN Y TRANSACCIÓN
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            using var tx = _conn.BeginTransaction();

            // 3. Obtener el último NroCbt
            var ultimoNro = await _conn.ExecuteScalarAsync<int?>(
                @"SELECT MAX(nrocbt) 
                  FROM PedWebCab 
                  WHERE codtipcbt = @CodTipCbt AND cemcbt = @CemCbt",
                new { dto.CodTipCbt, dto.CemCbt },
                tx
            ) ?? 0;

            var nuevoNro = ultimoNro + 1;

            // 4. Crear cabecera
            var fecAhora = DateTime.Now;
            var csid = $"{dto.CodTipCbt}{dto.CemCbt}{nuevoNro}{dto.CodCli}";

            await _conn.ExecuteAsync(@"
                INSERT INTO PedWebCab (
                    codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli,
                    codiva, codsuc, codlist, codclabon, codven, codtra, codconvta,
                    codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
                    impnetgracbt, impivacbt, imptotcbt,
                    obscbt, autped, usringreso, fecingreso, usraut, fecaut,
                    nrobocent, pedwebinc, obscomunicado, fecpedwebinc, autpedsec, usrautsec, fecautsec, Confirmado, Estado, OrigenPedido
                ) VALUES (
                    @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli,
                    1, '1', 1, 1, '01', '01', 'CC',
                    1, 1, 0, 0, 0, 0, 0,
                    0, 0, 0,
                    '', 'N', 'Default', @FecIngreso, '', @FecAut,
                    @NroBocEnt, '', '', @FecPedWebInc, '', '', @FecAutSec, @Confirmado, 'Pendiente', 'Local'
                );
            ",
            new
            {
                dto.CodTipCbt,
                dto.CemCbt,
                NroCbt = nuevoNro,
                Csid = csid,
                FecCbt = fecAhora,
                FecEntCbt = fecAhora.AddDays(5),
                dto.CodCli,
                FecIngreso = fecAhora,
                FecAut = fecAhora,
                NroBocEnt = dto.NroBocEnt,
                FecPedWebInc = fecAhora,
                FecAutSec = fecAhora,
                Confirmado = dto.Confirmado,
            }, tx);

            // 5. Insertar detalles CON BONIFICACIÓN (YA PRECALCULADA) 🔥
            short secuencia = 1;
            decimal totalPedido = 0;
            decimal totalDescuentos = 0;

            foreach (var det in dto.Detalles)
            {
                // 🔥 USAR BONIFICACIÓN PRECALCULADA
                var (porcentajeBonificacion, codClaBon) = bonificaciones[det.CodArt];

                // 🔥 CALCULAR SUBTOTALES Y DESCUENTO
                var subtotalSinDescuento = det.PrecioUnitario * det.Cantidad;
                var importeDescuento = subtotalSinDescuento * (porcentajeBonificacion / 100);
                var subtotalConDescuento = subtotalSinDescuento - importeDescuento;

                System.Diagnostics.Debug.WriteLine(
                    $"[CrearPedidoLocalService] Art {det.CodArt}: " +
                    $"Cant {det.Cantidad}, " +
                    $"PrecioUnit {det.PrecioUnitario}, " +
                    $"Subtotal {subtotalSinDescuento}, " +
                    $"Bonif {porcentajeBonificacion}%, " +
                    $"Descuento {importeDescuento}, " +
                    $"Final {subtotalConDescuento}"
                );

                await _conn.ExecuteAsync(@"
                    INSERT INTO PedWebArt (
                        csid, secuencia, codart, desartamp, canart, preart,
                        codivaart, codclabon, fecentart, impbonart, impgraart,
                        impdesart, imprecart, impnetgraart, impivaart
                    ) VALUES (
                        @Csid, @Secuencia, @CodArt, @DesArtAmp, @CanArt, @PreArt,
                        78, @CodClaBon, @FecEntArt, @ImpBonArt, @ImpGraArt,
                        0, 0, @ImpNetGraArt, 0
                    );
                ",
                new
                {
                    Csid = csid,
                    Secuencia = secuencia,
                    det.CodArt,
                    DesArtAmp = det.DesArt,
                    CanArt = det.Cantidad,
                    PreArt = det.PrecioUnitario,
                    CodClaBon = codClaBon,
                    FecEntArt = fecAhora,
                    ImpBonArt = importeDescuento,
                    ImpGraArt = subtotalConDescuento,
                    ImpNetGraArt = subtotalConDescuento
                }, tx);

                totalPedido += subtotalConDescuento;
                totalDescuentos += importeDescuento;
                secuencia++;
            }

            // 6. Actualizar totales del pedido 🔥
            await _conn.ExecuteAsync(@"
                UPDATE PedWebCab
                SET imptotcbt = @ImpTotCbt,
                    impdescbt = @ImpDesCbt,
                    impgracbt = @ImpGraCbt
                WHERE csid = @Csid;
            ",
            new
            {
                ImpTotCbt = totalPedido,
                ImpDesCbt = totalDescuentos,
                ImpGraCbt = totalPedido + totalDescuentos,
                Csid = csid
            }, tx);

            System.Diagnostics.Debug.WriteLine(
                $"[CrearPedidoLocalService] ✅ Pedido {csid} creado. " +
                $"Total con descuento: {totalPedido}, " +
                $"Descuentos: {totalDescuentos}, " +
                $"Total sin descuento: {totalPedido + totalDescuentos}"
            );

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrearPedidoLocalService] ❌ ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CrearPedidoLocalService] StackTrace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            if (_conn.State == ConnectionState.Open)
                await _conn.CloseAsync();
        }
    }
}


//public class CrearPedidoLocalService : ICrearPedidoService
//{
//    private readonly SqliteConnection _conn;

//    public CrearPedidoLocalService(SqliteConnection conn)
//    {
//        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
//    }

//    public async Task<bool> CrearPedidoAsync(CrearPedidoDTO dto)
//    {
//        if (_conn.State != ConnectionState.Open)
//            await _conn.OpenAsync();

//        using var tx = _conn.BeginTransaction();

//        try
//        {
//            // 1. Obtener el último NroCbt
//            var ultimoNro = await _conn.ExecuteScalarAsync<int?>(
//                @"SELECT MAX(nrocbt) 
//                  FROM PedWebCab 
//                  WHERE codtipcbt = @CodTipCbt AND cemcbt = @CemCbt",
//                new { dto.CodTipCbt, dto.CemCbt },
//                tx
//            ) ?? 0;

//            var nuevoNro = ultimoNro + 1;

//            // 2. Crear cabecera
//            var fecAhora = DateTime.Now;

//            var csid = $"{dto.CodTipCbt}{dto.CemCbt}{nuevoNro}{dto.CodCli}";

//            await _conn.ExecuteAsync(@"
//                INSERT INTO PedWebCab (
//                    codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli,
//                    codiva, codsuc, codlist, codclabon, codven, codtra, codconvta,
//                    codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
//                    impnetgracbt, impivacbt, imptotcbt,
//                    obscbt, autped, usringreso, fecingreso, usraut, fecaut,
//                    nrobocent, pedwebinc, obscomunicado, fecpedwebinc, autpedsec, usrautsec, fecautsec, Confirmado, Estado, OrigenPedido
//                ) VALUES (
//                    @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli,
//                    1, '1', 1, 1, '01', '01', 'CC',
//                    1, 1, 0, 0, 0, 0, 0,
//                    0, 0, 0,
//                    '', 'N', 'Default', @FecIngreso, '', @FecAut,
//                    @NroBocEnt, '', '', @FecPedWebInc, '', '', @FecAutSec, @Confirmado, 'Pendiente', 'Local'
//                );
//            ",
//            new
//            {
//                dto.CodTipCbt,
//                dto.CemCbt,
//                NroCbt = nuevoNro,
//                Csid = csid,
//                FecCbt = fecAhora,
//                FecEntCbt = fecAhora.AddDays(5),
//                dto.CodCli,
//                FecIngreso = fecAhora,
//                FecAut = fecAhora,
//                NroBocEnt = dto.NroBocEnt,
//                FecPedWebInc = fecAhora,
//                FecAutSec = fecAhora,
//                Confirmado = dto.Confirmado,
//            }, tx);

//            // 3. Insertar detalles
//            short secuencia = 1;
//            decimal totalPedido = 0;

//            foreach (var det in dto.Detalles)
//            {
//                var subtotal = det.PrecioUnitario * det.Cantidad;

//                await _conn.ExecuteAsync(@"
//                    INSERT INTO PedWebArt (
//                        csid, secuencia, codart, desartamp, canart, preart,
//                        codivaart, codclabon, fecentart, impbonart, impgraart,
//                        impdesart, imprecart, impnetgraart, impivaart
//                    ) VALUES (
//                        @Csid, @Secuencia, @CodArt, @DesArtAmp, @CanArt, @PreArt,
//                        78, 0, @FecEntArt, 0, @ImpGraArt,
//                        0, 0, 0, 0
//                    );
//                ",
//                new
//                {
//                    Csid = csid,
//                    Secuencia = secuencia,
//                    det.CodArt,
//                    DesArtAmp = det.DesArt,
//                    CanArt = det.Cantidad,
//                    PreArt = det.PrecioUnitario,
//                    FecEntArt = fecAhora,
//                    ImpGraArt = subtotal
//                }, tx);

//                totalPedido += subtotal;
//                secuencia++;
//            }

//            // 4. Actualizar total del pedido
//            await _conn.ExecuteAsync(@"
//                UPDATE PedWebCab
//                SET imptotcbt = @ImpTotCbt
//                WHERE csid = @Csid;
//            ",
//            new { ImpTotCbt = totalPedido, Csid = csid }, tx);

//            tx.Commit();
//            return true;
//        }
//        catch (Exception ex)
//        {
//            tx.Rollback();
//            System.Diagnostics.Debug.WriteLine($"[CrearPedidoLocalService] ERROR: {ex.Message}");
//            return false;
//        }
//    }
//}


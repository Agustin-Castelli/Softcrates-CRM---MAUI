using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

public class CrearPedidoLocalService : ICrearPedidoService
{
    private readonly SqliteConnection _conn;

    public CrearPedidoLocalService(SqliteConnection conn)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
    }

    public async Task<bool> CrearPedidoAsync(CrearPedidoDTO dto)
    {
        if (_conn.State != ConnectionState.Open)
            await _conn.OpenAsync();

        using var tx = _conn.BeginTransaction();

        try
        {
            // 1. Obtener el último NroCbt
            var ultimoNro = await _conn.ExecuteScalarAsync<int?>(
                @"SELECT MAX(nrocbt) 
                  FROM PedWebCab 
                  WHERE codtipcbt = @CodTipCbt AND cemcbt = @CemCbt",
                new { dto.CodTipCbt, dto.CemCbt },
                tx
            ) ?? 0;

            var nuevoNro = ultimoNro + 1;

            // 2. Crear cabecera
            var fecAhora = DateTime.Now;

            var csid = $"{dto.CodTipCbt}{dto.CemCbt}{nuevoNro}{dto.CodCli}";

            await _conn.ExecuteAsync(@"
                INSERT INTO PedWebCab (
                    codtipcbt, cemcbt, nrocbt, csid, feccbt, fecentcbt, codcli,
                    codiva, codsuc, codlist, codclabon, codven, codtra, codconvta,
                    codmon, impcot, impgracbt, pordescbt, impdescbt, porreccbt, impreccbt,
                    impnetgracbt, impivacbt, imptotcbt,
                    obscbt, autped, usringreso, fecingreso, usraut, fecaut,
                    nrobocent, pedwebinc, obscomunicado, fecpedwebinc, autpedsec, usrautsec, fecautsec
                ) VALUES (
                    @CodTipCbt, @CemCbt, @NroCbt, @Csid, @FecCbt, @FecEntCbt, @CodCli,
                    1, '1', 1, 1, '01', '01', 'CC',
                    1, 1, 0, 0, 0, 0, 0,
                    0, 0, 0,
                    '', 'N', 'Default', @FecIngreso, '', @FecAut,
                    0, '', '', @FecPedWebInc, '', '', @FecAutSec
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
                FecPedWebInc = fecAhora,
                FecAutSec = fecAhora
            }, tx);

            // 3. Insertar detalles
            short secuencia = 1;
            decimal totalPedido = 0;

            foreach (var det in dto.Detalles)
            {
                var subtotal = det.PrecioUnitario * det.Cantidad;

                await _conn.ExecuteAsync(@"
                    INSERT INTO PedWebArt (
                        csid, secuencia, codart, desartamp, canart, preart,
                        codivaart, codclabon, fecentart, impbonart, impgraart,
                        impdesart, imprecart, impnetgraart, impivaart
                    ) VALUES (
                        @Csid, @Secuencia, @CodArt, @DesArtAmp, @CanArt, @PreArt,
                        78, 0, @FecEntArt, 0, @ImpGraArt,
                        0, 0, 0, 0
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
                    FecEntArt = fecAhora,
                    ImpGraArt = subtotal
                }, tx);

                totalPedido += subtotal;
                secuencia++;
            }

            // 4. Actualizar total del pedido
            await _conn.ExecuteAsync(@"
                UPDATE PedWebCab
                SET imptotcbt = @ImpTotCbt
                WHERE csid = @Csid;
            ",
            new { ImpTotCbt = totalPedido, Csid = csid }, tx);

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            System.Diagnostics.Debug.WriteLine($"[CrearPedidoLocalService] ERROR: {ex.Message}");
            return false;
        }
    }
}


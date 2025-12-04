using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class BonificacionLocalService : IBonificacionService
    {
        private readonly SqliteConnection _conn;

        public BonificacionLocalService(SqliteConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<ArticuloConBonificacionDTO>> ObtenerArticulosConBonificacionAsync(int codCli)
        {
            System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] === INICIO OBTENER ARTÍCULOS CON BONIFICACIÓN CLIENTE {codCli} ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                // 🔥 TRIM en la query de artículos
                const string sqlArticulos = @"
            SELECT 
                TRIM(a.CodArt) AS CodArt,
                a.DesArt AS DesArt,
                p.Precio AS Precio
            FROM Artic a
            LEFT JOIN PreVen p ON TRIM(a.CodArt) = TRIM(p.CodArt)
            ORDER BY a.DesArt;";

                var articulos = (await _conn.QueryAsync<ArticuloConBonificacionDTO>(sqlArticulos)).ToList();

                // 🔥 TRIM en la query de bonificaciones
                const string sqlBonificaciones = @"
            SELECT 
                TRIM(bac.CodArt) AS CodArt,
                bac.CodClaBon,
                bcd.PorBonImp
            FROM BonArtCli bac
            INNER JOIN BonClaDet bcd ON bac.CodClaBon = bcd.CodClaBon
            WHERE bac.CodCli = @CodCli 
              AND bac.inactivo = 0
              AND bcd.Secuencia = 1
            ORDER BY bac.CodArt;";

                var bonificaciones = await _conn.QueryAsync<(string CodArt, short CodClaBon, decimal PorBonImp)>(
                    sqlBonificaciones,
                    new { CodCli = codCli }
                );

                // Ya no es necesario .Trim() en el código porque la query lo hace
                var dictBonificaciones = bonificaciones.ToDictionary(
                    b => b.CodArt,
                    b => (b.CodClaBon, b.PorBonImp)
                );

                foreach (var art in articulos)
                {
                    if (dictBonificaciones.TryGetValue(art.CodArt, out var bonif))
                    {
                        art.CodClaBon = bonif.CodClaBon;
                        art.PorcentajeBonificacion = bonif.PorBonImp;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ✅ Artículos: {articulos.Count}, con bonif: {dictBonificaciones.Count}");
                return articulos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();
            }
        }

        public async Task<decimal> ObtenerPorcentajeBonificacionAsync(int codCli, string codArt, decimal cantidad, decimal precioUnitario)
        {
            System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] === CALCULAR BONIFICACIÓN Cliente {codCli}, Art {codArt} ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                // 🔥 TRIM en la comparación
                const string sqlBonArtCli = @"
            SELECT CodClaBon 
            FROM BonArtCli 
            WHERE CodCli = @CodCli 
              AND TRIM(CodArt) = TRIM(@CodArt)
              AND inactivo = 0;";

                var codClaBon = await _conn.QueryFirstOrDefaultAsync<short?>(
                    sqlBonArtCli,
                    new { CodCli = codCli, CodArt = codArt }
                );

                if (!codClaBon.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] No hay bonificación para este artículo");
                    return 0;
                }

                const string sqlEscalones = @"
            SELECT 
                CodClaBon,
                Secuencia,
                TipEsc,
                ValEscDes,
                ValEscHas,
                PorBonImp
            FROM BonClaDet
            WHERE CodClaBon = @CodClaBon
            ORDER BY Secuencia;";

                var escalones = (await _conn.QueryAsync<BonClaDetDTO>(
                    sqlEscalones,
                    new { CodClaBon = codClaBon.Value }
                )).ToList();

                if (!escalones.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] No hay escalones para CodClaBon {codClaBon}");
                    return 0;
                }

                decimal importeTotal = cantidad * precioUnitario;
                BonClaDetDTO? escalonAplicable = null;

                foreach (var escalon in escalones.OrderByDescending(e => e.ValEscDes))
                {
                    if (escalon.ValEscDes <= importeTotal &&
                        (escalon.ValEscHas == null || importeTotal <= escalon.ValEscHas))
                    {
                        escalonAplicable = escalon;
                        break;
                    }
                }

                if (escalonAplicable == null)
                {
                    escalonAplicable = escalones.OrderBy(e => e.Secuencia).First();
                }

                var porcentaje = escalonAplicable?.PorBonImp ?? 0;
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ✅ Bonificación: {porcentaje}% (Importe: {importeTotal})");

                return porcentaje;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ Error: {ex.Message}");
                return 0;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();
            }
        }

        // 🔥 MÉTODO CORREGIDO CON MÁS LOGS
        public async Task<BonificacionDTO?> ObtenerBonificacionBaseAsync(int codCli, string codArt)
        {
            System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] === OBTENER BONIFICACIÓN BASE Cliente {codCli}, Art {codArt} ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] Abriendo conexión...");
                    await _conn.OpenAsync();
                }

                // 🔥 PRIMERO: Verificar si existe el artículo en BonArtCli
                const string sqlVerificar = @"
                    SELECT COUNT(*) 
                    FROM BonArtCli 
                    WHERE CodCli = @CodCli 
                      AND TRIM(CodArt) = TRIM(@CodArt);";

                var existe = await _conn.ExecuteScalarAsync<int>(sqlVerificar, new { CodCli = codCli, CodArt = codArt });
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] Registros en BonArtCli: {existe}");

                if (existe == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ No existe en BonArtCli");
                    return null;
                }

                // 🔥 SEGUNDO: Ver si está inactivo
                const string sqlInactivo = @"
                    SELECT inactivo 
                    FROM BonArtCli 
                    WHERE CodCli = @CodCli 
                      AND TRIM(CodArt) = TRIM(@CodArt);";

                var inactivo = await _conn.ExecuteScalarAsync<int>(sqlInactivo, new { CodCli = codCli, CodArt = codArt });
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] Inactivo: {inactivo}");

                if (inactivo != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ Bonificación inactiva");
                    return null;
                }

                // 🔥 TERCERO: Obtener CodClaBon
                const string sqlCodClaBon = @"
                    SELECT CodClaBon 
                    FROM BonArtCli 
                    WHERE CodCli = @CodCli 
                      AND TRIM(CodArt) = TRIM(@CodArt) 
                      AND inactivo = 0;";

                var codClaBon = await _conn.ExecuteScalarAsync<short?>(sqlCodClaBon, new { CodCli = codCli, CodArt = codArt });
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] CodClaBon: {codClaBon}");

                if (!codClaBon.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ No se encontró CodClaBon");
                    return null;
                }

                // 🔥 CUARTO: Verificar si existe en BonClaDet
                const string sqlVerificarDetalle = @"
                    SELECT COUNT(*) 
                    FROM BonClaDet 
                    WHERE CodClaBon = @CodClaBon;";

                var existeDetalle = await _conn.ExecuteScalarAsync<int>(sqlVerificarDetalle, new { CodClaBon = codClaBon.Value });
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] Registros en BonClaDet: {existeDetalle}");

                if (existeDetalle == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ No existe en BonClaDet");
                    return null;
                }

                // 🔥 QUINTO: Obtener el detalle
                const string sql = @"
                    SELECT 
                        bac.CodClaBon,
                        bcd.PorBonImp AS PorcentajeBonificacion,
                        bcd.TipEsc AS TipoEscala
                    FROM BonArtCli bac
                    INNER JOIN BonClaDet bcd ON bac.CodClaBon = bcd.CodClaBon
                    WHERE bac.CodCli = @CodCli 
                      AND TRIM(bac.CodArt) = TRIM(@CodArt) 
                      AND bac.inactivo = 0
                      AND bcd.Secuencia = 1;";

                var bonificacion = await _conn.QueryFirstOrDefaultAsync<BonificacionDTO>(
                    sql,
                    new { CodCli = codCli, CodArt = codArt }
                );

                if (bonificacion != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ✅ Bonificación base: {bonificacion.PorcentajeBonificacion}%");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ Query final no retornó resultados");
                }

                return bonificacion;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ EXCEPCIÓN: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BONIFICACION LOCAL] ❌ StackTrace: {ex.StackTrace}");
                return null;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();

                System.Diagnostics.Debug.WriteLine("[BONIFICACION LOCAL] Conexión cerrada");
            }
        }
    }
}

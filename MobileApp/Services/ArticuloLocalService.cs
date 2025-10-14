using System.Data;
using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MobileApp.Services
{
    public class ArticuloLocalService : IArticuloService
    {
        private readonly SqliteConnection _conn;

        public ArticuloLocalService(SqliteConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<ArticuloDTO>> GetAll()
        {
            // === INICIO OBTENER TODOS ===
            System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] === INICIO OBTENER TODOS ===");

            try
            {
                // Abrir la conexión si no está abierta
                if (_conn.State != ConnectionState.Open)
                {
                    await _conn.OpenAsync();
                }

                // Traer artículos
                const string sqlArticulos = @"
            SELECT codart AS CodArt,
                   desart AS DesArt
            FROM Artic
            ORDER BY desart;";

                var articulos = (await _conn.QueryAsync<ArticuloDTO>(sqlArticulos)).ToList();

                // Traer precios en batch para no pegarle uno a uno
                const string sqlPrecios = @"
            SELECT codart AS CodArt,
                   precio AS Precio
            FROM PreVen;";

                var precios = await _conn.QueryAsync<(string CodArt, decimal Precio)>(sqlPrecios);
                var preciosDict = precios.ToDictionary(p => p.CodArt, p => p.Precio);

                // Asignar precios a los artículos
                foreach (var art in articulos)
                {
                    art.Precio = preciosDict.ContainsKey(art.CodArt) ? preciosDict[art.CodArt] : 0;
                }

                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ✅ Total artículos encontrados: {articulos.Count}");

                return articulos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                // Cerrar la conexión si está abierta
                if (_conn.State == ConnectionState.Open)
                {
                    await _conn.CloseAsync();
                }
                System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] Conexión cerrada");
            }
        }

        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulos(int pageNumber = 1, int pageSize = 50)
        {
            System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] === INICIO OBTENER PAGINA ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                int offset = (pageNumber - 1) * pageSize;

                // Traer artículos paginados
                const string sqlArticulos = @"
            SELECT codart AS CodArt,
                   desart AS DesArt
            FROM Artic
            ORDER BY desart
            LIMIT @PageSize OFFSET @Offset;";

                var articulos = (await _conn.QueryAsync<ArticuloDTO>(sqlArticulos, new { PageSize = pageSize, Offset = offset })).ToList();

                if (articulos.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] ⚠️ No se encontraron artículos en esta página.");
                    return articulos;
                }

                // Obtener los códigos de artículo de esta página
                var codigos = articulos.Select(a => a.CodArt).ToList();

                // Construir el filtro dinámico para precios
                string codigosParam = string.Join(",", codigos.Select(c => $"'{c}'"));

                string sqlPrecios = $@"
            SELECT codart AS CodArt,
                   precio  AS Precio
            FROM PreVen
            WHERE codart IN ({codigosParam});";

                var precios = await _conn.QueryAsync<(string CodArt, decimal Precio)>(sqlPrecios);
                var preciosDict = precios.ToDictionary(p => p.CodArt, p => p.Precio);

                foreach (var art in articulos)
                {
                    art.Precio = preciosDict.ContainsKey(art.CodArt) ? preciosDict[art.CodArt] : 0;
                }

                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ✅ Total artículos página {pageNumber}: {articulos.Count}");
                return articulos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();

                System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] Conexión cerrada");
            }
        }



        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorNombre(string desArt)
        {
            System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] === BUSCAR POR NOMBRE '{desArt}' ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                const string sqlArticulos = @"
                    SELECT codart AS CodArt,
                           desart AS DesArt
                    FROM Artic
                    WHERE UPPER(TRIM(desart)) LIKE UPPER(TRIM('%' || @desArt || '%'))
                    ORDER BY desart;";

                var articulos = (await _conn.QueryAsync<ArticuloDTO>(sqlArticulos, new { desArt })).ToList();

                const string sqlPrecios = @"
                    SELECT codart AS CodArt,
                           precio  AS Precio
                    FROM PreVen
                    WHERE codart IN @codigos;";

                var precios = await _conn.QueryAsync<(string CodArt, decimal Precio)>(
                    sqlPrecios,
                    new { codigos = articulos.Select(a => a.CodArt).ToArray() }
                );

                var preciosDict = precios.ToDictionary(p => p.CodArt, p => p.Precio);

                foreach (var art in articulos)
                {
                    art.Precio = preciosDict.ContainsKey(art.CodArt) ? preciosDict[art.CodArt] : 0;
                }

                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ✅ Resultados encontrados: {articulos.Count}");
                return articulos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();

                System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] Conexión cerrada");
            }
        }

        public async Task<ArticuloDTO> ObtenerArticuloPorCodigo(string codArt)
        {
            System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] === BUSCAR POR CÓDIGO '{codArt}' ===");

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                const string sqlArticulo = @"
                    SELECT codart AS CodArt,
                           desart AS DesArt
                    FROM Artic
                    WHERE codart = @codArt;";

                var articulo = await _conn.QueryFirstOrDefaultAsync<ArticuloDTO>(sqlArticulo, new { codArt });

                if (articulo != null)
                {
                    const string sqlPrecio = @"
                        SELECT precio
                        FROM PreVen
                        WHERE codart = @codArt
                        LIMIT 1;";

                    articulo.Precio = await _conn.ExecuteScalarAsync<decimal?>(sqlPrecio, new { codArt }) ?? 0;
                }

                return articulo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ARTICULO LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    await _conn.CloseAsync();

                System.Diagnostics.Debug.WriteLine("[ARTICULO LOCAL] Conexión cerrada");
            }
        }
    }
}

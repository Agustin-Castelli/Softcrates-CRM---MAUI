using Core.DTOs;
using Core.Interfaces;
using Core.Models.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace MobileApp.Services
{
    public class ClientLocalService : IClientService
    {
        private readonly SqliteConnection _conn;

        public ClientLocalService(SqliteConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
        {
            System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] === INICIO BÚSQUEDA ===");
            System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] Término de búsqueda: '{term}'");

            try
            {
                // Abrir conexión UNA sola vez al inicio
                if (_conn.State != ConnectionState.Open)
                {
                    await _conn.OpenAsync();
                }

                // Query principal - CONSISTENTE con nombre de tabla "Clien"
                const string sql = @"
            SELECT 
                codcli          AS CodCli,
                nomcli          AS NomCli,
                saldodeucc      AS SaldoDeuCc,
                saldovencc     AS SaldoVencCc,
                limcrecli       AS LimiteCredito,
                limcrecliuso    AS LimiteCreditoUso
            FROM Clien 
            WHERE 
                UPPER(TRIM(COALESCE(nomcli, ''))) LIKE UPPER(TRIM('%' || @term || '%'))
                OR UPPER(TRIM(COALESCE(codcli, ''))) LIKE UPPER(TRIM('%' || @term || '%'))
            ORDER BY nomcli
            LIMIT 50;";

                System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] Ejecutando query con tabla 'Clien'");
                System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] Parámetro @term: '{term}'");

                var clients = await _conn.QueryAsync<ClientData>(sql, new { term });
                var clientList = clients.ToList();

                System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] ✅ Resultados encontrados: {clientList.Count}");

                if (clientList.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("[CLIENT SEARCH LOCAL] === PRIMEROS RESULTADOS ===");
                    foreach (var client in clientList.Take(3))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] - {client.CodCli}: {client.NomCli}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CLIENT SEARCH LOCAL] ⚠️ No se encontraron clientes");

                    // Debug: Verificar si hay datos en la tabla
                    var totalClients = await _conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM Clien");
                    System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] Total clientes en tabla: {totalClients}");

                    if (totalClients > 0)
                    {
                        // Mostrar algunos ejemplos
                        var sampleClients = await _conn.QueryAsync<ClientData>(@"
                    SELECT 
                        codcli AS CodCli,
                        nomcli AS NomCli,
                        saldodeucc AS SaldoDeuCc,
                        saldovencc AS SaldoVencCc,
                        limcrecli AS LimiteCredito,
                        limcrecliuso AS LimiteCreditoUso
                    FROM Clien 
                    LIMIT 3");

                        System.Diagnostics.Debug.WriteLine("[CLIENT SEARCH LOCAL] === EJEMPLOS DE CLIENTES ===");
                        foreach (var client in sampleClients)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] - '{client.CodCli}': '{client.NomCli}'");
                        }
                    }
                }

                return clientList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] ❌ Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CLIENT SEARCH LOCAL] StackTrace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                // Solo cerrar si la conexión fue abierta por este método
                if (_conn.State == ConnectionState.Open)
                {
                    await _conn.CloseAsync();
                }
                System.Diagnostics.Debug.WriteLine("[CLIENT SEARCH LOCAL] Conexión cerrada");
            }
        }

        public async Task<ClientData> GetClientePorIdAsync(int id)
        {
            const string sql = @"
        SELECT 
            codcli          AS CodCli,
            nomcli          AS NomCli,
            saldodeucc      AS SaldoDeuCc,
            saldovencc     AS SaldoVencCc,
            limcrecli   AS LimiteCredito,
            limcrecliuso AS LimiteCreditoUso
        FROM Clien 
        WHERE codcli = @id
        LIMIT 1;";

            await _conn.OpenAsync();
            try
            {
                var client = await _conn.QueryFirstOrDefaultAsync<ClientData>(sql, new { id });
                return client;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<ClientResumenDto?> GetResumenCliente(int codCli)
        {
            const string clientSql = @"
        SELECT 
            codcli          AS CodCli,
            nomcli          AS NomCli,
            saldodeucc      AS SaldoDeuCc,
            saldovencc     AS SaldoVencCc,
            limcrecli   AS LimiteCredito,
            limcrecliuso AS LimiteCreditoUso
        FROM Clien
        WHERE codcli = @codCli
        LIMIT 1;";

            const string facturasSql = @"
        SELECT 
            (codtipcbtdeu || cemcbtdeu || nrocbtdeu) AS NumeroFactura,
            feccbtdeu       AS Fecha,
            imporideu       AS Importe,
            estadocc        AS Estado
        FROM deudeu 
        WHERE codcli = @codCli
        ORDER BY feccbtdeu DESC;";

            await _conn.OpenAsync();
            try
            {
                // Buscar cliente
                var cliente = await _conn.QueryFirstOrDefaultAsync<Client>(clientSql, new { codCli });
                if (cliente == null) return null;

                // Buscar facturas del cliente
                var facturas = await _conn.QueryAsync<FacturaDto>(facturasSql, new { codCli });

                var dto = new ClientResumenDto
                {
                    CodCli = cliente.CodCli,
                    Nombre = cliente.NomCli,
                    SaldoActual = cliente.SaldoDeuCc,
                    LimiteCredito = cliente.LimiteCredito,
                    SaldoVencido = cliente.SaldoVencCc,
                    PorcentajeCreditoUsado = cliente.LimiteCreditoUso,
                    Facturas = facturas.ToList()
                };

                return dto;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }
    }
}


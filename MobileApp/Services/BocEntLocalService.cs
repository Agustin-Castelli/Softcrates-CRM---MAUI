// Infrastructure/Local/BocEntLocalService.cs
using Core.DTOs;
using Core.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Infrastructure.Local
{
    public class BocEntLocalService : IBocEntService
    {
        private readonly SqliteConnection _conn;

        public BocEntLocalService(SqliteConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetAll()
        {
            System.Diagnostics.Debug.WriteLine("[BOCENT LOCAL] === INICIO OBTENER TODAS ===");

            try
            {
                // Abrir la conexión si no está abierta
                if (_conn.State != ConnectionState.Open)
                {
                    await _conn.OpenAsync();
                }

                const string sql = @"
                    SELECT 
                        codcli AS CodCli,
                        nrobocent AS NroBocEnt,
                        nombocent AS NomBocEnt,
                        dombocent AS DomBocEnt
                    FROM BocEnt
                    ORDER BY codcli, nrobocent;";

                var bocas = (await _conn.QueryAsync<BocaEntregaDTO>(sql)).ToList();

                System.Diagnostics.Debug.WriteLine($"[BOCENT LOCAL] ✅ Total bocas encontradas: {bocas.Count}");

                return bocas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BOCENT LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                // Cerrar la conexión si está abierta
                if (_conn.State == ConnectionState.Open)
                {
                    await _conn.CloseAsync();
                }
                System.Diagnostics.Debug.WriteLine("[BOCENT LOCAL] Conexión cerrada");
            }
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetByCliente(int codCli)
        {
            System.Diagnostics.Debug.WriteLine($"[BOCENT LOCAL] === INICIO OBTENER POR CLIENTE {codCli} ===");

            try
            {
                // Abrir la conexión si no está abierta
                if (_conn.State != ConnectionState.Open)
                {
                    await _conn.OpenAsync();
                }

                const string sql = @"
                    SELECT 
                        codcli AS CodCli,
                        nrobocent AS NroBocEnt,
                        nombocent AS NomBocEnt,
                        dombocent AS DomBocEnt
                    FROM BocEnt
                    WHERE codcli = @CodCli
                    ORDER BY nrobocent;";

                var bocas = (await _conn.QueryAsync<BocaEntregaDTO>(sql, new { CodCli = codCli })).ToList();

                System.Diagnostics.Debug.WriteLine($"[BOCENT LOCAL] ✅ Bocas encontradas para cliente {codCli}: {bocas.Count}");

                return bocas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BOCENT LOCAL] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                // Cerrar la conexión si está abierta
                if (_conn.State == ConnectionState.Open)
                {
                    await _conn.CloseAsync();
                }
                System.Diagnostics.Debug.WriteLine("[BOCENT LOCAL] Conexión cerrada");
            }
        }
    }
}

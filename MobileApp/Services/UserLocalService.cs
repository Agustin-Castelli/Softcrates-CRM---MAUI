using Core.DTOs;
using Core.Interfaces;
using Core.Models.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    /// <summary>
    /// Login offline contra la tabla local `sisusuar` (SQLite).
    /// - Matchea por codusr (case-insensitive, trim).
    /// - Valida password (trim para columnas CHAR antiguas).
    /// - Rechaza usuarios inactivos.
    /// Devuelve UserDataResponse con Token vacío (igual que la API).
    /// </summary>
    public class UserLocalService : IUserService
    {
        private readonly SqliteConnection _conn;

        public UserLocalService(SqliteConnection conn)
        {
            _conn = conn;
        }

        public async Task<UserDataResponse?> LoginAsync(UserLoginDto request)
        {
            // Diagnóstico temporal
            await DiagnosticDatabaseAsync();

            const string sql = @"
            SELECT 
                codusr   AS CodUsr, 
                nomusr   AS NomUsr, 
                admusr   AS AdmUsr
            FROM sisusuar
            WHERE 
                UPPER(TRIM(COALESCE(nomusr, ''))) = UPPER(TRIM(@Username))
                AND TRIM(COALESCE(pwdusr, '')) = TRIM(@Password)
                AND (inactivo IS NULL OR inactivo = 0 OR inactivo = '0' OR inactivo = '')
            LIMIT 1;";

            System.Diagnostics.Debug.WriteLine($"[LOGIN] Intentando conectar a: {_conn.ConnectionString}");

            await _conn.OpenAsync();

            System.Diagnostics.Debug.WriteLine("[LOGIN] Conexión abierta exitosamente");
            try
            {
                var user = await _conn.QueryFirstOrDefaultAsync<User>(
                    sql, new { request.Username, request.Password });

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Query ejecutada, usuario encontrado: {user != null}");

                if (user == null)
                    return null;

                return new UserDataResponse
                {
                    CodUsr = user.CodUsr,
                    Name = user.NomUsr ?? "",
                    IsAdmin = user.AdmUsr ?? "N",
                    Token = "" // sin JWT en offline
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Error específico: {ex}");
                throw;
            }
            finally
            {
                await _conn.CloseAsync();
                System.Diagnostics.Debug.WriteLine("[LOGIN] Conexión cerrada");
            }
        }



        private async Task DiagnosticDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DIAGNOSTIC] Probando conexión a: {_conn.ConnectionString}");

                await _conn.OpenAsync();
                System.Diagnostics.Debug.WriteLine("[DIAGNOSTIC] Conexión abierta OK");

                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using var reader = cmd.ExecuteReader();

                System.Diagnostics.Debug.WriteLine("[DIAGNOSTIC] === TABLAS ENCONTRADAS ===");
                var count = 0;
                while (reader.Read())
                {
                    var tableName = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine($"[DIAGNOSTIC] Tabla {count + 1}: {tableName}");
                    count++;
                }
                System.Diagnostics.Debug.WriteLine($"[DIAGNOSTIC] Total: {count} tablas");

                if (count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DIAGNOSTIC] ⚠️ NO SE ENCONTRARON TABLAS!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAGNOSTIC] Error: {ex.Message}");
            }
            finally
            {
                if (_conn.State == System.Data.ConnectionState.Open)
                    await _conn.CloseAsync();
            }
        }
    }
}

using Microsoft.Maui.Storage;

namespace MobileApp.Data
{
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Copia el .db desde Resources/Raw a AppDataDirectory si no existe.
        /// </summary>
        public static string EnsureDatabase(string dbFileName, bool overwrite = false)
        {
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);

            try
            {
                // Si existe pero está vacío, forzar recreación
                if (File.Exists(targetPath))
                {
                    var fileInfo = new FileInfo(targetPath);
                    if (fileInfo.Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[DB INIT] Archivo existente está vacío, forzando recreación");
                        overwrite = true;
                    }
                }

                if (overwrite && File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                    System.Diagnostics.Debug.WriteLine("[DB INIT] Archivo existente eliminado");
                }

                if (!File.Exists(targetPath))
                {
                    System.Diagnostics.Debug.WriteLine("[DB INIT] Copiando DB inicial desde Resources/Raw...");

                    using var stream = FileSystem.OpenAppPackageFileAsync(dbFileName).Result;
                    using var fileStream = File.Create(targetPath);
                    stream.CopyTo(fileStream);

                    System.Diagnostics.Debug.WriteLine("[DB INIT] Copia completada");
                }

                TestDatabaseConnection(targetPath);
                // Resto del método...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB INIT] Error: {ex}");
                throw;
            }

            return targetPath;
        }

        private static void TestDatabaseConnection(string dbPath)
        {
            try
            {
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using var reader = cmd.ExecuteReader();

                System.Diagnostics.Debug.WriteLine("[DB TEST] === TODAS LAS TABLAS ===");
                var tableCount = 0;
                while (reader.Read())
                {
                    System.Diagnostics.Debug.WriteLine($"  - {reader.GetString(0)}");
                    tableCount++;
                }
                System.Diagnostics.Debug.WriteLine($"[DB TEST] Total de tablas: {tableCount}");

                if (tableCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DB TEST] ⚠️ LA BASE DE DATOS ESTÁ VACÍA!");
                }

                System.Diagnostics.Debug.WriteLine("[DB TEST] Conexión exitosa");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB TEST] Error de conexión: {ex.Message}");
                throw;
            }
        }
    }
}
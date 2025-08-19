using Core.DTOs;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using MobileApp.Data;
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
        private readonly SQLiteDbContext _db;

        public UserLocalService(SQLiteDbContext db)
        {
            _db = db;
        }

        public async Task<UserDataResponse?> LoginAsync(UserLoginDto request)
        {
            if (request is null) return null;

            var username = (request.Username ?? string.Empty).Trim().ToUpperInvariant();
            var password = (request.Password ?? string.Empty).Trim();

            // Buscamos por codusr ignorando espacios y case
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    (u.CodUsr ?? string.Empty).Trim().ToUpper() == username);

            if (user is null) return null;

            // Inactivo => no permitir login
            if (user.Inactivo) return null;

            // Validación de password (columna CHAR puede traer espacios)
            var storedPwd = (user.PwdUsr ?? string.Empty).Trim();
            if (!string.Equals(storedPwd, password, StringComparison.Ordinal))
                return null;

            // Map a DTO que usa la UI (igual que respuesta de API)
            return new UserDataResponse
            {
                CodUsr = (user.CodUsr ?? string.Empty).Trim(),
                Name = (user.NomUsr ?? user.CodUsr ?? string.Empty).Trim(),
                IsAdmin = string.IsNullOrWhiteSpace(user.AdmUsr)
                            ? "N"
                            : (user.AdmUsr!.Trim().Equals("S", StringComparison.OrdinalIgnoreCase) ? "S" : user.AdmUsr.Trim()),
                Token = string.Empty // sin JWT en offline / actual
            };
        }
    }
}

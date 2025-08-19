using Core.DTOs;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using MobileApp.Data;
using MobileApp.Models.Entities;
using System;

namespace MobileApp.Services
{
    public class ClientLocalService : IClientService
    {
        private readonly SQLiteDbContext _db; // o lo que uses con SQLite

        public ClientLocalService(SQLiteDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
        {
            return await _db.Clients
                .Where(c => c.NomCli.Contains(term))
                .Select(c => new ClientData
                {
                    CodCli = c.CodCli,
                    NomCli = c.NomCli,
                    SaldoDeuCc = c.SaldoDeuCc,
                    SaldoVencCc = c.SaldoVencCc,
                    LimiteCredito = c.LimiteCredito,
                    LimiteCreditoUso = c.LimiteCreditoUso,
                })
                .ToListAsync();
        }

        public async Task<ClientData> GetClientePorIdAsync(int id)
        {
            var entity = await _db.Clients.FindAsync(id);
            return new ClientData
            {
                CodCli = entity.CodCli,
                NomCli = entity.NomCli,
                SaldoDeuCc = entity.SaldoDeuCc,
                SaldoVencCc = entity.SaldoVencCc,
                LimiteCredito = entity.LimiteCredito,
                LimiteCreditoUso = entity.LimiteCreditoUso,
            };
        }

        public async Task<ClientResumenDto?> GetResumenCliente(int codCli)
        {
            var cliente = await _db.Clients.FirstOrDefaultAsync(c => c.CodCli == codCli);
            if (cliente == null) return null;

            // acá buscás todas las facturas que tengan el mismo CodCli
            var facturas = await _db.Facturas
                .Where(f => f.CodCli == codCli)
                .ToListAsync();

            var dto = new ClientResumenDto
            {
                CodCli = cliente.CodCli,
                Nombre = cliente.NomCli,
                SaldoActual = cliente.SaldoDeuCc,
                LimiteCredito = cliente.LimiteCredito,
                SaldoVencido = cliente.SaldoVencCc,
                PorcentajeCreditoUsado = cliente.LimiteCreditoUso,
                Facturas = facturas.Select(f => new FacturaDto
                {
                    NumeroFactura = f.CSIDcbtDeu,   // el campo PK compuesto que definimos
                    Fecha = f.FechaCbt,
                    Importe = f.ImporteOriginal,
                    Estado = f.EstadoCC
                }).ToList()
            };

            return dto;
        }

    }

}


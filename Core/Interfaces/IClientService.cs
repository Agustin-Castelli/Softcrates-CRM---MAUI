using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IClientService
    {
        Task<IEnumerable<ClientData>> SearchClientsAsync(string term);
        public Task<ClientData> GetClientePorIdAsync(int id);
        public Task<ClientResumenDto> GetResumenCliente(int id);
    }
}

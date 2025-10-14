using Core.DTOs.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IPedidoService
    {
        Task<PedidoDTO> ObtenerPedidoPorCodigo(string codigo);
        Task<IEnumerable<PedidoDTO>> ObtenerHistorialPedidos(int codCli, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<PedidoDTO>> GetAllPedidos();
    }
}

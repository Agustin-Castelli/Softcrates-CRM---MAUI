using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IArticuloService
    {
        Task<IEnumerable<ArticuloDTO>> GetAll();
        Task<IEnumerable<ArticuloDTO>> ObtenerArticulos(int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorNombre(string desArt);
        Task<ArticuloDTO> ObtenerArticuloPorCodigo(string codArt);
    }
}

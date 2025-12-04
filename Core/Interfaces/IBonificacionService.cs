using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IBonificacionService
    {
        Task<IEnumerable<ArticuloConBonificacionDTO>> ObtenerArticulosConBonificacionAsync(int codCli);
        Task<decimal> ObtenerPorcentajeBonificacionAsync(int codCli, string codArt, decimal cantidad, decimal precioUnitario);
        Task<BonificacionDTO?> ObtenerBonificacionBaseAsync(int codCli, string codArt);
    }
}

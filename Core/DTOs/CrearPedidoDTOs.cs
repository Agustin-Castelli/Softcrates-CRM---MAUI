using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class CrearPedidoDTO
    {
        public short CodTipCbt { get; set; } = 96;
        public short CemCbt { get; set; } = 1;
        public int NroCbt { get; set; }   // lo resolvemos más adelante
        public int CodCli { get; set; }
        public int Confirmado { get; set; } = 0;
        public int NroBocEnt { get; set; }

        public List<CrearPedidoDetalleDTO> Detalles { get; set; } = new();
    }

    public class CrearPedidoDetalleDTO
    {
        public string CodArt { get; set; } = string.Empty;
        public string DesArt { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class CrearPedidoSyncDTO : CrearPedidoDTO
    {
        public string Estado { get; set; } = "Pendiente";
        public string Csid { get; set; } = String.Empty;
    }
}

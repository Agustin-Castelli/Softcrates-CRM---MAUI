using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
        public class ClientData
        {
            public int CodCli { get; set; }
            public string NomCli { get; set; } = string.Empty;
            public decimal SaldoDeuCc { get; set; }
            public decimal SaldoVencCc { get; set; }
            public decimal LimiteCredito { get; set; }
            public decimal LimiteCreditoUso { get; set; }
        }

        public class Clients
        {
            public List<ClientData> ClientsList { get; set; }
        }

        public class ClientResumenDto
        {
            // Datos de cliente
            public int CodCli { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public decimal SaldoActual { get; set; }
            public decimal LimiteCredito { get; set; }
            public decimal SaldoVencido { get; set; }
            public decimal PorcentajeCreditoUsado { get; set; }

            // Facturas
            public List<FacturaDto> Facturas { get; set; } = new();
        }

        public class FacturaDto
        {
            public string NumeroFactura { get; set; }
            public DateTime Fecha { get; set; }
            public decimal Importe { get; set; }
            public string Estado { get; set; } = string.Empty;
        }
}

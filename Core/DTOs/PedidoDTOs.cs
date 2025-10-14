using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    namespace Core.DTOs
    {
        public class PedidoDTO
        {
            public short CodTipCbt { get; set; }
            public short CemCbt { get; set; }
            public int NroCbt { get; set; }
            public string Csid { get; set; } = string.Empty;
            public DateTime FecCbt { get; set; }
            public DateTime FecEntCbt { get; set; }
            public int CodCli { get; set; }
            public short CodIva { get; set; }
            public string CodSuc { get; set; } = string.Empty;
            public short CodList { get; set; }
            public short CodClaBon { get; set; }
            public string CodVen { get; set; } = string.Empty;
            public string CodTra { get; set; } = string.Empty;
            public string CodConVta { get; set; } = string.Empty;
            public short CodMon { get; set; }
            public decimal ImpCot { get; set; }
            public decimal ImpGraCbt { get; set; }
            public decimal PorDesCbt { get; set; }
            public decimal ImpDesCbt { get; set; }
            public decimal PorRecCbt { get; set; }
            public decimal ImpRecCbt { get; set; }
            public decimal ImpNetGraCbt { get; set; }
            public decimal ImpIvaCbt { get; set; }
            public decimal ImpTotCbt { get; set; }
            public string ObsCbt { get; set; } = string.Empty;
            public string AutPed { get; set; } = string.Empty;
            public string UsrIngreso { get; set; } = string.Empty;
            public DateTime FecIngreso { get; set; }
            public string UsrAut { get; set; } = string.Empty;
            public DateTime FecAut { get; set; }
            public int NroBocEnt { get; set; }
            public string PedWebInc { get; set; } = string.Empty;
            public string ObsComunicado { get; set; } = string.Empty;
            public DateTime FecPedWebInc { get; set; }
            public string AutPedSec { get; set; } = string.Empty;
            public string UsrAutSec { get; set; } = string.Empty;
            public DateTime FecAutSec { get; set; }

            public List<PedidoDetalleDTO> Detalles { get; set; } = new();
        }

        public class PedidoDetalleDTO
        {
            public string Csid { get; set; } = string.Empty;
            public short Secuencia { get; set; }
            public string CodArt { get; set; } = string.Empty;
            public short CodIvaArt { get; set; }
            public short CodClaBon { get; set; }
            public string DesArtAmp { get; set; } = string.Empty;
            public DateTime FecEntArt { get; set; }
            public decimal CanArt { get; set; }
            public decimal PreArt { get; set; }
            public decimal ImpBonArt { get; set; }
            public decimal ImpGraArt { get; set; }
            public decimal ImpDesArt { get; set; }
            public decimal ImpPrecArt { get; set; }
            public decimal ImpNetGraArt { get; set; }
            public decimal ImpIvaArt { get; set; }
        }
    }

}

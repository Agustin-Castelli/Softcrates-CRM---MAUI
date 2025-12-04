using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class BocaEntregaDTO
    {
        public int CodCli { get; set; }
        public int NroBocEnt { get; set; }
        public string? NomBocEnt { get; set; }
        public string? DomBocEnt { get; set; }
    }
}

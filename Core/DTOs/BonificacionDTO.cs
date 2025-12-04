using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class BonificacionDTO
    {
        public short CodClaBon { get; set; }
        public decimal PorcentajeBonificacion { get; set; }
        public string TipoEscala { get; set; } = string.Empty;
    }

    public class ArticuloConBonificacionDTO
    {
        public string CodArt { get; set; } = string.Empty;
        public string DesArt { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public short? CodClaBon { get; set; }
        public decimal? PorcentajeBonificacion { get; set; }
        public bool TieneBonificacion => CodClaBon.HasValue && PorcentajeBonificacion.HasValue;
    }

    public class BonArtCliDTO
    {
        public int CodCli { get; set; }
        public string CodArt { get; set; } = string.Empty;
        public short CodClaBon { get; set; }
        public bool Inactivo { get; set; }
    }

    public class BonClaDetDTO
    {
        public short CodClaBon { get; set; }
        public short Secuencia { get; set; }
        public string? TipEsc { get; set; }
        public decimal? ValEscDes { get; set; }
        public decimal? ValEscHas { get; set; }
        public decimal? PorBonImp { get; set; }
        public decimal? PorBonCan { get; set; }
        public string? DisFac { get; set; }
    }
}

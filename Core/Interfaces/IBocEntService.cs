using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IBocEntService
    {
        Task<IEnumerable<BocaEntregaDTO>> GetAll();
        Task<IEnumerable<BocaEntregaDTO>> GetByCliente(int codCli);
    }
}

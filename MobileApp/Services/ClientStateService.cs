using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    // ---------------->>>>>>>      ESTA CLASE LA UTILIZAMOS PARA ALMACENAR LA DATA DEL CLIENTE SELECCIONADO EN HOME.RAZOR Y ENVIARLA A OTROS COMPONENTES QUE LA NECESITEN.

    public class ClientStateService
    {
        public event Action OnChange;

        private ClientData _selectedClient;
        public ClientData SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}

using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;

namespace MobileApp.Services
{
    public class ClientProxyService : IClientService
    {
        private readonly IClientService _apiService;
        private readonly IClientService _localService;
        private readonly IConnectivityService _connectivity;

        public ClientProxyService(
            ClientApiService apiService,
            ClientLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;
        }

        public async Task<ClientData> GetClientePorIdAsync(int id)
        {
            if (_connectivity.IsConnected())
            {
                return await _apiService.GetClientePorIdAsync(id);
            }
            else
            {
                return await _localService.GetClientePorIdAsync(id);
            }
        }

        public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
        {
            if (_connectivity.IsConnected())
            {
                return await _apiService.SearchClientsAsync(term);
            }
            else
            {
                return await _localService.SearchClientsAsync(term);
            }
        }

        public async Task<ClientResumenDto?> GetResumenCliente(int codCli)
        {
            if (_connectivity.IsConnected())
            {
                return await _apiService.GetResumenCliente(codCli);
            }
            else
            {
                return await _localService.GetResumenCliente(codCli);
            }
        }


        // Si tenés otros métodos (GetById, Sync, etc.) los replicás con el mismo patrón.
    }
}

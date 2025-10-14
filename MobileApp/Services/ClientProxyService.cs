using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;

namespace MobileApp.Services
{
    //public class ClientProxyService : IClientService
    //{
    //    private readonly IClientService _apiService;
    //    private readonly IClientService _localService;
    //    private readonly IConnectivityService _connectivity;

    //    public ClientProxyService(
    //        ClientApiService apiService,
    //        ClientLocalService localService,
    //        IConnectivityService connectivity)
    //    {
    //        _apiService = apiService;
    //        _localService = localService;
    //        _connectivity = connectivity;
    //    }

    //    public async Task<ClientData> GetClientePorIdAsync(int id)
    //    {
    //        if (_connectivity.IsConnected())
    //        {
    //            return await _apiService.GetClientePorIdAsync(id);
    //        }
    //        else
    //        {
    //            return await _localService.GetClientePorIdAsync(id);
    //        }
    //    }

    //    public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
    //    {
    //        if (_connectivity.IsConnected())
    //        {
    //            System.Diagnostics.Debug.WriteLine("Intentando acceder al metodo SearchClientsAsync desde ProxyService");
    //            return await _apiService.SearchClientsAsync(term);
    //        }
    //        else
    //        {
    //            System.Diagnostics.Debug.WriteLine("Intentando acceder al METODO LOCAL de SearchClientsAsync");
    //            return await _localService.SearchClientsAsync(term);
    //        }
    //    }

    //    public async Task<ClientResumenDto?> GetResumenCliente(int codCli)
    //    {
    //        if (_connectivity.IsConnected())
    //        {
    //            return await _apiService.GetResumenCliente(codCli);
    //        }
    //        else
    //        {
    //            return await _localService.GetResumenCliente(codCli);
    //        }
    //    }


    //    // Si tenés otros métodos (GetById, Sync, etc.) los replicás con el mismo patrón.
    //}

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

        public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
        {
            System.Diagnostics.Debug.WriteLine($"[PROXY] SearchClientsAsync - Término: '{term}'");

            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    // Configurar un timeout más corto para la operación
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    return await _apiService.SearchClientsAsync(term);
                }
                catch (TaskCanceledException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROXY] Timeout en API Service: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error HTTP en API Service: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
            }

            // Usar servicio local como fallback
            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
            return await _localService.SearchClientsAsync(term);
        }

        public async Task<ClientData> GetClientePorIdAsync(int id)
        {
            System.Diagnostics.Debug.WriteLine($"[PROXY] GetClientePorIdAsync - ID: {id}");

            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");
                    return await _apiService.GetClientePorIdAsync(id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
            }

            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
            return await _localService.GetClientePorIdAsync(id);
        }

        public async Task<ClientResumenDto?> GetResumenCliente(int codCli)
        {
            System.Diagnostics.Debug.WriteLine($"[PROXY] GetResumenCliente - CodCli: {codCli}");

            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");
                    return await _apiService.GetResumenCliente(codCli);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
            }

            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
            return await _localService.GetResumenCliente(codCli);
        }
    }
}

using Core.DTOs;
using Core.DTOs.Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class CrearPedidoProxyService : ICrearPedidoService
    {
        private readonly ICrearPedidoService _apiService;
        private readonly ICrearPedidoService _localService;
        private readonly IConnectivityService _connectivity;

        public CrearPedidoProxyService(
            CrearPedidoApiService apiService,
            CrearPedidoLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;
        }

        public async Task<bool> CrearPedidoAsync(CrearPedidoDTO pedido)
        {
            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    return await _apiService.CrearPedidoAsync(pedido);
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
            return await _localService.CrearPedidoAsync(pedido);
        }
    }
}

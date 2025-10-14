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
    public class PedidoProxyService : IPedidoService
    {
        private readonly IPedidoService _apiService;
        private readonly IPedidoService _localService;
        private readonly IConnectivityService _connectivity;

        public PedidoProxyService(
            PedidoApiService apiService,
            PedidoLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;
        }

        public async Task<PedidoDTO> ObtenerPedidoPorCodigo(string codigo)
        {
            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    return await _apiService.ObtenerPedidoPorCodigo(codigo);
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
            return await _localService.ObtenerPedidoPorCodigo(codigo);
        }

        public async Task<IEnumerable<PedidoDTO>> ObtenerHistorialPedidos(int codCli, int pageNumber = 1, int pageSize = 50)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] HISTORIALPEDIDOS: Intentando con API Service");
                    return await _apiService.ObtenerHistorialPedidos(codCli, pageNumber = 1, pageSize = 50);
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
            return await _localService.ObtenerHistorialPedidos(codCli, pageNumber = 1, pageSize = 50);
        }

        public async Task<IEnumerable<PedidoDTO>> GetAllPedidos()
        {
            return null;
        }
    }
}

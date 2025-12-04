using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using Infrastructure.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class BocEntProxyService : IBocEntService
    {
        private readonly IBocEntService _apiService;
        private readonly IBocEntService _localService;
        private readonly IConnectivityService _connectivity;

        public BocEntProxyService(
            BocEntApiService apiService,
            BocEntLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetAll()
        {
            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    return await _apiService.GetAll();
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
            return await _localService.GetAll();
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetByCliente(int codCli)
        {
            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    return await _apiService.GetByCliente(codCli);
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
            return await _localService.GetByCliente(codCli);
        }
    }
}

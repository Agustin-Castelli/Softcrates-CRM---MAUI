using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public class ArticuloProxyService : IArticuloService
    {
        private readonly IArticuloService _apiService;
        private readonly IArticuloService _localService;
        private readonly IConnectivityService _connectivity;

        public ArticuloProxyService(
            ArticuloApiService apiService,
            ArticuloLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;
        }

        public async Task<IEnumerable<ArticuloDTO>> GetAll()
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

        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulos(int pageNumber = 1, int pageSize = 50)
        {
            // Solo intentar API si hay conexión
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

                    return await _apiService.ObtenerArticulos(pageNumber, pageSize);
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
            return await _localService.ObtenerArticulos(pageNumber, pageSize);
        }

        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorNombre(string desArt)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");
                    return await _apiService.ObtenerArticulosPorNombre(desArt);
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
            return await _localService.ObtenerArticulosPorNombre(desArt);
        }

        public async Task<ArticuloDTO> ObtenerArticuloPorCodigo(string codArt)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");
                    return await _apiService.ObtenerArticuloPorCodigo(codArt);
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
            return await _localService.ObtenerArticuloPorCodigo(codArt);
        }
    }
}

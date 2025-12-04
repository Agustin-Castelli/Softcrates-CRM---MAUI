using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using Infrastructure.Local;

namespace MobileApp.Services
{
    public class BonificacionProxyService : IBonificacionService
    {
        private readonly BonificacionApiService _apiService;
        private readonly BonificacionLocalService _localService;
        private readonly IConnectivityService _connectivity;

        public BonificacionProxyService(
            BonificacionApiService apiService,
            BonificacionLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _localService = localService ?? throw new ArgumentNullException(nameof(localService));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        }

        public async Task<IEnumerable<ArticuloConBonificacionDTO>> ObtenerArticulosConBonificacionAsync(int codCli)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] Intentando API para cliente {codCli}");
                    var resultado = await _apiService.ObtenerArticulosConBonificacionAsync(codCli);
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ✅ API exitosa");
                    return resultado;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ❌ Error API: {ex.GetType().Name} - {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BONIF PROXY] Sin conexión detectada");
            }

            System.Diagnostics.Debug.WriteLine("[BONIF PROXY] → Usando Local Service");
            return await _localService.ObtenerArticulosConBonificacionAsync(codCli);
        }

        public async Task<decimal> ObtenerPorcentajeBonificacionAsync(int codCli, string codArt, decimal cantidad, decimal precioUnitario)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] Intentando API para {codArt}");
                    var resultado = await _apiService.ObtenerPorcentajeBonificacionAsync(codCli, codArt, cantidad, precioUnitario);
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ✅ API exitosa: {resultado}%");
                    return resultado;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ❌ Error API: {ex.GetType().Name} - {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BONIF PROXY] Sin conexión detectada");
            }

            System.Diagnostics.Debug.WriteLine("[BONIF PROXY] → Usando Local Service");
            return await _localService.ObtenerPorcentajeBonificacionAsync(codCli, codArt, cantidad, precioUnitario);
        }

        public async Task<BonificacionDTO?> ObtenerBonificacionBaseAsync(int codCli, string codArt)
        {
            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] Intentando API base para {codArt}");
                    var resultado = await _apiService.ObtenerBonificacionBaseAsync(codCli, codArt);
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ✅ API exitosa: {resultado?.PorcentajeBonificacion}%");
                    return resultado;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] ❌ Error API: {ex.GetType().Name} - {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BONIF PROXY] Sin conexión detectada");
            }

            System.Diagnostics.Debug.WriteLine($"[BONIF PROXY] → Usando Local Service para {codArt}");
            return await _localService.ObtenerBonificacionBaseAsync(codCli, codArt);
        }
    }
}



//using Core.DTOs;
//using Core.Interfaces;
//using Infrastructure.Api;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MobileApp.Services
//{
//    public class BonificacionProxyService : IBonificacionService
//    {
//        private readonly IBonificacionService _apiService;
//        private readonly IBonificacionService _localService;
//        private readonly IConnectivityService _connectivity;

//        public BonificacionProxyService(
//            BonificacionApiService apiService,
//            BonificacionLocalService localService,
//            IConnectivityService connectivity)
//        {
//            _apiService = apiService;
//            _localService = localService;
//            _connectivity = connectivity;
//        }

//        public async Task<IEnumerable<ArticuloConBonificacionDTO>> ObtenerArticulosConBonificacionAsync(int codCli)
//        {
//            // Solo intentar API si hay conexión
//            if (_connectivity.IsConnected())
//            {
//                try
//                {
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

//                    return await _apiService.ObtenerArticulosConBonificacionAsync(codCli);
//                }
//                catch (TaskCanceledException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Timeout en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (HttpRequestException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error HTTP en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (Exception ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
//            }

//            // Usar servicio local como fallback
//            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
//            return await _localService.ObtenerArticulosConBonificacionAsync(codCli);
//        }



//        public async Task<decimal> ObtenerPorcentajeBonificacionAsync(int codCli, string codArt, decimal cantidad, decimal precioUnitario)
//        {
//            // Solo intentar API si hay conexión
//            if (_connectivity.IsConnected())
//            {
//                try
//                {
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

//                    return await _apiService.ObtenerPorcentajeBonificacionAsync(codCli, codArt, cantidad, precioUnitario);
//                }
//                catch (TaskCanceledException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Timeout en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (HttpRequestException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error HTTP en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (Exception ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
//            }

//            // Usar servicio local como fallback
//            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
//            return await _localService.ObtenerPorcentajeBonificacionAsync(codCli, codArt, cantidad, precioUnitario);
//        }



//        public async Task<BonificacionDTO?> ObtenerBonificacionBaseAsync(int codCli, string codArt)
//        {
//            // Solo intentar API si hay conexión
//            if (_connectivity.IsConnected())
//            {
//                try
//                {
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Intentando con API Service");

//                    return await _apiService.ObtenerBonificacionBaseAsync(codCli, codArt);
//                }
//                catch (TaskCanceledException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Timeout en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (HttpRequestException ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error HTTP en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//                catch (Exception ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"[PROXY] Error en API Service: {ex.Message}");
//                    System.Diagnostics.Debug.WriteLine("[PROXY] Fallback a Local Service");
//                }
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("[PROXY] Sin conexión a internet");
//            }

//            // Usar servicio local como fallback
//            System.Diagnostics.Debug.WriteLine("[PROXY] Usando Local Service");
//            return await _localService.ObtenerBonificacionBaseAsync(codCli, codArt);
//        }
//    }
//}

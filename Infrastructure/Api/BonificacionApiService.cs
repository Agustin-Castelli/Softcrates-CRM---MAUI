using Core.DTOs;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Api
{
    public class BonificacionApiService : IBonificacionService
    {
        private readonly HttpClient _httpClient;

        public BonificacionApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ArticuloConBonificacionDTO>> ObtenerArticulosConBonificacionAsync(int codCli)
        {
            var response = await _httpClient.GetAsync($"Bonificacion/GetArticulosConBonificacion/{codCli}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<ArticuloConBonificacionDTO>>()
                ?? Enumerable.Empty<ArticuloConBonificacionDTO>();
        }

        public async Task<decimal> ObtenerPorcentajeBonificacionAsync(int codCli, string codArt, decimal cantidad, decimal precioUnitario)
        {
            var response = await _httpClient.GetAsync($"Bonificacion/GetPorcentaje/{codCli}/{codArt}?cantidad={cantidad}&precioUnitario={precioUnitario}");
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<PorcentajeResponse>();
            return resultado?.Porcentaje ?? 0;
        }

        // 🔥 ELIMINAR COMPLETAMENTE EL TRY-CATCH
        public async Task<BonificacionDTO?> ObtenerBonificacionBaseAsync(int codCli, string codArt)
        {
            var response = await _httpClient.GetAsync($"Bonificacion/GetBonificacionBase/{codCli}/{codArt}");
            response.EnsureSuccessStatusCode(); // Lanza excepción que el Proxy capturará

            return await response.Content.ReadFromJsonAsync<BonificacionDTO>();
        }

        private class PorcentajeResponse
        {
            public int CodCli { get; set; }
            public string CodArt { get; set; } = string.Empty;
            public decimal Porcentaje { get; set; }
        }
    }
}

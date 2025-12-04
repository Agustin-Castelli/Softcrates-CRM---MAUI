using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Api
{
    // TENER EN CUENTA QUE PUEDE SER NECESARIA LA UTILIZACION DE UNA INTERFAZ PARA LA SINCRONIZACION

    public class BonClaDetApiService
    {
        private readonly HttpClient _httpClient;

        public BonClaDetApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<BonClaDetDTO>> GetAll()
        {
            var response = await _httpClient.GetAsync("BonClaDet/GetAll");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BonClaDetDTO>>()
                ?? Enumerable.Empty<BonClaDetDTO>();
        }

        public async Task<IEnumerable<BonClaDetDTO>> GetByCodClaBon(short codClaBon)
        {
            var response = await _httpClient.GetAsync($"BonClaDet/GetByCodClaBon/{codClaBon}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BonClaDetDTO>>()
                ?? Enumerable.Empty<BonClaDetDTO>();
        }
    }
}

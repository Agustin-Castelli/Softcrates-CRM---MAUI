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

    public class BonArtCliApiService
    {
        private readonly HttpClient _httpClient;

        public BonArtCliApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<BonArtCliDTO>> GetAll()
        {
            var response = await _httpClient.GetAsync("BonArtCli/GetAll");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BonArtCliDTO>>()
                ?? Enumerable.Empty<BonArtCliDTO>();
        }

        public async Task<IEnumerable<BonArtCliDTO>> GetByCliente(int codCli)
        {
            var response = await _httpClient.GetAsync($"BonArtCli/GetByCliente/{codCli}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BonArtCliDTO>>()
                ?? Enumerable.Empty<BonArtCliDTO>();
        }
    }
}

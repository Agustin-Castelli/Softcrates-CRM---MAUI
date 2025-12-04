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
    public class BocEntApiService : IBocEntService
    {
        private readonly HttpClient _httpClient;

        public BocEntApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetAll()
        {
            var response = await _httpClient.GetAsync("BocEnt/GetAll");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BocaEntregaDTO>>()
                ?? Enumerable.Empty<BocaEntregaDTO>();
        }

        public async Task<IEnumerable<BocaEntregaDTO>> GetByCliente(int codCli)
        {
            var response = await _httpClient.GetAsync($"BocEnt/GetByCliente/{codCli}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BocaEntregaDTO>>()
                ?? Enumerable.Empty<BocaEntregaDTO>();
        }
    }
}

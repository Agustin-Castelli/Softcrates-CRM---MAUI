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
    public class ClientApiService : IClientService
    {
        private readonly HttpClient _httpClient;

        public ClientApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ClientData>> SearchClientsAsync(string term)
        {
            // Endpoint de tu API: GET /api/Client/search?name={term}
            var url = $"Client/search?name={Uri.EscapeDataString(term)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ClientData>>()
                   ?? Enumerable.Empty<ClientData>();
        }

        public async Task<ClientData> GetClientePorIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"Client/GetById/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClientData>();
        }

        public async Task<ClientResumenDto> GetResumenCliente(int id)
        {
            var response = await _httpClient.GetAsync($"Client/resumen/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClientResumenDto>();
        }

        // Otros métodos según la interfaz IClienteService...
    }
}

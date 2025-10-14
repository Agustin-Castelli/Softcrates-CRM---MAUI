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
    public class ArticuloApiService : IArticuloService
    {
        private readonly HttpClient _httpClient;

        public ArticuloApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ArticuloDTO>> GetAll() 
        { 
            var response = await _httpClient.GetAsync($"Articulo/GetAll");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ArticuloDTO>>() 
                ?? Enumerable.Empty<ArticuloDTO>(); 
        }

        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulos(int pageNumber = 1, int pageSize = 50)
        {
            var response = await _httpClient.GetAsync($"Articulo/ObtenerTodos?pageNumber={pageNumber}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ArticuloDTO>>()
                ?? Enumerable.Empty<ArticuloDTO>();
        }

        public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorNombre(string desArt)
        {
            var response = await _httpClient.GetAsync($"Articulo/{desArt}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ArticuloDTO>>()
                ?? Enumerable.Empty<ArticuloDTO>();
        }

        public async Task<ArticuloDTO> ObtenerArticuloPorCodigo(string codArt)
        {
            var response = await _httpClient.GetAsync($"Articulo/{codArt}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ArticuloDTO>();

        }
    }
}

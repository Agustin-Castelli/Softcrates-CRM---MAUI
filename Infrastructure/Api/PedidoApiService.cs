using Core.DTOs;
using Core.DTOs.Core.DTOs;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Api
{
    public class PedidoApiService : IPedidoService
    {
        private readonly HttpClient _httpClient;

        public PedidoApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PedidoDTO> ObtenerPedidoPorCodigo(string codigo)
        {
            var response = await _httpClient.GetAsync($"Pedido/ObtenerPedidoPorNumero/{codigo}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PedidoDTO>();
        }

        public async Task<IEnumerable<PedidoDTO>> ObtenerHistorialPedidos(int codCli, int pageNumber = 1, int pageSize = 50)
        {
            System.Diagnostics.Debug.WriteLine($"[PedidoApiService] 🔹 Obteniendo historial de pedidos: cliente={codCli}, página={pageNumber}, tamaño={pageSize}");

            var response = await _httpClient.GetAsync($"Pedido/ObtenerHistorial?codCli={codCli}&pageNumber={pageNumber}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<PedidoDTO>>() ?? Enumerable.Empty<PedidoDTO>();
        }


        public async Task<IEnumerable<PedidoDTO>> GetAllPedidos()
        {
            System.Diagnostics.Debug.WriteLine($"INGRESANDO AL METODO GetAllPedidos.");
            var response = await _httpClient.GetAsync($"Pedido/GetAllPedidos");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<PedidoDTO>>()
                ?? Enumerable.Empty<PedidoDTO>();
        }
    }
}

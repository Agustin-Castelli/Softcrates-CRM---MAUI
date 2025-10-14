using Core.DTOs;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Api
{
    // Services/CrearPedidoApiService.cs
    public class CrearPedidoApiService : ICrearPedidoService
    {
        private readonly HttpClient _http;

        public CrearPedidoApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> CrearPedidoAsync(CrearPedidoDTO pedido)
        {
            var response = await _http.PostAsJsonAsync("Pedido/CrearPedido", pedido);
            return response.IsSuccessStatusCode;
        }

        // 🚀 Nuevo método para sincronizar pedidos pendientes
        public async Task<IEnumerable<CrearPedidoDTO>> CrearPedidosMasivoAsync(IEnumerable<CrearPedidoDTO> pedidos)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Pedido/SyncPedidos", pedidos);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<IEnumerable<CrearPedidoDTO>>();
                return result ?? Enumerable.Empty<CrearPedidoDTO>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex, "Error al sincronizar pedidos pendientes");
                throw;
            }
        }
    }

}

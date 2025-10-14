using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    // Services/CartStateService.cs
    using Core.DTOs;

    public class CartStateService
    {
        private readonly List<CrearPedidoDetalleDTO> _items = new();
        public IReadOnlyList<CrearPedidoDetalleDTO> Items => _items;

        public event Action? OnChange;

        public decimal ObtenerMontoTotal() => _items.Sum(item => item.Cantidad * item.PrecioUnitario);

        public void AddOrUpdateItem(ArticuloDTO articulo, int cantidad)
        {
            if (cantidad <= 0)
            {
                RemoveItem(articulo.CodArt);
                return;
            }

            var existing = _items.FirstOrDefault(i => i.CodArt == articulo.CodArt);
            if (existing != null)
            {
                existing.Cantidad = cantidad;
                existing.PrecioUnitario = articulo.Precio;
                existing.DesArt = articulo.DesArt;
            }
            else
            {
                _items.Add(new CrearPedidoDetalleDTO
                {
                    CodArt = articulo.CodArt,
                    DesArt = articulo.DesArt,
                    Cantidad = cantidad,
                    PrecioUnitario = articulo.Precio
                });
            }

            NotifyStateChanged();
        }

        public void RemoveItem(string codArt)
        {
            var item = _items.FirstOrDefault(i => i.CodArt == codArt);
            if (item != null)
            {
                _items.Remove(item);
                NotifyStateChanged();
            }
        }

        public void Clear()
        {
            _items.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

}

// Infrastructure/Services/ConnectivityService.cs
using Core.Interfaces;
using Microsoft.Maui.Networking;

namespace MobileApp.Services
{
    public class ConnectivityService : IConnectivityService
    {
        private readonly IConnectivity _connectivity;

        public event EventHandler<bool>? ConnectivityChanged;

        public ConnectivityService(IConnectivity connectivity)
        {
            _connectivity = connectivity;

            _connectivity.ConnectivityChanged += (_, __) =>
            {
                var isConnected = IsConnected();
                ConnectivityChanged?.Invoke(this, isConnected);
            };
        }

        public bool IsConnected()
        {
            return _connectivity.NetworkAccess == NetworkAccess.Internet;
        }
    }
}


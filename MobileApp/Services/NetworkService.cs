using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public interface INetworkService
    {
        bool IsUsingCellularData();
        bool IsConnected { get; }
    }

    public class NetworkService : INetworkService
    {
        public bool IsConnected 
        { 
            get 
            {
                var connectivity = Connectivity.Current;
                return connectivity.NetworkAccess == NetworkAccess.Internet;
            }
        }

        public bool IsUsingCellularData()
        {
            var connectivity = Connectivity.Current;
            
            // Está usando datos móviles si:
            // - Tiene conexión cellular
            // - Y NO tiene WiFi disponible
            return connectivity.ConnectionProfiles.Contains(ConnectionProfile.Cellular) 
                   && !connectivity.ConnectionProfiles.Contains(ConnectionProfile.WiFi);
        }
    }

    public interface IDialogService
    {
        Task ShowAlertAsync(string title, string message, string buttonText = "Entendido");
    }

    public class DialogService : IDialogService
    {
        public async Task ShowAlertAsync(string title, string message, string buttonText = "Entendido")
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, buttonText);
            }
        }
    }
}

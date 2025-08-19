// Core/Interfaces/IConnectivityService.cs
namespace Core.Interfaces
{
    public interface IConnectivityService
    {
        bool IsConnected();
        event EventHandler<bool> ConnectivityChanged;
    }
}

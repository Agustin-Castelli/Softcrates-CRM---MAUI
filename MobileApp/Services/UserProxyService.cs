using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using Microsoft.Maui.Media;
using System;
using System.Net.Http;

namespace MobileApp.Services
{
    public class UserProxyService : IUserService
    {
        private readonly IUserService _apiService;
        private readonly IUserService _localService;
        private readonly IConnectivityService _connectivity;

        public UserProxyService(
            UserApiService apiService,
            UserLocalService localService,
            IConnectivityService connectivity)
        {
            _apiService = apiService;
            _localService = localService;
            _connectivity = connectivity;

            System.Diagnostics.Debug.WriteLine($"--------------> UserProxyService inicializado <--------------");
        }

        public async Task<UserDataResponse?> LoginAsync(UserLoginDto request)
        {
            System.Diagnostics.Debug.WriteLine($"--------------> Entrando a Proxy.LoginAsync <--------------");

            if (_connectivity.IsConnected())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"--------------> Intentando login contra API <--------------");

                    return await _apiService.LoginAsync(request);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex}");
                    System.Diagnostics.Debug.WriteLine($"--------------> Falló login contra API, probando Local. <--------------");
                    return await _localService.LoginAsync(request);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"--------------> Sin internet, usando Local <--------------");
                return await _localService.LoginAsync(request);
            }
        }
    }
}


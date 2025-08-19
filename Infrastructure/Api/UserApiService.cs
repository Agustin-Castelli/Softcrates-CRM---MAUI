using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Api
{
    public class UserApiService : IUserService
    {
        private readonly HttpClient _httpClient;

        public UserApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserDataResponse?> LoginAsync(UserLoginDto request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BaseAddress: {_httpClient.BaseAddress}");
                System.Diagnostics.Debug.WriteLine($"Intentando login a: {_httpClient.BaseAddress}User/Login");

                var response = await _httpClient.PostAsJsonAsync("User/Login", request);

                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error response: {errorContent}");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<UserDataResponse>();
                if (result != null)
                {
                    result.Token = string.Empty;
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception en LoginAsync: {ex}");
                throw;
            }
        }
    }
}

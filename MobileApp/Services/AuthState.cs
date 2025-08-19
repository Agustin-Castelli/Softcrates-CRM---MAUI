using Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Services
{
    public interface IAuthState
    {
        bool IsLoggedIn { get; }
        UserDataResponse? CurrentUser { get; } // Cambia UserDto por el nombre de tu DTO
        event Action OnAuthenticationStateChanged;
        void SetLoggedIn(bool value, UserDataResponse? user = null);
        void Logout();
    }

    public class AuthState : IAuthState
    {
        public bool IsLoggedIn { get; private set; }
        public UserDataResponse? CurrentUser { get; private set; } // Cambia UserDto por el nombre de tu DTO
        public event Action OnAuthenticationStateChanged;

        public void SetLoggedIn(bool value, UserDataResponse? user = null)
        {
            IsLoggedIn = value;
            CurrentUser = value ? user : null;
            OnAuthenticationStateChanged?.Invoke();
        }

        public void Logout()
        {
            IsLoggedIn = false;
            CurrentUser = null;
            OnAuthenticationStateChanged?.Invoke();
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserDataResponse
    {
        public string CodUsr { get; set; }
        public string Name { get; set; }
        public string IsAdmin { get; set; }
        public string Token { get; set; }  // En el futuro vendrá el JWT acá
    }
}

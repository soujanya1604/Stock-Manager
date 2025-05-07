using Microsoft.AspNetCore.Identity;

namespace Stock_Manager.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string Password { get; set; }
    }

}

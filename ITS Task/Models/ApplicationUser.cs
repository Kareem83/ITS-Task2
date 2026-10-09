using Microsoft.AspNetCore.Identity;

namespace ITS_Task.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Employee? Employee { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
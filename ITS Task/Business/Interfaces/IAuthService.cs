using ITS_Task.Business.DTOs;

namespace ITS_Task.Business.Interfaces
{
    public interface IAuthService
    {
        Task<RefreshTokenDto?> LoginAsync(
            LoginDto loginDto);

        Task<RefreshTokenDto?> RefreshTokenAsync(
            string refreshToken);
    }
}
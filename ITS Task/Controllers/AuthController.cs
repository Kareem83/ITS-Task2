using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ITS_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginDto loginDto)
        {
            var result =
                await _authService
                    .LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(
                    "Invalid email or password");

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            RefreshTokenRequestDto dto)
        {
            var result =
                await _authService
                    .RefreshTokenAsync(
                        dto.RefreshToken);

            if (result == null)
                return Unauthorized(
                    "Invalid or expired refresh token");

            return Ok(result);
        }
    }
}
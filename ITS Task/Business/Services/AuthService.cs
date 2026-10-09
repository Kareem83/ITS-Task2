using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.Business.Settings;
using ITS_Task.Models;
using ITS_Task.Business.Exceptions;
using ITS_Task.DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ITS_Task.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITransactionRunner _transactions;
        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtSettings> jwtOptions, AppDbContext context, ITransactionRunner transactions)
        {
            _userManager = userManager;
            _jwtSettings = jwtOptions.Value;
            _context = context;
            _transactions = transactions;
        }

        public async Task<RefreshTokenDto?> LoginAsync(
            LoginDto loginDto)
        {
            return await _transactions.RunAsync<RefreshTokenDto?>(async () =>
            {
            var user =
                await _userManager
                    .FindByEmailAsync(loginDto.Email);

            if (user == null || await _context.Employees.AnyAsync(e => e.UserId == user.Id && !e.IsActive))
                return null;

            var passwordValid =
                await _userManager
                    .CheckPasswordAsync(
                        user,
                        loginDto.Password);

            if (!passwordValid)
                return null;

            var accessToken =
                await GenerateAccessTokenAsync(user);

            var refreshToken =
                GenerateRefreshToken();

            user.RefreshToken = refreshToken;

            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(7);

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded) throw new ApiException(409, "Authentication state changed; retry");

            return new RefreshTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        
            });
        }

        public async Task<RefreshTokenDto?> RefreshTokenAsync(
            string refreshToken)
        {
            return await _transactions.RunAsync<RefreshTokenDto?>(async () =>
            {
            if (string.IsNullOrWhiteSpace(refreshToken)) return null;
            var user =
                await _userManager.Users
                    .FirstOrDefaultAsync(
                        x => x.RefreshToken == refreshToken);

            if (user == null || await _context.Employees.AnyAsync(e => e.UserId == user.Id && !e.IsActive))
                return null;

            if (user.RefreshTokenExpiryTime == null ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var newAccessToken =
                await GenerateAccessTokenAsync(user);

            var newRefreshToken =
                GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;

            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(7);

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded) throw new ApiException(409, "Authentication state changed; retry");

            return new RefreshTokenDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        
            });
        }

        private async Task<string>
            GenerateAccessTokenAsync(
                ApplicationUser user)
        {
            var roles =
                await _userManager
                    .GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id),

                new Claim(
                    ClaimTypes.Email,
                    user.Email!)
            };

            foreach (var role in roles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _jwtSettings.Key));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires:
                        DateTime.UtcNow.AddMinutes(
                            _jwtSettings.DurationInMinutes),
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes =
                new byte[64];

            using var rng =
                RandomNumberGenerator.Create();

            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(
                randomBytes);
        }
    }
}
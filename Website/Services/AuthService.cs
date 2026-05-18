using Website.Dtos;
using Website.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Website.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Kiểm tra email đã tồn tại
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                throw new InvalidOperationException("Email đã được sử dụng.");

            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                UserName = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address?.Trim()
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            return GenerateToken(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (user is null)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            return GenerateToken(user);
        }

        private AuthResponseDto GenerateToken(ApplicationUser user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("fullName", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(expireMinutes);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = user.Email!,
                FullName = user.FullName,
                Expiry = expiry
            };
        }

    }
}

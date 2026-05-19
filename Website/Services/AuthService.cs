using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Website.Dtos;
using Website.Models;

namespace Website.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

            // Đảm bảo Role "Admin" và "User" tồn tại trong database
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            // Gán quyền: Nếu là tài khoản đầu tiên trong hệ thống HOẶC email bắt đầu bằng "admin@", cấp quyền Admin. Còn lại là User.
            var isFirstUser = !await _userManager.Users.AnyAsync();
            var roleToAssign = (isFirstUser || dto.Email.Trim().ToLower().StartsWith("admin@")) ? "Admin" : "User";

            await _userManager.AddToRoleAsync(user, roleToAssign);

            return await GenerateTokenAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (user is null)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            return await GenerateTokenAsync(user);
        }

        private async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("fullName", user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Thêm các quyền (Roles) vào Claims của JWT token
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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
                FullName = user.FullName ?? string.Empty,
                Expiry = expiry
            };
        }

    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Website.Dtos;
using Website.Models;
using Website.Services;
using static Website.Dtos.DTOs;

namespace Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        /// <summary>Đăng ký tài khoản mới</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng ký thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Đăng nhập</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng nhập thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Lấy danh sách tất cả người dùng (Chỉ Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userList.Add(new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.Address,
                    Roles = roles
                });
            }
            return Ok(ApiResponse<object>.Ok(userList, "Lấy danh sách người dùng thành công"));
        }

        /// <summary>Cấp/Thay đổi quyền của người dùng (Chỉ Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng với email này."));
            }

            // Xóa tất cả quyền hiện tại
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(ApiResponse<object>.Fail("Không thể xóa các quyền cũ."));
            }

            // Thêm quyền mới
            var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<object>.Fail(errors));
            }

            return Ok(ApiResponse<object>.Ok(null, $"Đã cập nhật quyền của {dto.Email} thành {dto.Role}"));
        }
    }
}

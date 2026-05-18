using Microsoft.AspNetCore.Mvc;
using Website.Dtos;
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

        public AuthController(IAuthService authService) => _authService = authService;

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
    }
}

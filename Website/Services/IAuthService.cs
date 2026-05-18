using Website.Dtos;
using static Website.Dtos.AuthResponseDto;

namespace Website.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    }
}

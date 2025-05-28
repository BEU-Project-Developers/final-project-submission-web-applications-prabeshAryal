using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MusicAppBackend.DTOs;
using MusicAppBackend.Services;

namespace MusicAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO register)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", register.Email);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration failed due to invalid model state for email: {Email}", register.Email);
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(register);

            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for email: {Email}. Reason: {Message}", register.Email, result.Message);
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("User successfully registered with email: {Email}", register.Email);
            return Ok(new { message = result.Message, user = result.User });
        }        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            _logger.LogInformation("Login attempt for email: {Email}", login.Email);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed due to invalid model state for email: {Email}", login.Email);
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(login);

            if (!result.Success)
            {
                _logger.LogWarning("Login failed for email: {Email}. Reason: {Message}", login.Email, result.Message);
                return Unauthorized(new { message = result.Message });
            }

            _logger.LogInformation("User successfully logged in with email: {Email}", login.Email);
            return Ok(new TokenResponseDTO
            {
                Token = result.Token!,
                RefreshToken = result.RefreshToken!,
                User = result.User!
            });
        }        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO refreshTokenDto)
        {
            _logger.LogInformation("Token refresh attempt");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Token refresh failed due to invalid model state");
                return BadRequest(ModelState);
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

            if (!result.Success)
            {
                _logger.LogWarning("Token refresh failed: Invalid refresh token");
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            _logger.LogInformation("Token successfully refreshed");
            return Ok(new
            {
                token = result.Token,
                refreshToken = result.RefreshToken
            });
        }        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDTO refreshTokenDto)
        {
            _logger.LogInformation("Token revocation attempt");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Token revocation failed due to invalid model state");
                return BadRequest(ModelState);
            }

            var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);

            if (!result)
            {
                _logger.LogWarning("Token revocation failed");
                return BadRequest(new { message = "Token revocation failed" });
            }

            _logger.LogInformation("Token successfully revoked");
            return Ok(new { message = "Token revoked successfully" });
        }
    }
} 
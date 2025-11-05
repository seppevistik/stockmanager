using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using System.Security.Claims;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(registerDto, ipAddress);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(loginDto, ipAddress);

        if (!result.Success)
            return Unauthorized(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var profile = await _authService.GetUserProfileAsync(userId);
        if (profile == null)
            return NotFound(new { message = "User profile not found" });

        return Ok(profile);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var result = await _authService.UpdateUserProfileAsync(userId, updateDto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Build reset URL from request origin
        var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password";

        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto, resetUrl);

        // Always return success to prevent email enumeration
        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResetPasswordAsync(resetPasswordDto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Password has been reset successfully" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ipAddress);

        if (!result.Success)
            return Unauthorized(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest revokeTokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = GetIpAddress();
        var result = await _authService.RevokeTokenAsync(revokeTokenDto.RefreshToken, ipAddress);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Token revoked successfully" });
    }

    private string GetIpAddress()
    {
        // Try to get IP from X-Forwarded-For header (for proxies/load balancers)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }

        // Fallback to RemoteIpAddress
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

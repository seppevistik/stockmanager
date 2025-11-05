using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using System.Security.Claims;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQuery query)
    {
        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var result = await _userService.GetUsersAsync(businessId, query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var user = await _userService.GetUserByIdAsync(id, businessId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var userId = GetUserId();
        var result = await _userService.CreateUserAsync(request, businessId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.User!.Id }, result.User);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var userId = GetUserId();
        var result = await _userService.UpdateUserAsync(id, request, businessId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "User updated successfully" });
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var userId = GetUserId();
        var result = await _userService.ToggleUserStatusAsync(id, businessId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "User status toggled successfully" });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(string id, [FromBody] ResetUserPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var result = await _userService.ResetUserPasswordAsync(id, businessId, request.NewPassword);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "User password reset successfully" });
    }

    [HttpPost("{id}/revoke-sessions")]
    public async Task<IActionResult> RevokeUserSessions(string id)
    {
        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var userId = GetUserId();
        var result = await _userService.RevokeUserSessionsAsync(id, businessId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "User sessions revoked successfully" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var businessId = GetBusinessId();
        if (businessId == 0)
        {
            return Unauthorized(new { message = "Business ID not found" });
        }

        var stats = await _userService.GetUserStatisticsAsync(businessId);
        return Ok(stats);
    }

    private int GetBusinessId()
    {
        var businessIdClaim = User.FindFirstValue("BusinessId");
        return int.TryParse(businessIdClaim, out var businessId) ? businessId : 0;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}

public class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

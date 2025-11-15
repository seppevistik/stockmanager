using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Data.Contexts;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusinessController : ControllerBase
{
    private readonly BusinessService _businessService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public BusinessController(
        BusinessService businessService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _businessService = businessService;
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private int GetUserRole() => int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "3");

    /// <summary>
    /// Get the current user's business information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBusiness()
    {
        var businessId = GetBusinessId();
        var business = await _businessService.GetBusinessByIdAsync(businessId);

        if (business == null)
            return NotFound(new { message = "Business not found" });

        return Ok(business);
    }

    /// <summary>
    /// Update business settings (Admin only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBusiness([FromBody] UpdateBusinessDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();
        var userRole = GetUserRole();

        var result = await _businessService.UpdateBusinessAsync(businessId, updateDto, userId, userRole);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Create a new business and link to user as Admin
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var result = await _businessService.CreateBusinessAsync(userId, createDto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { businessId = result.BusinessId });
    }

    /// <summary>
    /// Get all businesses the user has access to
    /// </summary>
    [HttpGet("my-businesses")]
    public async Task<IActionResult> GetMyBusinesses()
    {
        var userId = GetUserId();
        var businesses = await _businessService.GetUserBusinessesAsync(userId);

        return Ok(businesses);
    }

    /// <summary>
    /// Switch to a different business and get new JWT token
    /// </summary>
    [HttpPost("switch")]
    public async Task<IActionResult> SwitchBusiness([FromBody] SwitchBusinessDto switchDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        // Switch the business
        var result = await _businessService.SwitchBusinessAsync(userId, switchDto.BusinessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        // Get updated user and business info
        var user = await _userManager.FindByIdAsync(userId);
        var business = await _context.Businesses.FindAsync(switchDto.BusinessId);
        var userBusiness = await _context.UserBusinesses
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == switchDto.BusinessId && ub.IsActive);

        if (user == null || business == null || userBusiness == null)
            return StatusCode(500, new { message = "Failed to retrieve updated business information" });

        // Generate new JWT token with new business context
        var token = GenerateJwtToken(user, business.Id, business.Name, userBusiness.Role.ToString(), out DateTime expiresAt);

        // Get all user's businesses
        var businesses = await _businessService.GetUserBusinessesAsync(userId);

        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = null, // Refresh token remains the same
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = userBusiness.Role.ToString(),
            BusinessId = business.Id,
            BusinessName = business.Name,
            Businesses = businesses
        };

        return Ok(response);
    }

    private string GenerateJwtToken(ApplicationUser user, int businessId, string businessName, string role, out DateTime expiresAt)
    {
        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, role),
            new Claim("BusinessId", businessId.ToString()),
            new Claim("BusinessName", businessName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiresAt = DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claimsList,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

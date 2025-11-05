using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.API.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> RegisterAsync(RegisterDto registerDto)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            return (false, "User with this email already exists", null);
        }

        // Create business first
        var business = new Business
        {
            Name = registerDto.BusinessName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Create user
        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            BusinessId = business.Id,
            Role = registerDto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        var token = GenerateJwtToken(user, business.Name);

        var response = new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            BusinessId = user.BusinessId,
            BusinessName = business.Name
        };

        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !user.IsActive)
        {
            return (false, "Invalid email or password", null);
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!validPassword)
        {
            return (false, "Invalid email or password", null);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var business = await _context.Businesses.FindAsync(user.BusinessId);
        if (business == null)
        {
            return (false, "Business not found", null);
        }

        var token = GenerateJwtToken(user, business.Name);

        var response = new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            BusinessId = user.BusinessId,
            BusinessName = business.Name
        };

        return (true, null, response);
    }

    private string GenerateJwtToken(ApplicationUser user, string businessName)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("BusinessId", user.BusinessId.ToString()),
            new Claim("BusinessName", businessName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Profile Management
    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.Business)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = (int)user.Role,
            RoleName = user.Role.ToString(),
            BusinessId = user.BusinessId,
            BusinessName = user.Business.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<(bool Success, string? Error)> UpdateUserProfileAsync(string userId, UpdateUserProfileRequest updateDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Update user details
        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;

        // Check if email is being changed
        if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
        {
            // Check if new email is already taken
            var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return (false, "Email is already taken");
            }

            user.Email = updateDto.Email;
            user.UserName = updateDto.Email;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    // Password Management
    public async Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, ChangePasswordRequest changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            changePasswordDto.CurrentPassword,
            changePasswordDto.NewPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordDto, string resetUrl)
    {
        var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
        if (user == null || !user.IsActive)
        {
            // Don't reveal that the user does not exist or is not active
            return (true, null);
        }

        // Generate reset token
        var resetToken = GenerateResetToken();

        // Store token in database
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(passwordResetToken);
        await _context.SaveChangesAsync();

        // Send email with reset link
        await _emailService.SendPasswordResetEmailAsync(user.Email!, resetToken, resetUrl);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest resetPasswordDto)
    {
        // Find the reset token
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == resetPasswordDto.Token &&
                t.User.Email == resetPasswordDto.Email &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            return (false, "Invalid or expired reset token");
        }

        var user = resetToken.User;

        // Reset password using UserManager
        var removePasswordResult = await _userManager.RemovePasswordAsync(user);
        if (!removePasswordResult.Succeeded)
        {
            return (false, "Failed to reset password");
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, resetPasswordDto.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
            return (false, string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
        }

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private string GenerateResetToken()
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}

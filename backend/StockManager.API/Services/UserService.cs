using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.API.Services;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int businessId, UserListQuery query)
    {
        var usersQuery = _context.Users
            .Include(u => u.Business)
            .Where(u => u.BusinessId == businessId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u =>
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower));
        }

        if (query.Role.HasValue)
        {
            usersQuery = usersQuery.Where(u => (int)u.Role == query.Role.Value);
        }

        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        // Get total count
        var totalCount = await usersQuery.CountAsync();

        // Apply pagination
        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Get active session counts
        var userIds = users.Select(u => u.Id).ToList();
        var activeSessionCounts = await _context.RefreshTokens
            .Where(rt => userIds.Contains(rt.UserId) && rt.IsActive)
            .GroupBy(rt => rt.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = (int)u.Role,
            RoleName = u.Role.ToString(),
            BusinessId = u.BusinessId,
            BusinessName = u.Business.Name,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            ActiveSessionCount = activeSessionCounts.GetValueOrDefault(u.Id, 0)
        }).ToList();

        return new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, int businessId)
    {
        var user = await _context.Users
            .Include(u => u.Business)
            .FirstOrDefaultAsync(u => u.Id == userId && u.BusinessId == businessId);

        if (user == null)
        {
            return null;
        }

        var activeSessionCount = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == userId && rt.IsActive);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = (int)user.Role,
            RoleName = user.Role.ToString(),
            BusinessId = user.BusinessId,
            BusinessName = user.Business.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            ActiveSessionCount = activeSessionCount
        };
    }

    public async Task<(bool Success, string? Error, UserDto? User)> CreateUserAsync(
        CreateUserRequest request,
        int businessId,
        string createdByUserId)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, "User with this email already exists", null);
        }

        // Create user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            BusinessId = businessId,
            Role = (UserRole)request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.TemporaryPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        // Send welcome email if requested
        if (request.SendWelcomeEmail)
        {
            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);
        }

        var userDto = await GetUserByIdAsync(user.Id, businessId);
        return (true, null, userDto);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        int businessId,
        string updatedByUserId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.BusinessId == businessId);

        if (user == null)
        {
            return (false, "User not found");
        }

        // Prevent self-demotion from admin
        if (userId == updatedByUserId && user.Role == UserRole.Admin && (UserRole)request.Role != UserRole.Admin)
        {
            return (false, "You cannot remove your own admin privileges");
        }

        // Update user details
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = (UserRole)request.Role;

        // Update email if changed
        if (request.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return (false, "Email is already taken by another user");
            }

            user.Email = request.Email;
            user.UserName = request.Email;
        }

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleUserStatusAsync(
        string userId,
        int businessId,
        string updatedByUserId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.BusinessId == businessId);

        if (user == null)
        {
            return (false, "User not found");
        }

        // Prevent self-deactivation
        if (userId == updatedByUserId)
        {
            return (false, "You cannot deactivate your own account");
        }

        // Toggle active status
        user.IsActive = !user.IsActive;

        // If deactivating, revoke all refresh tokens
        if (!user.IsActive)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = "admin_action";
            }
        }

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetUserPasswordAsync(
        string userId,
        int businessId,
        string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.BusinessId == businessId);

        if (user == null)
        {
            return (false, "User not found");
        }

        // Reset password
        var removePasswordResult = await _userManager.RemovePasswordAsync(user);
        if (!removePasswordResult.Succeeded)
        {
            return (false, "Failed to reset password");
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addPasswordResult.Succeeded)
        {
            return (false, string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RevokeUserSessionsAsync(
        string userId,
        int businessId,
        string revokedByUserId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.BusinessId == businessId);

        if (user == null)
        {
            return (false, "User not found");
        }

        // Revoke all active refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = $"admin_action_by_{revokedByUserId}";
        }

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<int> GetUserCountAsync(int businessId)
    {
        return await _context.Users
            .CountAsync(u => u.BusinessId == businessId);
    }

    public async Task<Dictionary<string, int>> GetUserStatisticsAsync(int businessId)
    {
        var users = await _context.Users
            .Where(u => u.BusinessId == businessId)
            .ToListAsync();

        return new Dictionary<string, int>
        {
            ["Total"] = users.Count,
            ["Active"] = users.Count(u => u.IsActive),
            ["Inactive"] = users.Count(u => !u.IsActive),
            ["Admins"] = users.Count(u => u.Role == UserRole.Admin),
            ["Managers"] = users.Count(u => u.Role == UserRole.Manager),
            ["Staff"] = users.Count(u => u.Role == UserRole.Staff),
            ["Viewers"] = users.Count(u => u.Role == UserRole.Viewer)
        };
    }
}

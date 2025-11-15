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
        var userBusinessQuery = _context.UserBusinesses
            .Include(ub => ub.User)
            .Include(ub => ub.Business)
            .Where(ub => ub.BusinessId == businessId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            userBusinessQuery = userBusinessQuery.Where(ub =>
                ub.User.FirstName.ToLower().Contains(searchLower) ||
                ub.User.LastName.ToLower().Contains(searchLower) ||
                (ub.User.Email != null && ub.User.Email.ToLower().Contains(searchLower)));
        }

        if (query.Role.HasValue)
        {
            userBusinessQuery = userBusinessQuery.Where(ub => (int)ub.Role == query.Role.Value);
        }

        if (query.IsActive.HasValue)
        {
            userBusinessQuery = userBusinessQuery.Where(ub => ub.IsActive == query.IsActive.Value && ub.User.IsActive == query.IsActive.Value);
        }

        // Get total count
        var totalCount = await userBusinessQuery.CountAsync();

        // Apply pagination
        var userBusinesses = await userBusinessQuery
            .OrderBy(ub => ub.User.LastName)
            .ThenBy(ub => ub.User.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Get active session counts
        var userIds = userBusinesses.Select(ub => ub.User.Id).ToList();
        var now = DateTime.UtcNow;
        var activeSessionCounts = await _context.RefreshTokens
            .Where(rt => userIds.Contains(rt.UserId) && rt.RevokedAt == null && rt.ExpiresAt > now)
            .GroupBy(rt => rt.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var userDtos = userBusinesses.Select(ub => new UserDto
        {
            Id = ub.User.Id,
            Email = ub.User.Email!,
            FirstName = ub.User.FirstName,
            LastName = ub.User.LastName,
            Role = (int)ub.Role,
            RoleName = ub.Role.ToString(),
            BusinessId = ub.BusinessId,
            BusinessName = ub.Business.Name,
            IsActive = ub.IsActive && ub.User.IsActive,
            CreatedAt = ub.User.CreatedAt,
            LastLoginAt = ub.User.LastLoginAt,
            ActiveSessionCount = activeSessionCounts.GetValueOrDefault(ub.User.Id, 0)
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
        var userBusiness = await _context.UserBusinesses
            .Include(ub => ub.User)
            .Include(ub => ub.Business)
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == businessId);

        if (userBusiness == null)
        {
            return null;
        }

        var activeSessionCount = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == userId && rt.IsActive);

        return new UserDto
        {
            Id = userBusiness.User.Id,
            Email = userBusiness.User.Email!,
            FirstName = userBusiness.User.FirstName,
            LastName = userBusiness.User.LastName,
            Role = (int)userBusiness.Role,
            RoleName = userBusiness.Role.ToString(),
            BusinessId = userBusiness.BusinessId,
            BusinessName = userBusiness.Business.Name,
            IsActive = userBusiness.IsActive && userBusiness.User.IsActive,
            CreatedAt = userBusiness.User.CreatedAt,
            LastLoginAt = userBusiness.User.LastLoginAt,
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
            // User exists, check if they're already linked to this business
            var existingUserBusiness = await _context.UserBusinesses
                .FirstOrDefaultAsync(ub => ub.UserId == existingUser.Id && ub.BusinessId == businessId);

            if (existingUserBusiness != null)
            {
                return (false, "User is already a member of this business", null);
            }

            // Link existing user to this business
            var userBusiness = new UserBusiness
            {
                UserId = existingUser.Id,
                BusinessId = businessId,
                Role = (UserRole)request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserBusinesses.Add(userBusiness);
            await _context.SaveChangesAsync();

            var userDto = await GetUserByIdAsync(existingUser.Id, businessId);
            return (true, null, userDto);
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CurrentBusinessId = businessId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.TemporaryPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        // Create UserBusiness relationship
        var newUserBusiness = new UserBusiness
        {
            UserId = user.Id,
            BusinessId = businessId,
            Role = (UserRole)request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserBusinesses.Add(newUserBusiness);
        await _context.SaveChangesAsync();

        // Send welcome email if requested
        if (request.SendWelcomeEmail)
        {
            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);
        }

        var newUserDto = await GetUserByIdAsync(user.Id, businessId);
        return (true, null, newUserDto);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        int businessId,
        string updatedByUserId)
    {
        var userBusiness = await _context.UserBusinesses
            .Include(ub => ub.User)
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == businessId);

        if (userBusiness == null)
        {
            return (false, "User not found");
        }

        // Prevent self-demotion from admin
        if (userId == updatedByUserId && userBusiness.Role == UserRole.Admin && (UserRole)request.Role != UserRole.Admin)
        {
            return (false, "You cannot remove your own admin privileges");
        }

        // Update user details
        var user = userBusiness.User;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        userBusiness.Role = (UserRole)request.Role;

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

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleUserStatusAsync(
        string userId,
        int businessId,
        string updatedByUserId)
    {
        var userBusiness = await _context.UserBusinesses
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == businessId);

        if (userBusiness == null)
        {
            return (false, "User not found");
        }

        // Prevent self-deactivation
        if (userId == updatedByUserId)
        {
            return (false, "You cannot deactivate your own account");
        }

        // Toggle active status
        userBusiness.IsActive = !userBusiness.IsActive;

        // If deactivating, revoke all refresh tokens for this user
        if (!userBusiness.IsActive)
        {
            var now = DateTime.UtcNow;
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
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
        var userBusiness = await _context.UserBusinesses
            .Include(ub => ub.User)
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == businessId);

        if (userBusiness == null)
        {
            return (false, "User not found");
        }

        var user = userBusiness.User;

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
        var userBusiness = await _context.UserBusinesses
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == businessId);

        if (userBusiness == null)
        {
            return (false, "User not found");
        }

        // Revoke all active refresh tokens
        var now = DateTime.UtcNow;
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
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
        return await _context.UserBusinesses
            .CountAsync(ub => ub.BusinessId == businessId);
    }

    public async Task<Dictionary<string, int>> GetUserStatisticsAsync(int businessId)
    {
        var userBusinesses = await _context.UserBusinesses
            .Include(ub => ub.User)
            .Where(ub => ub.BusinessId == businessId)
            .ToListAsync();

        return new Dictionary<string, int>
        {
            ["Total"] = userBusinesses.Count(),
            ["Active"] = userBusinesses.Count(ub => ub.IsActive && ub.User.IsActive),
            ["Inactive"] = userBusinesses.Count(ub => !ub.IsActive || !ub.User.IsActive),
            ["Admins"] = userBusinesses.Count(ub => ub.Role == UserRole.Admin),
            ["Managers"] = userBusinesses.Count(ub => ub.Role == UserRole.Manager),
            ["Staff"] = userBusinesses.Count(ub => ub.Role == UserRole.Staff),
            ["Viewers"] = userBusinesses.Count(ub => ub.Role == UserRole.Viewer)
        };
    }
}

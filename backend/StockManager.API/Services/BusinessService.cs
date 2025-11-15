using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.API.Services;

public class BusinessService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BusinessService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
        _userManager = userManager;
    }

    public virtual async Task<BusinessDto?> GetBusinessByIdAsync(int businessId)
    {
        var business = await _unitOfWork.Businesses.GetByIdAsync(businessId);
        if (business == null)
            return null;

        return _mapper.Map<BusinessDto>(business);
    }

    public virtual async Task<(bool Success, string? Error)> UpdateBusinessAsync(
        int businessId,
        UpdateBusinessDto updateDto,
        string userId,
        int userRole)
    {
        // Only Admin (role 0) can update business settings
        if (userRole != 0)
        {
            return (false, "Only administrators can update business settings");
        }

        var business = await _unitOfWork.Businesses.GetByIdAsync(businessId);
        if (business == null)
        {
            return (false, "Business not found");
        }

        // Map the update DTO to the business entity
        _mapper.Map(updateDto, business);
        business.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Businesses.UpdateAsync(business);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    // Multi-Business Management
    public virtual async Task<(bool Success, string? Error, int? BusinessId)> CreateBusinessAsync(
        string userId,
        CreateBusinessDto createDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        // Create the business
        var business = new Business
        {
            Name = createDto.Name,
            Description = createDto.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Businesses.AddAsync(business);
        await _unitOfWork.SaveChangesAsync();

        // Create UserBusiness relationship with the creator as Admin
        var userBusiness = new UserBusiness
        {
            UserId = userId,
            BusinessId = business.Id,
            Role = createDto.UserRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserBusinesses.Add(userBusiness);

        // Set as user's current business if they don't have one
        if (!user.CurrentBusinessId.HasValue)
        {
            user.CurrentBusinessId = business.Id;
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();

        return (true, null, business.Id);
    }

    public virtual async Task<List<UserBusinessDto>> GetUserBusinessesAsync(string userId)
    {
        var userBusinesses = await _context.UserBusinesses
            .Include(ub => ub.Business)
            .Where(ub => ub.UserId == userId && ub.IsActive)
            .Select(ub => new UserBusinessDto
            {
                BusinessId = ub.BusinessId,
                BusinessName = ub.Business.Name,
                Role = ub.Role.ToString(),
                IsActive = ub.IsActive
            })
            .ToListAsync();

        return userBusinesses;
    }

    public virtual async Task<(bool Success, string? Error)> SwitchBusinessAsync(
        string userId,
        int businessId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Verify user has access to this business
        var userBusiness = await _context.UserBusinesses
            .FirstOrDefaultAsync(ub =>
                ub.UserId == userId &&
                ub.BusinessId == businessId &&
                ub.IsActive);

        if (userBusiness == null)
        {
            return (false, "You don't have access to this business");
        }

        // Update user's current business
        user.CurrentBusinessId = businessId;
        await _userManager.UpdateAsync(user);

        return (true, null);
    }
}

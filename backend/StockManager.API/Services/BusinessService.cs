using AutoMapper;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class BusinessService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BusinessService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
}

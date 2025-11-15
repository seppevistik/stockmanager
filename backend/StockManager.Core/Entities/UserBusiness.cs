using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class UserBusiness : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public UserRole Role { get; set; } = UserRole.Staff;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Business Business { get; set; } = null!;
}

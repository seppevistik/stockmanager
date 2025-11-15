using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

public class CreateBusinessDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole UserRole { get; set; } = UserRole.Admin; // Creator's role in the business
}

public class SwitchBusinessDto
{
    public int BusinessId { get; set; }
}

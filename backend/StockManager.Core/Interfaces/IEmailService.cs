namespace StockManager.Core.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl);
    Task SendWelcomeEmailAsync(string email, string firstName);
}

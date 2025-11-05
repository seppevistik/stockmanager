using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        // TODO: Replace with actual email sending implementation (SendGrid, SMTP, etc.)
        _logger.LogInformation(
            "Password Reset Email - To: {Email}, Token: {Token}, URL: {Url}",
            email, resetToken, resetUrl);

        // For now, just log the reset URL that would be sent
        _logger.LogInformation(
            "Password reset link: {Url}?token={Token}&email={Email}",
            resetUrl, resetToken, email);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string firstName)
    {
        // TODO: Replace with actual email sending implementation
        _logger.LogInformation(
            "Welcome Email - To: {Email}, Name: {FirstName}",
            email, firstName);

        return Task.CompletedTask;
    }
}

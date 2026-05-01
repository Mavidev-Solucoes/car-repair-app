namespace CarRepairShop.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}

using CarRepairShop.Application.ServiceOrders.Commands.Services;
using CarRepairShop.Application.Settings;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class ServiceOrderNotificationServiceTests
{
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<IOptions<AppSettings>> _appSettingsMock = new();
    private readonly Mock<ILogger<ServiceOrderNotificationService>> _loggerMock = new();

    [Fact]
    public async Task NotifyServiceReceivedAsync_TemplateFails_DoesNotThrow()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2022, "ABC1D23", "White");
        var employee = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);

        _appSettingsMock.Setup(o => o.Value).Returns(new AppSettings { BaseUrl = "https://example.com" });
        _emailTemplateServiceMock.Setup(t => t.RenderServiceReceivedAsync(order, customer, vehicle, employee))
            .ThrowsAsync(new InvalidOperationException("template error"));

        var service = new ServiceOrderNotificationService(
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _appSettingsMock.Object,
            _loggerMock.Object);

        await service.NotifyServiceReceivedAsync(order, customer, vehicle, employee, CancellationToken.None);

        _emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyApprovalRequestedAsync_UsesTrimmedBaseUrl()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        var expectedApprovalUrl = $"https://example.com/api/services/{order.Id}/approve";
        var expectedRejectionUrl = $"https://example.com/api/services/{order.Id}/reject";

        _appSettingsMock.Setup(o => o.Value).Returns(new AppSettings { BaseUrl = "https://example.com/" });
        _emailTemplateServiceMock.Setup(t => t.RenderWaitingForApprovalAsync(order, customer, expectedApprovalUrl, expectedRejectionUrl))
            .ReturnsAsync("<html>approval</html>");

        var service = new ServiceOrderNotificationService(
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _appSettingsMock.Object,
            _loggerMock.Object);

        await service.NotifyApprovalRequestedAsync(order, customer, CancellationToken.None);

        _emailServiceMock.Verify(e => e.SendAsync(customer.Email, customer.Name, "Your service requires your approval", "<html>approval</html>", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyApprovalRequestedAsync_TemplateFails_DoesNotThrow()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");

        _appSettingsMock.Setup(o => o.Value).Returns(new AppSettings { BaseUrl = "https://example.com" });
        _emailTemplateServiceMock.Setup(t => t.RenderWaitingForApprovalAsync(It.IsAny<ServiceOrder>(), customer, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("template error"));

        var service = new ServiceOrderNotificationService(
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _appSettingsMock.Object,
            _loggerMock.Object);

        await service.NotifyApprovalRequestedAsync(order, customer, CancellationToken.None);

        _emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

using CarRepairShop.Domain.Settings;
using CarRepairShop.Services.Implementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CarRepairShop.UnitTests.Services;

public class EmailServiceTests
{
    private static SmtpSettings ValidSettings() => new()
    {
        Host = "smtp.example.com",
        Port = 465,
        UseSsl = true,
        Username = "user@example.com",
        Password = "secret",
        FromEmail = "noreply@example.com",
        FromName = "Car Repair Shop"
    };

    private static IOptions<SmtpSettings> ToOptions(SmtpSettings settings) =>
        Options.Create(settings);

    private static ILogger<EmailService> CreateLogger() =>
        new Mock<ILogger<EmailService>>().Object;

    [Fact]
    public void Constructor_WithValidSettings_DoesNotThrow()
    {
        var ex = Record.Exception(() => new EmailService(ToOptions(ValidSettings()), CreateLogger()));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingHost_ThrowsInvalidOperationException(string host)
    {
        var settings = ValidSettings();
        settings.Host = host;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(ToOptions(settings), CreateLogger()));

        Assert.Contains("Host", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingFromEmail_ThrowsInvalidOperationException(string fromEmail)
    {
        var settings = ValidSettings();
        settings.FromEmail = fromEmail;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(ToOptions(settings), CreateLogger()));

        Assert.Contains("FromEmail", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingUsername_ThrowsInvalidOperationException(string username)
    {
        var settings = ValidSettings();
        settings.Username = username;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(ToOptions(settings), CreateLogger()));

        Assert.Contains("Username", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingPassword_ThrowsInvalidOperationException(string password)
    {
        var settings = ValidSettings();
        settings.Password = password;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(ToOptions(settings), CreateLogger()));

        Assert.Contains("Password", ex.Message);
    }
}

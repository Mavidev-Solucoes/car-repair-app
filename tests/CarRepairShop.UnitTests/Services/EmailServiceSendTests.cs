using CarRepairShop.Domain.Settings;
using CarRepairShop.Services.Implementations;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace CarRepairShop.UnitTests.Services;

public class EmailServiceSendTests
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

    private static EmailService CreateService(ISmtpClient client, SmtpSettings? settings = null)
    {
        settings ??= ValidSettings();
        return new EmailService(ToOptions(settings), CreateLogger(), () => client);
    }

    [Fact]
    public async Task SendAsync_PlainText_ConnectsAuthenticatesAndSendsMessage()
    {
        var clientMock = new Mock<ISmtpClient>();
        var service = CreateService(clientMock.Object);

        await service.SendAsync("to@example.com", "Recipient", "Hello", "body text");

        clientMock.Verify(c => c.ConnectAsync(
            "smtp.example.com", 465, SecureSocketOptions.SslOnConnect,
            It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.AuthenticateAsync(
            "user@example.com", "secret",
            It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.SendAsync(
            It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress?>()),
            Times.Once);
        clientMock.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_HtmlBody_SetsHtmlBody()
    {
        var clientMock = new Mock<ISmtpClient>();
        MimeMessage? capturedMessage = null;
        clientMock
            .Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress?>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress?>((msg, _, _) => capturedMessage = msg)
            .ReturnsAsync(string.Empty);

        var service = CreateService(clientMock.Object);
        await service.SendAsync("to@example.com", "Recipient", "Subject", "<b>html</b>", isHtml: true);

        Assert.NotNull(capturedMessage);
        var htmlBody = capturedMessage.HtmlBody;
        Assert.NotNull(htmlBody);
        Assert.Contains("<b>html</b>", htmlBody);
    }

    [Fact]
    public async Task SendAsync_WithoutSsl_UsesNoneSocketOptions()
    {
        var settings = ValidSettings();
        settings.UseSsl = false;
        var clientMock = new Mock<ISmtpClient>();
        var service = CreateService(clientMock.Object, settings);

        await service.SendAsync("to@example.com", "Recipient", "Subject", "body");

        clientMock.Verify(c => c.ConnectAsync(
            It.IsAny<string>(), It.IsAny<int>(), SecureSocketOptions.None,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenSendThrows_StillDisconnects()
    {
        var clientMock = new Mock<ISmtpClient>();
        clientMock
            .Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress?>()))
            .ThrowsAsync(new InvalidOperationException("send failed"));

        var service = CreateService(clientMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("to@example.com", "Recipient", "Subject", "body"));

        clientMock.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_SetsCorrectRecipients()
    {
        var clientMock = new Mock<ISmtpClient>();
        MimeMessage? capturedMessage = null;
        clientMock
            .Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress?>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress?>((msg, _, _) => capturedMessage = msg)
            .ReturnsAsync(string.Empty);

        var service = CreateService(clientMock.Object);
        await service.SendAsync("to@example.com", "Recipient Name", "Subject", "body");

        Assert.NotNull(capturedMessage);
        Assert.Equal("noreply@example.com", ((MailboxAddress)capturedMessage.From[0]).Address);
        Assert.Equal("Car Repair Shop", ((MailboxAddress)capturedMessage.From[0]).Name);
        Assert.Equal("to@example.com", ((MailboxAddress)capturedMessage.To[0]).Address);
        Assert.Equal("Recipient Name", ((MailboxAddress)capturedMessage.To[0]).Name);
        Assert.Equal("Subject", capturedMessage.Subject);
    }
}

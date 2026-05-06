using System.Net;
using System.Net.Http.Json;
using CarRepairShop.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace CarRepairShop.UnitTests.Services.API;

public class UiApiClientTests
{
    private static (UiApiClient client, Mock<HttpMessageHandler> handler) CreateClient(
        HttpResponseMessage response,
        string? accessToken = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");

        // Always need RequestServices for GetTokenAsync
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.AuthenticateAsync(
                It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(accessToken is not null
                ? AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new System.Security.Claims.ClaimsPrincipal(),
                        new AuthenticationProperties(
                            new Dictionary<string, string?> { [".Token.access_token"] = accessToken }),
                        CookieAuthenticationDefaults.AuthenticationScheme))
                : AuthenticateResult.NoResult());

        var sp = new ServiceCollection();
        sp.AddSingleton(authServiceMock.Object);
        httpContext.RequestServices = sp.BuildServiceProvider();

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var client = new UiApiClient(httpClientFactoryMock.Object, httpContextAccessorMock.Object);
        return (client, handlerMock);
    }

    [Fact]
    public async Task GetAsync_Success_ReturnsParsedResponse()
    {
        var expected = new { Name = "Alice" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        };
        var (client, _) = CreateClient(response);

        var result = await client.GetAsync<dynamic>("/api/test", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAsync_NotFound_ThrowsUiApiException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"title\":\"Not found.\",\"detail\":\"Resource not found.\"}")
        };
        var (client, _) = CreateClient(response);

        var ex = await Assert.ThrowsAsync<UiApiException>(
            () => client.GetAsync<object>("/api/test", CancellationToken.None));

        Assert.Equal(404, ex.StatusCode);
        Assert.Contains("Resource not found.", ex.Message);
    }

    [Fact]
    public async Task GetAsync_ServerError_WithErrors_ThrowsWithErrors()
    {
        var content = new
        {
            title = "Validation failed.",
            errors = new Dictionary<string, string[]>
            {
                { "Name", new[] { "Name is required." } }
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(content)
        };
        var (client, _) = CreateClient(response);

        var ex = await Assert.ThrowsAsync<UiApiException>(
            () => client.GetAsync<object>("/api/test", CancellationToken.None));

        Assert.Equal(422, ex.StatusCode);
        Assert.NotNull(ex.Errors);
    }

    [Fact]
    public async Task GetAsync_ServerError_MalformedJson_ThrowsWithStatusText()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("not json")
        };
        var (client, _) = CreateClient(response);

        var ex = await Assert.ThrowsAsync<UiApiException>(
            () => client.GetAsync<object>("/api/test", CancellationToken.None));

        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task PostAsync_WithResponse_ReturnsResult()
    {
        var result = new { Id = Guid.NewGuid() };
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(result)
        };
        var (client, _) = CreateClient(response);

        var returned = await client.PostAsync<object, dynamic>("/api/test", new { Name = "Alice" }, CancellationToken.None);

        Assert.NotNull(returned);
    }

    [Fact]
    public async Task PostAsync_WithoutResponse_Succeeds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        var (client, _) = CreateClient(response);

        var ex = await Record.ExceptionAsync(() =>
            client.PostAsync("/api/test", new { Name = "Alice" }, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PatchAsync_NoBody_Succeeds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        var (client, _) = CreateClient(response);

        var ex = await Record.ExceptionAsync(() =>
            client.PatchAsync("/api/test/id/action", CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PatchAsync_WithBody_Succeeds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        var (client, _) = CreateClient(response);

        var ex = await Record.ExceptionAsync(() =>
            client.PatchAsync("/api/test/id", new { Value = "new" }, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PatchAsync_WithResponse_ReturnsResult()
    {
        var result = new { Status = "Approved" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(result)
        };
        var (client, _) = CreateClient(response);

        var returned = await client.PatchAsync<dynamic>("/api/test/id/approve", CancellationToken.None);

        Assert.NotNull(returned);
    }

    [Fact]
    public async Task PutAsync_ReturnsResult()
    {
        var result = new { Name = "Updated" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(result)
        };
        var (client, _) = CreateClient(response);

        var returned = await client.PutAsync<object, dynamic>("/api/test/id", new { Name = "Updated" }, CancellationToken.None);

        Assert.NotNull(returned);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        var (client, _) = CreateClient(response);

        var ex = await Record.ExceptionAsync(() =>
            client.DeleteAsync("/api/test/id", CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task GetAsync_EmptyResponse_ThrowsUiApiException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };
        var (client, _) = CreateClient(response);

        var ex = await Assert.ThrowsAsync<UiApiException>(
            () => client.GetAsync<object>("/api/test", CancellationToken.None));

        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("empty response", ex.Message);
    }

    [Fact]
    public async Task HttpContextAccessor_NullContext_ThrowsInvalidOperation()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var client = new UiApiClient(httpClientFactoryMock.Object, httpContextAccessorMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync<object>("/api/test", CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WithAccessToken_AddsAuthorizationHeader()
    {
        var result = new { Name = "Test" };
        var capturedRequest = (HttpRequestMessage?)null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(result)
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");

        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.AuthenticateAsync(
                It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(
                new AuthenticationTicket(
                    new System.Security.Claims.ClaimsPrincipal(),
                    new AuthenticationProperties(
                        new Dictionary<string, string?> { [".Token.access_token"] = "my-token" }),
                    CookieAuthenticationDefaults.AuthenticationScheme)));

        var sp = new ServiceCollection();
        sp.AddSingleton(authServiceMock.Object);
        httpContext.RequestServices = sp.BuildServiceProvider();

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var client = new UiApiClient(httpClientFactoryMock.Object, httpContextAccessorMock.Object);
        await client.GetAsync<dynamic>("/api/test", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", capturedRequest.Headers.Authorization?.Parameter);
    }
}

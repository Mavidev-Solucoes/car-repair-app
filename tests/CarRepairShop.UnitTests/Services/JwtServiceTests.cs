using System.Net;
using System.Text;
using CarRepairShop.Services.Implementations;
using Microsoft.Extensions.Configuration;

namespace CarRepairShop.UnitTests.Services;

public class JwtServiceTests
{
    [Fact]
    public async Task GenerateTokenAsync_ReturnsAccessToken()
    {
        var service = CreateService(
            """
            { "accessToken": "token-from-lambda" }
            """,
            HttpStatusCode.OK);

        var token = await service.GenerateTokenAsync("123.456.789-09");

        Assert.Equal("token-from-lambda", token);
    }

    [Fact]
    public async Task GenerateTokenAsync_NormalizesCpfBeforeSending()
    {
        string? capturedBody = null;
        var service = CreateService(
            """
            { "accessToken": "token-from-lambda" }
            """,
            HttpStatusCode.OK,
            content => capturedBody = content);

        await service.GenerateTokenAsync("123.456.789-09");

        Assert.Equal("{\"cpf\":\"12345678909\"}", capturedBody);
    }

    [Fact]
    public async Task GenerateTokenAsync_ThrowsWhenLambdaReturnsError()
    {
        var service = CreateService("{}", HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateTokenAsync("12345678909"));
    }

    [Fact]
    public async Task GenerateTokenAsync_ThrowsWhenAccessTokenMissing()
    {
        var service = CreateService("{\"foo\":\"bar\"}", HttpStatusCode.OK);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateTokenAsync("12345678909"));
    }

    [Fact]
    public async Task GenerateTokenAsync_ThrowsWhenBaseUrlNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new JwtService(config, new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateTokenAsync("12345678909"));
    }

    private static JwtService CreateService(
        string responseBody,
        HttpStatusCode statusCode,
        Action<string>? onRequestBody = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthLambda:BaseUrl"] = "https://lambda.example",
                ["AuthLambda:TokenPath"] = "/auth/token"
            })
            .Build();

        var httpClient = new HttpClient(new FakeHttpMessageHandler(request =>
        {
            var body = request.Content is null
                ? string.Empty
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            onRequestBody?.Invoke(body);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }));

        return new JwtService(config, httpClient);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}

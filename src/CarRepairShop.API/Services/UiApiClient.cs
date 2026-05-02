using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace CarRepairShop.API.Services;

public interface IUiApiClient
{
    Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken);
    Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken);
    Task PostAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken);
    Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string path, CancellationToken cancellationToken);
    Task<TResponse> PatchAsync<TResponse>(string path, CancellationToken cancellationToken);
    Task PatchAsync(string path, CancellationToken cancellationToken);
    Task PatchAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken);
}

public class UiApiClient : IUiApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UiApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(HttpMethod.Get, path, body: null, cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(HttpMethod.Post, path, request, cancellationToken);

    public Task PostAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, request, cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(HttpMethod.Put, path, request, cancellationToken);

    public Task DeleteAsync(string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, path, body: null, cancellationToken);

    public Task<TResponse> PatchAsync<TResponse>(string path, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(HttpMethod.Patch, path, body: null, cancellationToken);

    public Task PatchAsync(string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Patch, path, body: null, cancellationToken);

    public Task PatchAsync<TRequest>(string path, TRequest request, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Patch, path, request, cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        if (result is null)
            throw new UiApiException("The API returned an empty response.", statusCode: 500);

        return result;
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{context.Request.Scheme}://{context.Request.Host}");

        var token = await context.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await client.SendAsync(request, cancellationToken);
        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        ApiErrorResponse? apiError = null;
        try
        {
            apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
        }
        catch
        {
            // Ignore malformed payloads and fall back to status text.
        }

        throw new UiApiException(
            apiError?.Detail ?? apiError?.Title ?? $"The API request failed with status code {(int)response.StatusCode}.",
            (int)response.StatusCode,
            apiError?.Errors);
    }

    private sealed class ApiErrorResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public IDictionary<string, string[]>? Errors { get; set; }
    }
}

public class UiApiException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public UiApiException(string message, int statusCode, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

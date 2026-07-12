using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var parseResult = AppOptions.Parse(args);

if (parseResult.ShowHelp)
{
    AppOptions.PrintUsage();
    return 0;
}

if (!parseResult.IsValid || parseResult.Options is null)
{
    if (!string.IsNullOrWhiteSpace(parseResult.ErrorMessage))
    {
        Console.Error.WriteLine(parseResult.ErrorMessage);
        Console.Error.WriteLine();
    }

    AppOptions.PrintUsage();
    return 1;
}

try
{
    var options = parseResult.Options;
    using var cancellationTokenSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
        Console.WriteLine("Interrupção solicitada. Finalizando o teste e consolidando métricas...");
    };

    var handler = new SocketsHttpHandler
    {
        MaxConnectionsPerServer = Math.Max(100, options.Concurrency * 2),
        AutomaticDecompression = DecompressionMethods.All
    };

    using var httpClient = new HttpClient(handler)
    {
        BaseAddress = options.BaseUri,
        Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
    };

    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (!options.SkipAuthentication)
    {
        var token = string.IsNullOrWhiteSpace(options.Token)
            ? await AuthenticateAsync(httpClient, options, cancellationTokenSource.Token)
            : options.Token;

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    Console.WriteLine("Configuração do teste:");
    Console.WriteLine($"- Base URL: {options.BaseUri}");
    Console.WriteLine($"- Endpoint GET: {options.Endpoint}");
    Console.WriteLine($"- Concorrência: {options.Concurrency}");
    Console.WriteLine($"- Duração: {options.DurationSeconds}s");
    Console.WriteLine($"- Timeout por requisição: {options.TimeoutSeconds}s");
    Console.WriteLine($"- Autenticação: {(options.SkipAuthentication ? "desabilitada" : "JWT")}");
    Console.WriteLine();

    var metrics = new LoadTestMetrics();
    await RunWarmupAsync(httpClient, options, cancellationTokenSource.Token);

    Console.WriteLine("Iniciando o stress test...");

    var startedAt = Stopwatch.GetTimestamp();
    var finishesAt = DateTimeOffset.UtcNow.AddSeconds(options.DurationSeconds);
    var workers = Enumerable.Range(1, options.Concurrency)
        .Select(_ => ExecuteWorkerAsync(httpClient, options, finishesAt, metrics, cancellationTokenSource.Token));

    await Task.WhenAll(workers);

    Console.WriteLine();
    metrics.PrintSummary(Stopwatch.GetElapsedTime(startedAt));
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Erro ao executar o teste: {exception.Message}");
    return 1;
}

static async Task<string> AuthenticateAsync(HttpClient httpClient, AppOptions options, CancellationToken cancellationToken)
{
    var loginPayload = JsonSerializer.Serialize(new
    {
        email = options.Email,
        password = options.Password
    });

    using var response = await httpClient.PostAsync(
        options.LoginPath,
        new StringContent(loginPayload, Encoding.UTF8, "application/json"),
        cancellationToken);

    var content = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Falha ao autenticar em '{options.LoginPath}'. Status {(int)response.StatusCode} ({response.StatusCode}). Resposta: {content}");
    }

    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, JsonOptions.Serializer);

    if (string.IsNullOrWhiteSpace(loginResponse?.Token))
    {
        throw new InvalidOperationException("A autenticação foi concluída, mas a resposta não retornou um token JWT válido.");
    }

    return loginResponse.Token;
}

static async Task RunWarmupAsync(HttpClient httpClient, AppOptions options, CancellationToken cancellationToken)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, options.Endpoint);
    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Warm-up falhou para '{options.Endpoint}'. Status {(int)response.StatusCode} ({response.StatusCode}).");
    }

    Console.WriteLine($"Warm-up: {(int)response.StatusCode} ({response.StatusCode}) - {bytes.Length} bytes");
    Console.WriteLine();
}

static async Task ExecuteWorkerAsync(
    HttpClient httpClient,
    AppOptions options,
    DateTimeOffset finishesAt,
    LoadTestMetrics metrics,
    CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < finishesAt)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, options.Endpoint);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            metrics.Record(response.StatusCode, Stopwatch.GetElapsedTime(startedAt), payload.LongLength);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            break;
        }
        catch (Exception exception)
        {
            metrics.RecordFailure(Stopwatch.GetElapsedTime(startedAt), exception);
        }
    }
}

internal sealed record LoginResponse(string? Token);

internal sealed class LoadTestMetrics
{
    private readonly ConcurrentBag<double> _latenciesInMilliseconds = [];
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _transportErrors;
    private long _receivedBytes;

    public void Record(HttpStatusCode statusCode, TimeSpan elapsed, long receivedBytes)
    {
        var numericStatusCode = (int)statusCode;

        Interlocked.Increment(ref _totalRequests);
        _latenciesInMilliseconds.Add(elapsed.TotalMilliseconds);
        Interlocked.Add(ref _receivedBytes, receivedBytes);
        _statusCodes.AddOrUpdate(numericStatusCode, 1, (_, current) => current + 1);

        if (numericStatusCode >= 200 && numericStatusCode <= 299)
        {
            Interlocked.Increment(ref _successfulRequests);
            return;
        }

        Interlocked.Increment(ref _failedRequests);
    }

    public void RecordFailure(TimeSpan elapsed, Exception exception)
    {
        _ = exception;
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _failedRequests);
        Interlocked.Increment(ref _transportErrors);
        _latenciesInMilliseconds.Add(elapsed.TotalMilliseconds);
    }

    public void PrintSummary(TimeSpan totalDuration)
    {
        var latencies = _latenciesInMilliseconds.ToArray();
        Array.Sort(latencies);

        Console.WriteLine("Resumo:");
        Console.WriteLine($"- Duração total: {totalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"- Total de requisições: {_totalRequests}");
        Console.WriteLine($"- Sucesso (2xx): {_successfulRequests}");
        Console.WriteLine($"- Falhas: {_failedRequests}");
        Console.WriteLine($"- Erros de transporte: {_transportErrors}");
        Console.WriteLine($"- Throughput médio: {(totalDuration.TotalSeconds > 0 ? _totalRequests / totalDuration.TotalSeconds : 0):F2} req/s");
        Console.WriteLine($"- Dados recebidos: {_receivedBytes / 1024d / 1024d:F2} MiB");

        if (latencies.Length == 0)
        {
            Console.WriteLine("- Latência: sem amostras");
        }
        else
        {
            Console.WriteLine($"- Latência média: {latencies.Average():F2} ms");
            Console.WriteLine($"- Latência mínima: {latencies[0]:F2} ms");
            Console.WriteLine($"- Latência p95: {GetPercentile(latencies, 95):F2} ms");
            Console.WriteLine($"- Latência p99: {GetPercentile(latencies, 99):F2} ms");
            Console.WriteLine($"- Latência máxima: {latencies[^1]:F2} ms");
        }

        Console.WriteLine("- Status codes:");

        if (_statusCodes.IsEmpty && _transportErrors == 0)
        {
            Console.WriteLine("  - nenhum");
            return;
        }

        foreach (var statusCode in _statusCodes.OrderBy(item => item.Key))
        {
            Console.WriteLine($"  - {statusCode.Key}: {statusCode.Value}");
        }

        if (_transportErrors > 0)
        {
            Console.WriteLine($"  - transport-error: {_transportErrors}");
        }
    }

    private static double GetPercentile(double[] orderedValues, int percentile)
    {
        if (orderedValues.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile / 100d * orderedValues.Length) - 1;
        return orderedValues[Math.Clamp(index, 0, orderedValues.Length - 1)];
    }
}

internal sealed class AppOptions
{
    public required Uri BaseUri { get; init; }
    public required string Endpoint { get; init; }
    public required string LoginPath { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Token { get; init; }
    public required int Concurrency { get; init; }
    public required int DurationSeconds { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required bool SkipAuthentication { get; init; }

    public static ParseResult Parse(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)))
        {
            return ParseResult.Help();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                return ParseResult.Fail($"Argumento inválido: '{current}'. Use --help para ver os parâmetros suportados.");
            }

            if (string.Equals(current, "--skip-auth", StringComparison.OrdinalIgnoreCase))
            {
                flags.Add(current);
                continue;
            }

            if (index == args.Length - 1)
            {
                return ParseResult.Fail($"Valor ausente para o argumento '{current}'.");
            }

            values[current] = args[++index];
        }

        var baseUrl = GetValue(values, "--base-url", "CAR_REPAIR_SHOP_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(AppendTrailingSlash(baseUrl), UriKind.Absolute, out var baseUri))
        {
            return ParseResult.Fail("Informe uma URL base válida via --base-url ou CAR_REPAIR_SHOP_BASE_URL.");
        }

        var endpoint = GetValue(values, "--endpoint", "CAR_REPAIR_SHOP_ENDPOINT") ?? "/api/customers?pageNumber=1&pageSize=50";
        var loginPath = GetValue(values, "--login-path", "CAR_REPAIR_SHOP_LOGIN_PATH") ?? "/api/auth/login";
        var skipAuthentication = flags.Contains("--skip-auth");
        var email = GetValue(values, "--email", "CAR_REPAIR_SHOP_EMAIL");
        var password = GetValue(values, "--password", "CAR_REPAIR_SHOP_PASSWORD");
        var token = GetValue(values, "--token", "CAR_REPAIR_SHOP_TOKEN");

        if (!TryParsePositiveInt(GetValue(values, "--concurrency", "CAR_REPAIR_SHOP_CONCURRENCY"), 40, out var concurrency))
        {
            return ParseResult.Fail("O valor de --concurrency deve ser um inteiro maior que zero.");
        }

        if (!TryParsePositiveInt(GetValue(values, "--duration", "CAR_REPAIR_SHOP_DURATION_SECONDS"), 120, out var durationSeconds))
        {
            return ParseResult.Fail("O valor de --duration deve ser um inteiro maior que zero.");
        }

        if (!TryParsePositiveInt(GetValue(values, "--timeout", "CAR_REPAIR_SHOP_TIMEOUT_SECONDS"), 30, out var timeoutSeconds))
        {
            return ParseResult.Fail("O valor de --timeout deve ser um inteiro maior que zero.");
        }

        if (!skipAuthentication && string.IsNullOrWhiteSpace(token) &&
            (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)))
        {
            return ParseResult.Fail("Informe --email e --password, ou então use --token/--skip-auth.");
        }

        return ParseResult.Success(new AppOptions
        {
            BaseUri = baseUri,
            Endpoint = endpoint,
            LoginPath = loginPath,
            Email = email,
            Password = password,
            Token = token,
            Concurrency = concurrency,
            DurationSeconds = durationSeconds,
            TimeoutSeconds = timeoutSeconds,
            SkipAuthentication = skipAuthentication
        });
    }

    public static void PrintUsage()
    {
        Console.WriteLine(
            """
            Uso:
              dotnet run --project tools/CarRepairShop.PerformanceTester -- \
                --base-url http://localhost:8080 \
                --endpoint /api/customers?pageNumber=1&pageSize=50 \
                --email admin@carrepairshop.com \
                --password Admin@123 \
                --concurrency 60 \
                --duration 180

            Parâmetros:
              --base-url     URL base da API (ou CAR_REPAIR_SHOP_BASE_URL)
              --endpoint     Endpoint GET relativo ou absoluto (padrão: /api/customers?pageNumber=1&pageSize=50)
              --login-path   Endpoint de autenticação (padrão: /api/auth/login)
              --email        Usuário para login JWT
              --password     Senha para login JWT
              --token        JWT pronto para reutilizar sem autenticar
              --concurrency  Número de workers concorrentes (padrão: 40)
              --duration     Duração do teste em segundos (padrão: 120)
              --timeout      Timeout por requisição em segundos (padrão: 30)
              --skip-auth    Não autentica e envia as requisições sem Authorization
              --help         Exibe esta ajuda
            """);
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key, string environmentVariable)
        => values.TryGetValue(key, out var value) ? value : Environment.GetEnvironmentVariable(environmentVariable);

    private static bool TryParsePositiveInt(string? rawValue, int defaultValue, out int parsedValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsedValue = defaultValue;
            return true;
        }

        if (int.TryParse(rawValue, out parsedValue) && parsedValue > 0)
        {
            return true;
        }

        parsedValue = 0;
        return false;
    }

    private static string AppendTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
}

internal sealed class ParseResult
{
    public AppOptions? Options { get; private init; }
    public string? ErrorMessage { get; private init; }
    public bool ShowHelp { get; private init; }
    public bool IsValid => !ShowHelp && ErrorMessage is null && Options is not null;

    public static ParseResult Success(AppOptions options) => new() { Options = options };

    public static ParseResult Fail(string errorMessage) => new() { ErrorMessage = errorMessage };

    public static ParseResult Help() => new() { ShowHelp = true };
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Serializer = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

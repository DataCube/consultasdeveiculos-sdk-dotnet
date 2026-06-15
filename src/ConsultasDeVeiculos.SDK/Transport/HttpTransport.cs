using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;

namespace ConsultasDeVeiculos.SDK.Transport;

/// <summary>
/// Transporte HTTP - executa requisições reais à API
/// </summary>
public class HttpTransport : TransportBase
{
    private readonly HttpClient _httpClient;
    private readonly bool _compression;

    public HttpTransport(TransportOptions options) : base(options)
    {
        _compression = options.Compression;

        var handler = new HttpClientHandler();
        if (_compression)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(Timeout)
        };
    }

    public override async Task<SDKResponse> RequestAsync(
        EndpointDefinition endpoint,
        Dictionary<string, object?>? parameters = null,
        RequestOptions? options = null)
    {
        var url = BuildUrl(endpoint, parameters);
        var headers = BuildHeaders(endpoint, parameters);
        var body = BuildBody(endpoint, parameters);
        var method = new HttpMethod(endpoint.Method.ToUpperInvariant());

        // Adiciona auth_token no body (padrão da API)
        if (!string.IsNullOrEmpty(Token) && body != null)
        {
            body["auth_token"] = Token;
            // Remove placeholder do template se existir
            if (body.ContainsKey("auth_token") && body["auth_token"]?.ToString() == "{{api_token}}")
            {
                body["auth_token"] = Token;
            }
        }
        else if (!string.IsNullOrEmpty(Token) && body == null)
        {
            body = new Dictionary<string, object?> { ["auth_token"] = Token };
        }

        return await ExecuteWithRetryAsync(url, method, headers, body, options);
    }

    private async Task<SDKResponse> ExecuteWithRetryAsync(
        string url,
        HttpMethod method,
        Dictionary<string, string> headers,
        Dictionary<string, object?>? body,
        RequestOptions? options)
    {
        var maxRetries = options?.MaxRetries ?? MaxRetries;
        Exception? lastError = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);

                // Adiciona headers
                foreach (var (key, value) in headers)
                {
                    if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                    request.Headers.TryAddWithoutValidation(key, value);
                }

                if (_compression)
                {
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                }

                // Adiciona body para métodos que suportam
                if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put ||
                    method == HttpMethod.Patch || method == HttpMethod.Delete))
                {
                    var jsonBody = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options?.Timeout ?? Timeout));
                var response = await _httpClient.SendAsync(request, cts.Token);

                return await HandleResponseAsync(response);
            }
            catch (AuthenticationException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (RateLimitException rle) when (rle.RetryAfter.HasValue)
            {
                await Task.Delay(rle.RetryAfter.Value * 1000);
                continue;
            }
            catch (Exception ex)
            {
                lastError = ex;

                if (attempt == maxRetries) break;

                // Exponential backoff
                var delay = RetryDelay * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }

        throw lastError ?? new SDKException("Falha na requisição após múltiplas tentativas");
    }

    private async Task<SDKResponse> HandleResponseAsync(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        var responseBody = await response.Content.ReadAsStringAsync();

        object? data;
        if (contentType.Contains("application/json"))
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(responseBody);
            }
            catch
            {
                data = responseBody;
            }
        }
        else
        {
            data = responseBody;
        }

        if (response.IsSuccessStatusCode)
        {
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            return new SDKResponse
            {
                Success = true,
                Status = (int)response.StatusCode,
                Data = data,
                Headers = responseHeaders
            };
        }

        // Erros específicos
        var message = TryExtractMessage(data) ?? $"Erro HTTP {(int)response.StatusCode}";

        switch ((int)response.StatusCode)
        {
            case 401:
            case 403:
                throw new AuthenticationException(message,
                    new Dictionary<string, object?> { ["status"] = (int)response.StatusCode, ["data"] = data });

            case 400:
            case 422:
                throw new ValidationException(message,
                    new Dictionary<string, object?> { ["status"] = (int)response.StatusCode, ["data"] = data });

            case 429:
                var retryAfter = 60;
                if (response.Headers.TryGetValues("Retry-After", out var values))
                {
                    int.TryParse(values.FirstOrDefault(), out retryAfter);
                }
                throw new RateLimitException(message,
                    new Dictionary<string, object?> { ["status"] = 429, ["retryAfter"] = retryAfter, ["data"] = data });

            case 404:
                throw new SDKException(message, "NOT_FOUND",
                    new Dictionary<string, object?> { ["status"] = 404, ["data"] = data });

            case 500:
            case 502:
            case 503:
            case 504:
                throw new SDKException(message, "SERVER_ERROR",
                    new Dictionary<string, object?> { ["status"] = (int)response.StatusCode, ["data"] = data });

            default:
                throw new SDKException(message, "HTTP_ERROR",
                    new Dictionary<string, object?> { ["status"] = (int)response.StatusCode, ["data"] = data });
        }
    }

    private static string? TryExtractMessage(object? data)
    {
        if (data is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            if (json.TryGetProperty("message", out var msgProp))
                return msgProp.GetString();
        }
        return null;
    }
}

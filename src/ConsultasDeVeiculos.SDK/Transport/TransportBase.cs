using System.Web;

namespace ConsultasDeVeiculos.SDK.Transport;

/// <summary>
/// Interface base para transporte de requisições
/// </summary>
public abstract class TransportBase
{
    protected string? Token { get; }
    protected string? BaseUrl { get; }
    protected int Timeout { get; }
    protected int MaxRetries { get; }
    protected int RetryDelay { get; }
    protected Dictionary<string, string> DefaultHeaders { get; }

    protected TransportBase(TransportOptions options)
    {
        Token = options.Token;
        BaseUrl = options.BaseUrl;
        Timeout = options.Timeout > 0 ? options.Timeout : 30000;
        MaxRetries = options.MaxRetries >= 0 ? options.MaxRetries : 3;
        RetryDelay = options.RetryDelay > 0 ? options.RetryDelay : 1000;
        DefaultHeaders = options.Headers ?? new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["Content-Type"] = "application/json"
        };
    }

    /// <summary>
    /// Executa uma requisição
    /// </summary>
    public abstract Task<Core.SDKResponse> RequestAsync(
        Core.EndpointDefinition endpoint,
        Dictionary<string, object?>? parameters = null,
        RequestOptions? options = null);

    /// <summary>
    /// Constrói a URL final com path parameters
    /// </summary>
    protected string BuildUrl(Core.EndpointDefinition endpoint, Dictionary<string, object?>? parameters)
    {
        var url = endpoint.Url;

        if (parameters != null)
        {
            // Substitui path parameters: {{param}} ou :param
            foreach (var (key, value) in parameters)
            {
                if (value == null) continue;
                var strValue = Uri.EscapeDataString(value.ToString()!);
                url = url.Replace($"{{{{{key}}}}}", strValue);
                url = System.Text.RegularExpressions.Regex.Replace(url, $":{key}(?=/|$)", strValue);
            }
        }

        // Aplica baseUrl se a URL não for absoluta
        if (!string.IsNullOrEmpty(BaseUrl) && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
        }

        return url;
    }

    /// <summary>
    /// Mescla headers
    /// </summary>
    protected Dictionary<string, string> BuildHeaders(Core.EndpointDefinition endpoint, Dictionary<string, object?>? parameters)
    {
        var headers = new Dictionary<string, string>(DefaultHeaders);

        foreach (var (key, value) in endpoint.Headers)
        {
            headers[key] = value;
        }

        return headers;
    }

    /// <summary>
    /// Constrói o body da requisição
    /// </summary>
    protected Dictionary<string, object?>? BuildBody(Core.EndpointDefinition endpoint, Dictionary<string, object?>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return endpoint.Body;
        }

        // Se tem template de body no endpoint, faz merge
        if (endpoint.Body != null)
        {
            var merged = new Dictionary<string, object?>(endpoint.Body);
            foreach (var (key, value) in parameters)
            {
                merged[key] = value;
            }
            return merged;
        }

        return parameters;
    }
}

/// <summary>
/// Opções de transporte
/// </summary>
public class TransportOptions
{
    public string? Token { get; set; }
    public string? BaseUrl { get; set; }
    public int Timeout { get; set; } = 30000;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
    public bool Compression { get; set; } = true;
    public Dictionary<string, string>? Headers { get; set; }
    public int SandboxDelay { get; set; } = 100;
    public bool SandboxRandomErrors { get; set; }
    public double SandboxErrorRate { get; set; } = 0.1;
}

/// <summary>
/// Opções adicionais de requisição
/// </summary>
public class RequestOptions
{
    public int? Timeout { get; set; }
    public int? MaxRetries { get; set; }
}

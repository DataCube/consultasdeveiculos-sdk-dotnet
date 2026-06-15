using System.Text.Json;

namespace ConsultasDeVeiculos.SDK.Errors;

/// <summary>
/// Classe base para todos os erros da SDK
/// </summary>
public class SDKException : Exception
{
    public string Code { get; }
    public Dictionary<string, object?>? Details { get; }
    public string Timestamp { get; }

    public SDKException(string message, string code = "SDK_ERROR", Dictionary<string, object?>? details = null)
        : base(message)
    {
        Code = code;
        Details = SanitizeDetails(details);
        Timestamp = DateTime.UtcNow.ToString("o");
    }

    public SDKException(string message, string code, Dictionary<string, object?>? details, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Details = SanitizeDetails(details);
        Timestamp = DateTime.UtcNow.ToString("o");
    }

    private static Dictionary<string, object?>? SanitizeDetails(Dictionary<string, object?>? details)
    {
        if (details == null) return null;

        var sensitiveKeys = new[] { "auth_token", "token", "password", "secret", "api_key", "apikey", "authorization" };
        var sanitized = new Dictionary<string, object?>(details);

        foreach (var key in sanitized.Keys.ToList())
        {
            var lowerKey = key.ToLowerInvariant();
            if (sensitiveKeys.Any(sk => lowerKey.Contains(sk)))
            {
                sanitized[key] = "[REDACTED]";
            }
        }

        return sanitized;
    }

    public Dictionary<string, object?> ToSerializable()
    {
        return new Dictionary<string, object?>
        {
            ["name"] = GetType().Name,
            ["message"] = Message,
            ["code"] = Code,
            ["details"] = Details,
            ["timestamp"] = Timestamp
        };
    }
}

/// <summary>
/// Erro de autenticação
/// </summary>
public class AuthenticationException : SDKException
{
    public AuthenticationException(string message = "Falha na autenticação", Dictionary<string, object?>? details = null)
        : base(message, "AUTHENTICATION_ERROR", details) { }
}

/// <summary>
/// Erro de validação
/// </summary>
public class ValidationException : SDKException
{
    public ValidationException(string message = "Erro de validação", Dictionary<string, object?>? details = null)
        : base(message, "VALIDATION_ERROR", details) { }
}

/// <summary>
/// Erro de rate limiting
/// </summary>
public class RateLimitException : SDKException
{
    public int? RetryAfter { get; }

    public RateLimitException(string message = "Limite de requisições excedido", Dictionary<string, object?>? details = null)
        : base(message, "RATE_LIMIT_ERROR", details)
    {
        if (details != null && details.TryGetValue("retryAfter", out var retryAfter) && retryAfter is int ra)
        {
            RetryAfter = ra;
        }
    }
}

/// <summary>
/// Erro de endpoint não encontrado
/// </summary>
public class EndpointNotFoundException : SDKException
{
    public EndpointNotFoundException(string message = "Endpoint não encontrado", Dictionary<string, object?>? details = null)
        : base(message, "ENDPOINT_NOT_FOUND", details) { }
}

/// <summary>
/// Erro de especificação
/// </summary>
public class SpecificationException : SDKException
{
    public SpecificationException(string message = "Erro na especificação", Dictionary<string, object?>? details = null)
        : base(message, "SPECIFICATION_ERROR", details) { }
}

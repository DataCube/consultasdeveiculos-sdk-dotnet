using System.Text.Json;
using ConsultasDeVeiculos.SDK.Core;

namespace ConsultasDeVeiculos.SDK.Parser;

/// <summary>
/// Parser de requisições Postman - converte item de requisição em EndpointDefinition
/// </summary>
public class RequestParser
{
    public EndpointDefinition? Parse(JsonElement item, string ns = "")
    {
        if (!item.TryGetProperty("request", out var request))
            return null;

        var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unnamed" : "Unnamed";
        var methodName = NormalizeMethodName(name);
        var key = string.IsNullOrEmpty(ns) ? methodName : $"{ns}.{methodName}";

        return new EndpointDefinition
        {
            Key = key,
            Name = name,
            Namespace = ns,
            Method = ParseMethod(request),
            Url = ParseUrl(request),
            Headers = ParseHeaders(request),
            Body = ParseBody(request),
            Description = ParseDescription(request),
            Responses = ParseResponses(item)
        };
    }

    private string NormalizeMethodName(string name)
    {
        // Remove caracteres especiais
        var cleaned = System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\s]", "");
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return "unnamed";

        // camelCase
        return string.Concat(words.Select((word, index) =>
            index == 0
                ? word.ToLowerInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private string ParseMethod(JsonElement request)
    {
        if (request.ValueKind == JsonValueKind.String) return "GET";
        if (request.TryGetProperty("method", out var method)) return method.GetString() ?? "GET";
        return "GET";
    }

    private string ParseUrl(JsonElement request)
    {
        if (request.ValueKind == JsonValueKind.String) return request.GetString() ?? "";

        if (!request.TryGetProperty("url", out var url)) return "";

        if (url.ValueKind == JsonValueKind.String) return url.GetString() ?? "";

        // URL estruturada do Postman
        if (url.TryGetProperty("raw", out var raw)) return raw.GetString() ?? "";

        // Constrói URL a partir de partes
        var protocol = url.TryGetProperty("protocol", out var proto) ? proto.GetString() ?? "https" : "https";
        var host = "";
        if (url.TryGetProperty("host", out var hostProp))
        {
            host = hostProp.ValueKind == JsonValueKind.Array
                ? string.Join(".", hostProp.EnumerateArray().Select(h => h.GetString()))
                : hostProp.GetString() ?? "";
        }
        var path = "";
        if (url.TryGetProperty("path", out var pathProp))
        {
            path = pathProp.ValueKind == JsonValueKind.Array
                ? string.Join("/", pathProp.EnumerateArray().Select(p => p.GetString()))
                : pathProp.GetString() ?? "";
        }
        var port = url.TryGetProperty("port", out var portProp) ? $":{portProp.GetString()}" : "";

        return $"{protocol}://{host}{port}/{path}";
    }

    private Dictionary<string, string> ParseHeaders(JsonElement request)
    {
        var headers = new Dictionary<string, string>();

        if (request.ValueKind != JsonValueKind.Object) return headers;
        if (!request.TryGetProperty("header", out var headerArray)) return headers;
        if (headerArray.ValueKind != JsonValueKind.Array) return headers;

        foreach (var header in headerArray.EnumerateArray())
        {
            var key = header.TryGetProperty("key", out var k) ? k.GetString() : null;
            var value = header.TryGetProperty("value", out var v) ? v.GetString() : null;
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                headers[key] = value;
            }
        }

        return headers;
    }

    private Dictionary<string, object?>? ParseBody(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object) return null;
        if (!request.TryGetProperty("body", out var body)) return null;

        if (body.TryGetProperty("raw", out var raw))
        {
            var rawStr = raw.GetString();
            if (!string.IsNullOrEmpty(rawStr))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(rawStr);
                    if (parsed != null)
                    {
                        parsed.Remove("auth_token");
                        return parsed.Count > 0 ? parsed : null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        if (body.TryGetProperty("formdata", out var formdata) && formdata.ValueKind == JsonValueKind.Array)
        {
            var result = new Dictionary<string, object?>();
            foreach (var item in formdata.EnumerateArray())
            {
                var key = item.TryGetProperty("key", out var k) ? k.GetString() : null;
                var value = item.TryGetProperty("value", out var v) ? v.GetString() : null;
                if (!string.IsNullOrEmpty(key) && key != "auth_token")
                {
                    result[key] = value;
                }
            }
            return result.Count > 0 ? result : null;
        }

        if (body.TryGetProperty("urlencoded", out var urlencoded) && urlencoded.ValueKind == JsonValueKind.Array)
        {
            var result = new Dictionary<string, object?>();
            foreach (var item in urlencoded.EnumerateArray())
            {
                var key = item.TryGetProperty("key", out var k) ? k.GetString() : null;
                var value = item.TryGetProperty("value", out var v) ? v.GetString() : null;
                if (!string.IsNullOrEmpty(key) && key != "auth_token")
                {
                    result[key] = value;
                }
            }
            return result.Count > 0 ? result : null;
        }

        return null;
    }

    private string? ParseDescription(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object) return null;
        if (!request.TryGetProperty("description", out var desc)) return null;

        if (desc.ValueKind == JsonValueKind.String) return desc.GetString();
        if (desc.ValueKind == JsonValueKind.Object && desc.TryGetProperty("content", out var content))
            return content.GetString();

        return null;
    }

    private List<EndpointResponse> ParseResponses(JsonElement item)
    {
        var responses = new List<EndpointResponse>();

        if (!item.TryGetProperty("response", out var responseArray)) return responses;
        if (responseArray.ValueKind != JsonValueKind.Array) return responses;

        foreach (var resp in responseArray.EnumerateArray())
        {
            var endpointResponse = new EndpointResponse
            {
                Name = resp.TryGetProperty("name", out var name) ? name.GetString() : null,
                Status = resp.TryGetProperty("code", out var code) ? code.GetInt32() : 200,
                Body = resp.TryGetProperty("body", out var body) ? body.GetString() : null
            };

            if (resp.TryGetProperty("header", out var headers) && headers.ValueKind == JsonValueKind.Array)
            {
                endpointResponse.Headers = new Dictionary<string, string>();
                foreach (var header in headers.EnumerateArray())
                {
                    var key = header.TryGetProperty("key", out var k) ? k.GetString() : null;
                    var value = header.TryGetProperty("value", out var v) ? v.GetString() : null;
                    if (!string.IsNullOrEmpty(key) && value != null)
                    {
                        endpointResponse.Headers[key] = value;
                    }
                }
            }

            responses.Add(endpointResponse);
        }

        return responses;
    }
}

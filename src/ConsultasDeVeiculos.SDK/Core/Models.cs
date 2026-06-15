using System.Text.Json.Serialization;

namespace ConsultasDeVeiculos.SDK.Core;

/// <summary>
/// Definição de um endpoint parseado do Postman
/// </summary>
public class EndpointDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, object?>? Body { get; set; }
    public string? Description { get; set; }
    public List<EndpointResponse> Responses { get; set; } = new();
    public string? CollectionName { get; set; }
    public string? CollectionId { get; set; }
}

/// <summary>
/// Resposta de exemplo de um endpoint
/// </summary>
public class EndpointResponse
{
    public string? Name { get; set; }
    public int Status { get; set; } = 200;
    public string? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Resultado de uma requisição
/// </summary>
public class SDKResponse
{
    public bool Success { get; set; }
    public int Status { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public bool Sandbox { get; set; }
    public string? Endpoint { get; set; }
}

/// <summary>
/// Informações da SDK
/// </summary>
public class SDKInfo
{
    public string RuntimeVersion { get; set; } = string.Empty;
    public string SpecVersion { get; set; } = string.Empty;
    public string? GeneratedAt { get; set; }
    public bool Sandbox { get; set; }
    public int EndpointsCount { get; set; }
    public List<string> Namespaces { get; set; } = new();
    public int SlugsCount { get; set; }
}

/// <summary>
/// Informação resumida de endpoint
/// </summary>
public class EndpointInfo
{
    public string Slug { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Params { get; set; } = new();
}

/// <summary>
/// Manifest da especificação
/// </summary>
public class SpecManifest
{
    [JsonPropertyName("specVersion")]
    public string SpecVersion { get; set; } = "1.0.0";

    [JsonPropertyName("minRuntimeVersion")]
    public string MinRuntimeVersion { get; set; } = "1.0.0";

    [JsonPropertyName("generatedAt")]
    public string? GeneratedAt { get; set; }
}

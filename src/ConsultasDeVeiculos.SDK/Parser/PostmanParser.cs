using System.Text.Json;
using ConsultasDeVeiculos.SDK.Core;

namespace ConsultasDeVeiculos.SDK.Parser;

/// <summary>
/// Parser principal de coleções Postman
/// </summary>
public class PostmanParser
{
    private readonly FolderParser _folderParser = new();
    private readonly RequestParser _requestParser = new();

    /// <summary>
    /// Parseia uma coleção Postman completa
    /// </summary>
    public List<EndpointDefinition> Parse(JsonElement collection)
    {
        if (collection.ValueKind != JsonValueKind.Object) return new List<EndpointDefinition>();
        if (!collection.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array)
            return new List<EndpointDefinition>();

        var endpoints = new List<EndpointDefinition>();

        foreach (var item in items.EnumerateArray())
        {
            if (IsFolder(item))
            {
                var folderEndpoints = _folderParser.Parse(item, "");
                endpoints.AddRange(folderEndpoints);
            }
            else if (IsRequest(item))
            {
                var endpoint = _requestParser.Parse(item, "");
                if (endpoint != null)
                {
                    endpoints.Add(endpoint);
                }
            }
        }

        // Aplica transformações globais
        return PostProcess(endpoints, collection);
    }

    private bool IsFolder(JsonElement item)
    {
        return item.TryGetProperty("item", out var items) &&
               items.ValueKind == JsonValueKind.Array &&
               items.GetArrayLength() > 0;
    }

    private bool IsRequest(JsonElement item)
    {
        return item.TryGetProperty("request", out _);
    }

    private List<EndpointDefinition> PostProcess(List<EndpointDefinition> endpoints, JsonElement collection)
    {
        var variables = ExtractVariables(collection);
        var collectionName = collection.TryGetProperty("info", out var info) &&
                             info.TryGetProperty("name", out var name) ? name.GetString() : null;
        var collectionId = info.TryGetProperty("_postman_id", out var id) ? id.GetString() : null;

        foreach (var endpoint in endpoints)
        {
            // Substitui variáveis nas URLs
            endpoint.Url = ReplaceVariables(endpoint.Url, variables);

            // Substitui variáveis nos headers
            foreach (var key in endpoint.Headers.Keys.ToList())
            {
                endpoint.Headers[key] = ReplaceVariables(endpoint.Headers[key], variables);
            }

            endpoint.CollectionName = collectionName;
            endpoint.CollectionId = collectionId;
        }

        return endpoints;
    }

    private Dictionary<string, string> ExtractVariables(JsonElement collection)
    {
        var variables = new Dictionary<string, string>();

        if (!collection.TryGetProperty("variable", out var varList) || varList.ValueKind != JsonValueKind.Array)
            return variables;

        foreach (var v in varList.EnumerateArray())
        {
            var key = v.TryGetProperty("key", out var k) ? k.GetString() : null;
            var value = v.TryGetProperty("value", out var val) ? val.GetString() : null;
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                variables[key] = value;
            }
        }

        return variables;
    }

    private string ReplaceVariables(string input, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(input)) return input;

        foreach (var (key, value) in variables)
        {
            input = input.Replace($"{{{{{key}}}}}", value);
        }

        return input;
    }
}

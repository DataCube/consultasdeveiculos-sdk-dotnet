using System.Globalization;
using System.Text;
using System.Text.Json;
using ConsultasDeVeiculos.SDK.Core;

namespace ConsultasDeVeiculos.SDK.Parser;

/// <summary>
/// Parser de pastas Postman - converte em namespaces da SDK
/// </summary>
public class FolderParser
{
    private readonly RequestParser _requestParser = new();

    public List<EndpointDefinition> Parse(JsonElement folder, string parentNamespace = "")
    {
        var endpoints = new List<EndpointDefinition>();
        var ns = BuildNamespace(folder, parentNamespace);

        if (!folder.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array)
            return endpoints;

        foreach (var item in items.EnumerateArray())
        {
            if (IsFolder(item))
            {
                var subEndpoints = Parse(item, ns);
                endpoints.AddRange(subEndpoints);
            }
            else if (IsRequest(item))
            {
                var endpoint = _requestParser.Parse(item, ns);
                if (endpoint != null)
                {
                    endpoints.Add(endpoint);
                }
            }
        }

        return endpoints;
    }

    private string BuildNamespace(JsonElement folder, string parentNamespace)
    {
        var folderName = folder.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
        var normalizedName = NormalizeFolderName(folderName);

        if (string.IsNullOrEmpty(normalizedName))
            return parentNamespace;

        return string.IsNullOrEmpty(parentNamespace)
            ? normalizedName
            : $"{parentNamespace}.{normalizedName}";
    }

    private string NormalizeFolderName(string name)
    {
        // Remove acentos
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var withoutAccents = sb.ToString().Normalize(NormalizationForm.FormC);

        // Remove caracteres especiais
        var cleaned = System.Text.RegularExpressions.Regex.Replace(withoutAccents, @"[^\w\s]", "");
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return "";

        // camelCase
        return string.Concat(words.Select((word, index) =>
            index == 0
                ? word.ToLowerInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
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
}

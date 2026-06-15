using ConsultasDeVeiculos.SDK.Errors;

namespace ConsultasDeVeiculos.SDK.Core;

/// <summary>
/// Registro de endpoints - armazena e gerencia todos os endpoints parseados
/// </summary>
public class EndpointRegistry
{
    private readonly Dictionary<string, EndpointDefinition> _endpoints = new();
    private readonly Dictionary<string, Dictionary<string, object>> _namespaceTree = new();

    public void Register(EndpointDefinition endpoint)
    {
        if (string.IsNullOrEmpty(endpoint.Key))
            throw new ArgumentException("Endpoint deve ter uma key");

        _endpoints[endpoint.Key] = endpoint;
        AddToNamespaceTree(endpoint);
    }

    public EndpointDefinition Get(string key)
    {
        if (_endpoints.TryGetValue(key, out var endpoint))
            return endpoint;

        throw new EndpointNotFoundException($"Endpoint \"{key}\" não encontrado",
            new Dictionary<string, object?> { ["key"] = key });
    }

    public bool Has(string key) => _endpoints.ContainsKey(key);

    public IReadOnlyList<EndpointDefinition> List() => _endpoints.Values.ToList();

    public IReadOnlyList<EndpointDefinition> ListByNamespace(string ns)
        => _endpoints.Values.Where(ep => ep.Key.StartsWith(ns + ".")).ToList();

    public IReadOnlyList<string> GetNamespaces() => _namespaceTree.Keys.ToList();

    public int Size => _endpoints.Count;

    public void Clear()
    {
        _endpoints.Clear();
        _namespaceTree.Clear();
    }

    private void AddToNamespaceTree(EndpointDefinition endpoint)
    {
        var parts = endpoint.Key.Split('.');
        if (parts.Length == 0) return;

        var ns = parts[0];
        if (!_namespaceTree.ContainsKey(ns))
            _namespaceTree[ns] = new Dictionary<string, object>();
    }
}

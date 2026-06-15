namespace ConsultasDeVeiculos.SDK.Core;

/// <summary>
/// Gerenciador de configurações da SDK
/// </summary>
public class ConfigManager
{
    private static readonly Dictionary<string, object> DefaultConfig = new()
    {
        ["cacheDir"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".consultas-de-veiculos-sdk"),
        ["downloadUrl"] = Environment.GetEnvironmentVariable("DOWNLOAD_URL") ?? "https://painel.consultasdeveiculos.com/download-postman",
        ["timeout"] = 30000,
        ["maxRetries"] = 3,
        ["retryDelay"] = 1000,
        ["compression"] = true
    };

    private readonly Dictionary<string, object> _config;

    public ConfigManager(SDKOptions? options = null)
    {
        _config = new Dictionary<string, object>(DefaultConfig);

        if (options != null)
        {
            if (options.Timeout > 0) _config["timeout"] = options.Timeout;
            if (options.MaxRetries >= 0) _config["maxRetries"] = options.MaxRetries;
            if (options.RetryDelay > 0) _config["retryDelay"] = options.RetryDelay;
            _config["compression"] = options.Compression;
        }

        EnsureCacheDir();
    }

    public T Get<T>(string key)
    {
        if (_config.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return default!;
    }

    public void Set(string key, object value)
    {
        _config[key] = value;
    }

    public string GetCacheDir() => (string)_config["cacheDir"];

    public string GetCachedPostmanPath() => Path.Combine(GetCacheDir(), "postman.json");

    public string GetCachedManifestPath() => Path.Combine(GetCacheDir(), "manifest.json");

    /// <summary>
    /// Encontra arquivo Postman em um diretório
    /// </summary>
    public string? FindPostmanFile(string dir)
    {
        if (!Directory.Exists(dir)) return null;

        var files = Directory.GetFiles(dir);

        // Tenta padrão versionado
        var versionedFile = files.FirstOrDefault(f =>
            Path.GetFileName(f).StartsWith("Consultas", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(f).EndsWith(".postman_collection.json", StringComparison.OrdinalIgnoreCase));

        if (versionedFile != null) return versionedFile;

        // Fallback para postman.json
        var postmanFile = files.FirstOrDefault(f =>
            Path.GetFileName(f).Equals("postman.json", StringComparison.OrdinalIgnoreCase));

        return postmanFile;
    }

    /// <summary>
    /// Extrai versão do nome do arquivo
    /// </summary>
    public string? ExtractVersionFromFilename(string filename)
    {
        var match = System.Text.RegularExpressions.Regex.Match(filename, @"V([\d.]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    public bool HasLocalCache()
    {
        return File.Exists(GetCachedPostmanPath());
    }

    private void EnsureCacheDir()
    {
        var dir = GetCacheDir();
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}

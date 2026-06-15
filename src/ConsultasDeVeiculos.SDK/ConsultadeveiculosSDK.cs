using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;
using ConsultasDeVeiculos.SDK.Parser;
using ConsultasDeVeiculos.SDK.Transport;

namespace ConsultasDeVeiculos.SDK;

/// <summary>
/// ConsultadeveiculosSDK
/// 
/// Runtime Engine que consome endpoints definidos em coleções Postman
/// sem necessidade de implementação manual de cada endpoint.
/// 
/// Endpoints são acessados via método dinâmico baseado no slug da URL:
/// URL: veiculos/debitos-sp → método: ExecuteAsync("veiculos_debitos_sp", params)
/// 
/// Ou via indexador: sdk["veiculos_debitos_sp"]
/// </summary>
public class ConsultadeveiculosSDK
{
    public const string VERSION = "1.0.0";

    private readonly string? _authToken;
    private readonly bool _sandbox;
    private readonly SDKOptions _options;
    private readonly ConfigManager _configManager;
    private readonly EndpointRegistry _registry;
    private readonly TransportBase _transport;
    private readonly Dictionary<string, EndpointDefinition> _slugMap = new();
    private readonly SpecManifest _manifest;

    /// <summary>
    /// Indica se a SDK foi inicializada com sucesso
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// Indica se está em modo sandbox
    /// </summary>
    public bool IsSandbox => _sandbox;

    /// <summary>
    /// Cria uma nova instância da SDK a partir do arquivo .env
    /// Carrega variáveis do .env e cria a SDK automaticamente.
    /// </summary>
    /// <param name="envFilePath">Caminho do .env (opcional, busca automaticamente)</param>
    /// <returns>Instância configurada da SDK</returns>
    public static ConsultadeveiculosSDK FromEnv(string? envFilePath = null)
    {
        DotEnv.Load(envFilePath);
        var options = DotEnv.CreateOptionsFromEnvironment();
        return new ConsultadeveiculosSDK(options);
    }

    /// <summary>
    /// Cria uma nova instância da SDK a partir do arquivo .env em modo sandbox.
    /// </summary>
    /// <param name="envFilePath">Caminho do .env (opcional, busca automaticamente)</param>
    /// <returns>Instância em modo sandbox</returns>
    public static ConsultadeveiculosSDK FromEnvSandbox(string? envFilePath = null)
    {
        DotEnv.Load(envFilePath);
        var options = DotEnv.CreateOptionsFromEnvironment();
        options.Sandbox = true;
        return new ConsultadeveiculosSDK(options);
    }

    /// <summary>
    /// Cria uma nova instância da SDK
    /// </summary>
    /// <param name="options">Opções de configuração</param>
    public ConsultadeveiculosSDK(SDKOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sandbox = options.Sandbox;

        // Token armazenado de forma segura
        _authToken = _sandbox ? null : options.AuthToken;

        // Validação do token em modo produção
        if (!_sandbox && string.IsNullOrEmpty(_authToken))
        {
            throw new AuthenticationException(
                "AuthToken é obrigatório. Use Sandbox = true para modo de teste.");
        }

        // Inicialização
        _configManager = new ConfigManager(options);
        _registry = new EndpointRegistry();

        // Carrega spec
        var (postman, manifest) = LoadSpec();
        _manifest = manifest;

        // Valida compatibilidade
        ValidateCompatibility();

        // Parseia coleção Postman
        var parser = new PostmanParser();
        var endpoints = parser.Parse(postman);

        // Registra endpoints e cria mapa de slugs
        foreach (var endpoint in endpoints)
        {
            _registry.Register(endpoint);

            var slug = UrlToSlug(endpoint.Url);
            if (!string.IsNullOrEmpty(slug) && !_slugMap.ContainsKey(slug))
            {
                _slugMap[slug] = endpoint;
            }
        }

        // Cria transport
        _transport = CreateTransport();

        Initialized = true;
    }

    /// <summary>
    /// Executa um endpoint pelo slug
    /// </summary>
    /// <param name="slug">Slug do endpoint (ex: "veiculos_agregados")</param>
    /// <param name="parameters">Parâmetros da requisição</param>
    /// <returns>Resposta da API</returns>
    public async Task<SDKResponse> ExecuteAsync(string slug, Dictionary<string, object?>? parameters = null)
    {
        if (!_slugMap.TryGetValue(slug, out var endpoint))
        {
            var available = _slugMap.Keys.Take(5).ToList();
            throw new EndpointNotFoundException(
                $"Endpoint \"{slug}\" não encontrado. Exemplos disponíveis: {string.Join(", ", available)}...",
                new Dictionary<string, object?>
                {
                    ["slug"] = slug,
                    ["availableExamples"] = available
                });
        }

        return await _transport.RequestAsync(endpoint, parameters != null ? new Dictionary<string, object?>(parameters) : null);
    }

    /// <summary>
    /// Indexador para acesso direto por slug
    /// </summary>
    public EndpointAccessor this[string slug] => new(this, slug);

    /// <summary>
    /// Obtém informações da SDK
    /// </summary>
    public SDKInfo GetInfo()
    {
        return new SDKInfo
        {
            RuntimeVersion = VERSION,
            SpecVersion = _manifest.SpecVersion,
            GeneratedAt = _manifest.GeneratedAt,
            Sandbox = _sandbox,
            EndpointsCount = _registry.Size,
            Namespaces = _registry.GetNamespaces().ToList(),
            SlugsCount = _slugMap.Count
        };
    }

    /// <summary>
    /// Lista todos os endpoints disponíveis
    /// </summary>
    public List<EndpointInfo> ListEndpoints()
    {
        var endpoints = new List<EndpointInfo>();

        foreach (var (slug, endpoint) in _slugMap)
        {
            var parameters = endpoint.Body?.Keys.ToList() ?? new List<string>();
            endpoints.Add(new EndpointInfo
            {
                Slug = slug,
                Key = endpoint.Key,
                Name = endpoint.Name,
                Method = endpoint.Method,
                Url = endpoint.Url,
                Description = endpoint.Description,
                Params = parameters
            });
        }

        return endpoints;
    }

    /// <summary>
    /// Lista apenas os slugs disponíveis
    /// </summary>
    public List<string> ListSlugs() => _slugMap.Keys.ToList();

    /// <summary>
    /// Busca endpoints por padrão
    /// </summary>
    public List<EndpointInfo> SearchEndpoints(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var results = new List<EndpointInfo>();

        foreach (var (slug, endpoint) in _slugMap)
        {
            if (regex.IsMatch(slug) || regex.IsMatch(endpoint.Name))
            {
                var parameters = endpoint.Body?.Keys.ToList() ?? new List<string>();
                results.Add(new EndpointInfo
                {
                    Slug = slug,
                    Key = endpoint.Key,
                    Name = endpoint.Name,
                    Method = endpoint.Method,
                    Url = endpoint.Url,
                    Description = endpoint.Description,
                    Params = parameters
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Converte URL em slug para nome do método
    /// Ex: https://api.com/veiculos/debitos-sp → veiculos_debitos_sp
    /// </summary>
    internal static string? UrlToSlug(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var path = url;

            // Se for URL completa, extrai apenas o path
            if (url.Contains("://"))
            {
                var uri = new Uri(url);
                path = uri.AbsolutePath;
            }

            // Remove barra inicial e final
            path = path.Trim('/');

            // Remove variáveis de path como {{baseUrl}}
            path = Regex.Replace(path, @"\{\{[^}]+\}\}", "");
            path = path.TrimStart('/');

            // Substitui / por _ e - por _
            var slug = path
                .Replace('/', '_')
                .Replace('-', '_');

            // Remove underscores duplicados
            slug = Regex.Replace(slug, @"_+", "_");

            // Remove underscores no início/fim
            slug = slug.Trim('_').ToLowerInvariant();

            return string.IsNullOrEmpty(slug) ? null : slug;
        }
        catch
        {
            return null;
        }
    }

    private (JsonElement postman, SpecManifest manifest) LoadSpec()
    {
        // 1. Tenta carregar do cache local primeiro
        var cachedPostmanPath = _configManager.GetCachedPostmanPath();

        if (File.Exists(cachedPostmanPath))
        {
            try
            {
                var json = File.ReadAllText(cachedPostmanPath);
                var postman = JsonSerializer.Deserialize<JsonElement>(json);
                var manifest = LoadOrCreateManifest(_configManager.GetCachedManifestPath(), postman, null);
                return (postman, manifest);
            }
            catch
            {
                // Cache inválido, continua
            }
        }

        // 2. Tenta carregar do embedded resource
        var fromEmbedded = TryLoadFromEmbeddedResource();
        if (fromEmbedded.HasValue)
            return fromEmbedded.Value;

        // 3. Tenta carregar do diretório spec/ local
        var fromSpecDir = TryLoadFromSpecDir();
        if (fromSpecDir.HasValue)
            return fromSpecDir.Value;

        // 4. Nenhuma fonte disponível - faz download automático
        return DownloadSpecSync();
    }

    private (JsonElement postman, SpecManifest manifest)? TryLoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("postman.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName == null) return null;

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        var postmanDoc = JsonSerializer.Deserialize<JsonElement>(content);
        var manifestResult = LoadOrCreateManifest(null, postmanDoc, null);
        return (postmanDoc, manifestResult);
    }

    private (JsonElement postman, SpecManifest manifest)? TryLoadFromSpecDir()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? ".";

        // Tenta relativo ao assembly
        var specDir = Path.Combine(assemblyDir, "..", "..", "..", "..", "spec");
        var postmanPath = _configManager.FindPostmanFile(specDir);

        if (postmanPath == null)
        {
            // Tenta relativo ao diretório de trabalho
            specDir = Path.Combine(Directory.GetCurrentDirectory(), "spec");
            postmanPath = _configManager.FindPostmanFile(specDir);
        }

        if (postmanPath == null) return null;

        var json = File.ReadAllText(postmanPath);
        var postman = JsonSerializer.Deserialize<JsonElement>(json);
        var manifest = LoadOrCreateManifest(Path.Combine(Path.GetDirectoryName(postmanPath)!, "manifest.json"), postman, postmanPath);
        return (postman, manifest);
    }

    private (JsonElement postman, SpecManifest manifest) DownloadSpecSync()
    {
        var downloadUrl = Environment.GetEnvironmentVariable("DOWNLOAD_URL")
            ?? "https://painel.consultasdeveiculos.com/download-postman";

        var cacheDir = _configManager.GetCacheDir();
        Directory.CreateDirectory(cacheDir);

        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = httpClient.GetAsync(downloadUrl).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Salva no cache
            var cachedPath = _configManager.GetCachedPostmanPath();
            File.WriteAllText(cachedPath, content);

            // Salva manifest
            var manifestObj = new SpecManifest
            {
                SpecVersion = "1.0.0",
                MinRuntimeVersion = "1.0.0",
                GeneratedAt = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(_configManager.GetCachedManifestPath(),
                JsonSerializer.Serialize(manifestObj));

            var postman = JsonSerializer.Deserialize<JsonElement>(content);
            return (postman, manifestObj);
        }
        catch (Exception ex)
        {
            throw new SpecificationException(
                $"Arquivo Postman não encontrado e falha ao baixar automaticamente: {ex.Message}. " +
                "Execute 'consultas-de-veiculos-sdk update' manualmente.");
        }
    }

    private SpecManifest LoadOrCreateManifest(string? manifestPath, JsonElement postman, string? postmanPath)
    {
        if (!string.IsNullOrEmpty(manifestPath) && File.Exists(manifestPath))
        {
            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<SpecManifest>(json);
                if (manifest != null)
                {
                    if (string.IsNullOrEmpty(manifest.SpecVersion) && !string.IsNullOrEmpty(postmanPath))
                    {
                        manifest.SpecVersion = _configManager.ExtractVersionFromFilename(Path.GetFileName(postmanPath)) ?? "1.0.0";
                    }
                    return manifest;
                }
            }
            catch { }
        }

        // Cria manifest padrão
        var specVersion = "1.0.0";
        if (!string.IsNullOrEmpty(postmanPath))
        {
            specVersion = _configManager.ExtractVersionFromFilename(Path.GetFileName(postmanPath)) ?? "1.0.0";
        }
        if (postman.TryGetProperty("info", out var info) && info.TryGetProperty("version", out var version))
        {
            specVersion = version.GetString() ?? specVersion;
        }

        return new SpecManifest
        {
            SpecVersion = specVersion,
            MinRuntimeVersion = "1.0.0",
            GeneratedAt = DateTime.UtcNow.ToString("o")
        };
    }

    private void ValidateCompatibility()
    {
        var minRuntimeVersion = _manifest.MinRuntimeVersion;
        if (!IsVersionCompatible(VERSION, minRuntimeVersion))
        {
            throw new SpecificationException(
                $"Atualize a SDK para continuar utilizando esta versão da API Specification. " +
                $"Runtime atual: {VERSION}, Mínimo requerido: {minRuntimeVersion}");
        }
    }

    private static bool IsVersionCompatible(string current, string minimum)
    {
        var curr = current.Split('.').Select(int.Parse).ToArray();
        var min = minimum.Split('.').Select(int.Parse).ToArray();

        if (curr[0] > min[0]) return true;
        if (curr[0] < min[0]) return false;
        if (curr[1] > min[1]) return true;
        if (curr[1] < min[1]) return false;
        return curr[2] >= min[2];
    }

    private TransportBase CreateTransport()
    {
        var transportOptions = new TransportOptions
        {
            Token = _authToken,
            BaseUrl = _options.BaseUrl,
            Timeout = _configManager.Get<int>("timeout"),
            MaxRetries = _configManager.Get<int>("maxRetries"),
            RetryDelay = _configManager.Get<int>("retryDelay"),
            Compression = _configManager.Get<bool>("compression"),
            Headers = _options.Headers,
            SandboxDelay = _options.SandboxDelay,
            SandboxRandomErrors = _options.SandboxRandomErrors,
            SandboxErrorRate = _options.SandboxErrorRate
        };

        if (_sandbox)
        {
            return new SandboxTransport(transportOptions);
        }

        return new HttpTransport(transportOptions);
    }
}

/// <summary>
/// Accessor para chamada fluente via indexador
/// </summary>
public class EndpointAccessor
{
    private readonly ConsultadeveiculosSDK _sdk;
    private readonly string _slug;

    internal EndpointAccessor(ConsultadeveiculosSDK sdk, string slug)
    {
        _sdk = sdk;
        _slug = slug;
    }

    /// <summary>
    /// Executa o endpoint com os parâmetros informados
    /// </summary>
    public Task<SDKResponse> ExecuteAsync(Dictionary<string, object?>? parameters = null)
        => _sdk.ExecuteAsync(_slug, parameters);

    /// <summary>
    /// Executa o endpoint com parâmetros como objeto anônimo
    /// </summary>
    public Task<SDKResponse> ExecuteAsync(object parameters)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in parameters.GetType().GetProperties())
        {
            dict[prop.Name] = prop.GetValue(parameters);
        }
        return _sdk.ExecuteAsync(_slug, dict);
    }
}

using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

namespace ConsultasDeVeiculos.SDK.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return 0;
        }

        var command = args[0];
        var commandArgs = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "endpoints" => HandleEndpoints(commandArgs),
                "version" => HandleVersion(),
                "doctor" => HandleDoctor(),
                "clear-cache" => HandleClearCache(),
                "update" => await HandleUpdateAsync(),
                _ => HandleUnknown(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Erro: {ex.Message}");
            if (Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static int HandleEndpoints(string[] args)
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var filter = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : null;
        var verbose = args.Contains("--verbose") || args.Contains("-v");

        List<EndpointInfo> endpoints;
        if (!string.IsNullOrEmpty(filter))
        {
            endpoints = sdk.SearchEndpoints(filter);
            Console.WriteLine($"\n📡 Endpoints contendo \"{filter}\": {endpoints.Count}\n");
        }
        else
        {
            endpoints = sdk.ListEndpoints();
            Console.WriteLine($"\n📡 {endpoints.Count} Endpoints Disponíveis\n");
        }

        // Agrupa por namespace
        var grouped = endpoints.GroupBy(e => e.Slug.Split('_')[0]).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            Console.WriteLine($"  {group.Key.ToUpperInvariant()}:");
            foreach (var ep in group)
            {
                var paramsStr = ep.Params.Count > 0 ? $"{{ {string.Join(", ", ep.Params)} }}" : "";
                Console.WriteLine($"    • {ep.Slug}({paramsStr})");
                if (verbose && !string.IsNullOrEmpty(ep.Description))
                {
                    Console.WriteLine($"      {ep.Description}");
                }
            }
            Console.WriteLine();
        }

        return 0;
    }

    static int HandleVersion()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var info = sdk.GetInfo();

        Console.WriteLine($"\n📦 ConsultasDeVeiculos SDK .NET");
        Console.WriteLine($"   Runtime: v{info.RuntimeVersion}");
        Console.WriteLine($"   Spec: v{info.SpecVersion}");
        Console.WriteLine($"   Endpoints: {info.EndpointsCount}");
        Console.WriteLine($"   Namespaces: {string.Join(", ", info.Namespaces)}");
        Console.WriteLine($"   .NET: {Environment.Version}\n");

        return 0;
    }

    static int HandleDoctor()
    {
        Console.WriteLine("\n🩺 Diagnóstico do Ambiente\n");

        // .NET version
        Console.WriteLine($"  ✅ .NET {Environment.Version}");
        Console.WriteLine($"  ✅ OS: {Environment.OSVersion}");
        Console.WriteLine($"  ✅ Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

        // Tenta inicializar SDK
        try
        {
            var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
            var info = sdk.GetInfo();
            Console.WriteLine($"  ✅ SDK inicializada ({info.EndpointsCount} endpoints)");
            Console.WriteLine($"  ✅ Spec v{info.SpecVersion}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ SDK: {ex.Message}");
        }

        // Cache
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".consultas-de-veiculos-sdk");
        if (Directory.Exists(cacheDir))
        {
            var cachedPostman = Path.Combine(cacheDir, "postman.json");
            if (File.Exists(cachedPostman))
            {
                var fileInfo = new FileInfo(cachedPostman);
                Console.WriteLine($"  ✅ Cache: {fileInfo.Length / 1024}KB (atualizado: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm})");
            }
            else
            {
                Console.WriteLine("  ⚠️  Cache: postman.json não encontrado");
            }
        }
        else
        {
            Console.WriteLine("  ⚠️  Cache: diretório não existe");
        }

        Console.WriteLine();
        return 0;
    }

    static int HandleClearCache()
    {
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".consultas-de-veiculos-sdk");

        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, true);
            Console.WriteLine("✅ Cache limpo com sucesso.");
        }
        else
        {
            Console.WriteLine("ℹ️  Cache já está vazio.");
        }

        return 0;
    }

    static async Task<int> HandleUpdateAsync()
    {
        var downloadUrl = Environment.GetEnvironmentVariable("DOWNLOAD_URL")
            ?? "https://painel.consultasdeveiculos.com/download-postman";

        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".consultas-de-veiculos-sdk");
        Directory.CreateDirectory(cacheDir);

        Console.WriteLine($"📥 Baixando especificação de {downloadUrl}...");

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var postmanPath = Path.Combine(cacheDir, "postman.json");
            await File.WriteAllTextAsync(postmanPath, content);

            Console.WriteLine($"✅ Especificação atualizada: {postmanPath}");

            // Cria manifest
            var manifest = new
            {
                specVersion = "1.0.0",
                minRuntimeVersion = "1.0.0",
                generatedAt = DateTime.UtcNow.ToString("o")
            };
            var manifestPath = Path.Combine(cacheDir, "manifest.json");
            await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Falha ao atualizar: {ex.Message}");
            return 1;
        }
    }

    static int HandleUnknown(string command)
    {
        Console.Error.WriteLine($"❌ Comando desconhecido: {command}");
        Console.Error.WriteLine();
        ShowHelp();
        return 1;
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
📦 consultas-de-veiculos-sdk CLI (.NET)

Comandos disponíveis:

  endpoints     Lista todos os endpoints disponíveis (gerados do Postman)
  update        Atualiza a especificação da API
  version       Exibe versões do Runtime e Specification
  doctor        Executa diagnóstico do ambiente
  clear-cache   Limpa o cache local

Uso:
  consultas-de-veiculos-sdk <comando>

Exemplos:
  consultas-de-veiculos-sdk endpoints                 # Lista todos os endpoints
  consultas-de-veiculos-sdk endpoints veiculos        # Endpoints de veículos
  consultas-de-veiculos-sdk endpoints --verbose       # Com descrições
  consultas-de-veiculos-sdk update
  consultas-de-veiculos-sdk version
  consultas-de-veiculos-sdk doctor
");
    }
}

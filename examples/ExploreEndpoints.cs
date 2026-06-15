// Exemplo: Explorar endpoints disponíveis
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

// Carrega .env se existir
DotEnv.Load();

// Usa token do .env se disponível, senão sandbox
ConsultadeveiculosSDK client;
var envToken = Environment.GetEnvironmentVariable("API_TOKEN");
if (!string.IsNullOrEmpty(envToken))
    client = ConsultadeveiculosSDK.FromEnv();
else
    client = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });

// Informações da SDK
var info = client.GetInfo();
Console.WriteLine($"📦 SDK v{info.RuntimeVersion} | Spec v{info.SpecVersion}");
Console.WriteLine($"   Endpoints: {info.EndpointsCount}");
Console.WriteLine($"   Namespaces: {string.Join(", ", info.Namespaces)}");
Console.WriteLine();

// Listar todos os slugs
var slugs = client.ListSlugs();
Console.WriteLine($"📡 {slugs.Count} slugs disponíveis:");
foreach (var slug in slugs.Take(20))
{
    Console.WriteLine($"   • {slug}");
}
if (slugs.Count > 20) Console.WriteLine($"   ... e mais {slugs.Count - 20}");
Console.WriteLine();

// Buscar endpoints por termo
var veiculosEndpoints = client.SearchEndpoints("veiculos");
Console.WriteLine($"🔍 Endpoints com 'veiculos': {veiculosEndpoints.Count}");
foreach (var ep in veiculosEndpoints.Take(10))
{
    var paramsStr = ep.Params.Count > 0 ? $"{{ {string.Join(", ", ep.Params)} }}" : "";
    Console.WriteLine($"   📌 client.ExecuteAsync(\"{ep.Slug}\", {paramsStr})");
    Console.WriteLine($"      {ep.Name} [{ep.Method}]");
}

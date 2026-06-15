// Exemplo básico de uso da SDK
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;
using System.Text.Json;

// Carrega .env se existir (API_TOKEN, API_BASE_URL, etc.)
DotEnv.Load();

// Cria client: usa token do .env se disponível, senão usa sandbox
ConsultadeveiculosSDK client;
var envToken = Environment.GetEnvironmentVariable("API_TOKEN");

if (!string.IsNullOrEmpty(envToken))
{
    Console.WriteLine("🔑 Modo: PRODUÇÃO (token do .env)\n");
    client = ConsultadeveiculosSDK.FromEnv();
}
else
{
    Console.WriteLine("🧪 Modo: SANDBOX (sem .env ou token)\n");
    client = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
}

// Consulta usando o slug do endpoint
Console.WriteLine("📡 Executando: veiculos_agregados({ placa })\n");

var resultado = await client.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});

Console.WriteLine($"Status: {resultado.Status}");
Console.WriteLine($"Sucesso: {resultado.Success}");
Console.WriteLine($"Sandbox: {resultado.Sandbox}");
Console.WriteLine($"Data: {JsonSerializer.Serialize(resultado.Data, new JsonSerializerOptions { WriteIndented = true })}\n");

// Consulta usando indexador
Console.WriteLine("📡 Executando via indexador: client[\"veiculos_agregados\"]\n");

var resultado2 = await client["veiculos_agregados"].ExecuteAsync(new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});

Console.WriteLine($"Status: {resultado2.Status}\n");

// Usando dynamic (chamada similar ao JavaScript)
Console.WriteLine("📡 Executando via dynamic: sdk.veiculos_agregados()\n");

dynamic sdk = client.AsDynamic();
var resultado3 = await sdk.veiculos_agregados(new { placa = "ABC1234" });

Console.WriteLine($"Status: {resultado3.Status}");
Console.WriteLine($"\n✅ Todos os modos funcionaram com sucesso!");

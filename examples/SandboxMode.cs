// Exemplo: Modo Sandbox
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

// Carrega .env se existir (neste exemplo usamos sandbox independente do .env)
DotEnv.Load();

// O modo sandbox permite testar sem conexão com a API real
var client = new ConsultadeveiculosSDK(new SDKOptions
{
    Sandbox = true,
    SandboxDelay = 50,           // Simula latência menor (padrão: 100ms)
    SandboxRandomErrors = false  // Sem erros aleatórios
});

Console.WriteLine("🧪 Modo Sandbox - Respostas simuladas\n");

// As chamadas retornam respostas de exemplo do Postman
var resultado = await client.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});

Console.WriteLine($"Status: {resultado.Status}");
Console.WriteLine($"Sandbox: {resultado.Sandbox}");
Console.WriteLine($"Endpoint: {resultado.Endpoint}");
Console.WriteLine($"Data: {resultado.Data}");

// Múltiplas chamadas (sandbox não consome quota)
Console.WriteLine("\n--- Chamadas em lote ---\n");

var slugsToTest = new[] { "veiculos_agregados", "veiculos_debitos_sp", "cnh_nacional_simples" };

foreach (var slug in slugsToTest)
{
    try
    {
        var result = await client.ExecuteAsync(slug, new Dictionary<string, object?>
        {
            ["placa"] = "TEST1234"
        });
        Console.WriteLine($"✅ {slug}: HTTP {result.Status}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  {slug}: {ex.Message}");
    }
}

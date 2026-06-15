// Exemplo de tratamento de erros
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;

// Carrega .env se existir
DotEnv.Load();

// Exemplo 1: Token inválido
Console.WriteLine("=== Teste 1: Token vazio ===");
try
{
    var client = new ConsultadeveiculosSDK(new SDKOptions
    {
        AuthToken = "" // Token vazio
    });
}
catch (AuthenticationException ex)
{
    Console.WriteLine($"[AuthenticationException] {ex.Message}");
    Console.WriteLine($"  Code: {ex.Code}");
}

// Exemplo 2: Endpoint não encontrado
Console.WriteLine("\n=== Teste 2: Endpoint inexistente ===");
try
{
    var client = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
    await client.ExecuteAsync("endpoint_inexistente");
}
catch (EndpointNotFoundException ex)
{
    Console.WriteLine($"[EndpointNotFoundException] {ex.Message}");
    Console.WriteLine($"  Code: {ex.Code}");
    Console.WriteLine($"  Details: {System.Text.Json.JsonSerializer.Serialize(ex.Details)}");
}

// Exemplo 3: Tratando múltiplos tipos de erro
Console.WriteLine("\n=== Teste 3: Chamada com tratamento de erros ===");

// Usa token do .env se disponível, senão sandbox
ConsultadeveiculosSDK sdk;
var envToken = Environment.GetEnvironmentVariable("API_TOKEN");
if (!string.IsNullOrEmpty(envToken))
{
    sdk = ConsultadeveiculosSDK.FromEnv();
    Console.WriteLine("(usando token do .env)");
}
else
{
    sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
    Console.WriteLine("(usando sandbox)");
}

try
{
    var result = await sdk.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
    {
        ["placa"] = "ABC1234"
    });
    Console.WriteLine($"\n✅ Sucesso: HTTP {result.Status}");
}
catch (AuthenticationException ex)
{
    Console.WriteLine($"🔒 Erro de autenticação: {ex.Message}");
}
catch (ValidationException ex)
{
    Console.WriteLine($"⚠️  Erro de validação: {ex.Message}");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"🚫 Rate limit: {ex.Message} (retry after: {ex.RetryAfter}s)");
}
catch (SDKException ex)
{
    Console.WriteLine($"❌ Erro SDK: [{ex.Code}] {ex.Message}");
}

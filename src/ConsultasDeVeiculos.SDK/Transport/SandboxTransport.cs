using System.Text.Json;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;

namespace ConsultasDeVeiculos.SDK.Transport;

/// <summary>
/// Transporte Sandbox - retorna exemplos presentes no Postman sem chamadas externas
/// </summary>
public class SandboxTransport : TransportBase
{
    private int _delay;
    private bool _randomErrors;
    private double _errorRate;
    private readonly Random _random = new();

    public SandboxTransport(TransportOptions options) : base(options)
    {
        _delay = options.SandboxDelay > 0 ? options.SandboxDelay : 100;
        _randomErrors = options.SandboxRandomErrors;
        _errorRate = options.SandboxErrorRate;
    }

    public override async Task<SDKResponse> RequestAsync(
        EndpointDefinition endpoint,
        Dictionary<string, object?>? parameters = null,
        RequestOptions? options = null)
    {
        // Simula latência de rede
        await SimulateDelay();

        // Simula erros aleatórios se habilitado
        if (_randomErrors && _random.NextDouble() < _errorRate)
        {
            throw new SDKException(
                "Erro simulado de sandbox",
                "SANDBOX_SIMULATED_ERROR",
                new Dictionary<string, object?> { ["endpoint"] = endpoint.Key });
        }

        // Busca resposta de exemplo
        var response = FindExampleResponse(endpoint, parameters);

        return new SDKResponse
        {
            Success = true,
            Status = response.Status,
            Data = response.Data,
            Headers = response.Headers ?? new Dictionary<string, string>(),
            Sandbox = true,
            Endpoint = endpoint.Key
        };
    }

    private SandboxResponse FindExampleResponse(EndpointDefinition endpoint, Dictionary<string, object?>? parameters)
    {
        var responses = endpoint.Responses;

        if (responses.Count == 0)
        {
            return new SandboxResponse
            {
                Status = 200,
                Data = new Dictionary<string, object?>
                {
                    ["success"] = true,
                    ["message"] = $"Sandbox response for {endpoint.Name}",
                    ["endpoint"] = endpoint.Key,
                    ["params"] = parameters
                }
            };
        }

        // Tenta encontrar resposta de sucesso (2xx)
        var successResponse = responses.FirstOrDefault(r => r.Status >= 200 && r.Status < 300);

        if (successResponse != null)
        {
            return ProcessResponse(successResponse, parameters);
        }

        // Retorna primeira resposta disponível
        return ProcessResponse(responses[0], parameters);
    }

    private SandboxResponse ProcessResponse(EndpointResponse response, Dictionary<string, object?>? parameters)
    {
        object? data = response.Body;

        if (data is string strData)
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(strData);
            }
            catch
            {
                // Mantém como string se não for JSON
            }
        }

        return new SandboxResponse
        {
            Status = response.Status > 0 ? response.Status : 200,
            Data = data,
            Headers = response.Headers
        };
    }

    private async Task SimulateDelay()
    {
        var jitter = (int)(_random.NextDouble() * _delay * 0.5);
        var totalDelay = _delay + jitter;
        await Task.Delay(totalDelay);
    }

    public void SetDelay(int ms) => _delay = ms;

    public void SetRandomErrors(bool enabled, double rate = 0.1)
    {
        _randomErrors = enabled;
        _errorRate = rate;
    }

    private class SandboxResponse
    {
        public int Status { get; set; } = 200;
        public object? Data { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}

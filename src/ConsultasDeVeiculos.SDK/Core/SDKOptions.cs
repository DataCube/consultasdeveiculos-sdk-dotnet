namespace ConsultasDeVeiculos.SDK.Core;

/// <summary>
/// Opções de configuração da SDK
/// </summary>
public class SDKOptions
{
    /// <summary>
    /// Token de autenticação do cliente
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Habilita modo sandbox (ignora auth_token)
    /// </summary>
    public bool Sandbox { get; set; }

    /// <summary>
    /// URL base da API (opcional)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Timeout em ms (padrão: 30000)
    /// </summary>
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// Máximo de retries (padrão: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay base para retry exponencial (ms)
    /// </summary>
    public int RetryDelay { get; set; } = 1000;

    /// <summary>
    /// Habilitar compressão
    /// </summary>
    public bool Compression { get; set; } = true;

    /// <summary>
    /// Headers customizados
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Delay simulado em sandbox (ms)
    /// </summary>
    public int SandboxDelay { get; set; } = 100;

    /// <summary>
    /// Simular erros aleatórios em sandbox
    /// </summary>
    public bool SandboxRandomErrors { get; set; }

    /// <summary>
    /// Taxa de erro simulado em sandbox (0.0 a 1.0)
    /// </summary>
    public double SandboxErrorRate { get; set; } = 0.1;
}

namespace ConsultasDeVeiculos.SDK.Core;

/// <summary>
/// Leitor de arquivos .env compatível com Windows, Linux e macOS.
/// Carrega variáveis de ambiente a partir de arquivo .env
/// </summary>
public static class DotEnv
{
    /// <summary>
    /// Carrega variáveis de um arquivo .env para o ambiente do processo.
    /// Procura o arquivo .env no diretório atual e nos pais.
    /// </summary>
    /// <param name="filePath">Caminho específico do arquivo .env (opcional)</param>
    public static void Load(string? filePath = null)
    {
        var envFile = filePath ?? FindEnvFile();
        if (envFile == null || !File.Exists(envFile)) return;

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();

            // Ignora linhas vazias e comentários
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0) continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            // Remove aspas ao redor do valor
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            // Só define se não existir já no ambiente (prioridade para env vars reais)
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>
    /// Cria SDKOptions a partir das variáveis de ambiente (carregadas do .env ou do sistema)
    /// </summary>
    public static SDKOptions CreateOptionsFromEnvironment()
    {
        var options = new SDKOptions();

        var token = Environment.GetEnvironmentVariable("API_TOKEN");
        if (!string.IsNullOrEmpty(token))
            options.AuthToken = token;

        var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(baseUrl))
            options.BaseUrl = baseUrl;

        var timeout = Environment.GetEnvironmentVariable("API_TIMEOUT");
        if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out var t))
            options.Timeout = t;

        var maxRetries = Environment.GetEnvironmentVariable("API_MAX_RETRIES");
        if (!string.IsNullOrEmpty(maxRetries) && int.TryParse(maxRetries, out var r))
            options.MaxRetries = r;

        return options;
    }

    private static string? FindEnvFile()
    {
        var dir = Directory.GetCurrentDirectory();

        while (dir != null)
        {
            var envPath = Path.Combine(dir, ".env");
            if (File.Exists(envPath)) return envPath;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }
}

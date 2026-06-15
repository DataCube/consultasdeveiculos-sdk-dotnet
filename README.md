# ConsultasDeVeiculos SDK (.NET)

SDK .NET dinâmica para consultas de veículos, baseada em coleções Postman.

## 🚀 Visão Geral

Esta SDK funciona como um **Runtime Engine** que consome endpoints definidos em uma coleção Postman, sem necessidade de implementação manual de cada endpoint.

**Endpoints são gerados dinamicamente a partir da coleção Postman.**

### Características

- ✅ **100% Cross-platform**: Windows, Linux e macOS
- ✅ **Slug-based API**: Chamadas simples via `client.ExecuteAsync("veiculos_agregados")`
- ✅ **Dynamic support**: Use `dynamic` para chamadas similares ao JavaScript
- ✅ **Modo Sandbox**: Teste sem conexão com a API real
- ✅ **177 endpoints**: Gerados automaticamente da coleção Postman
- ✅ **CLI Completo**: Liste endpoints disponíveis
- ✅ **.NET 10**: Última versão do .NET

## 📦 Instalação

### Via NuGet

```bash
dotnet add package ConsultasDeVeiculos.SDK
```

### CLI Tool

```bash
dotnet tool install -g ConsultasDeVeiculos.SDK.Cli
```

## 🏁 Início Rápido

### Configuração via `.env`

Copie o arquivo `.env.example` para `.env` e configure:

```bash
cp .env.example .env
```

```env
# Token de autenticação da API (obrigatório em produção)
API_TOKEN=SEU_TOKEN_AQUI

# URL de download do postman.json (opcional)
DOWNLOAD_URL=https://painel.consultasdeveiculos.com/download-postman

# URL base da API (opcional)
API_BASE_URL=https://api.consultasdeveiculos.com

# Timeout das requisições em ms (opcional - padrão: 30000)
# API_TIMEOUT=30000

# Número máximo de retries (opcional - padrão: 3)
# API_MAX_RETRIES=3
```

Então use:

```csharp
using ConsultasDeVeiculos.SDK;

// Carrega .env automaticamente e cria o client
var client = ConsultadeveiculosSDK.FromEnv();

var resultado = await client.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});
```

### Modo Produção (via código)

```csharp
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

// Inicializa com token obrigatório
var client = new ConsultadeveiculosSDK(new SDKOptions
{
    AuthToken = "SEU_TOKEN_AQUI"
});

// Consulta usando o slug do endpoint
var resultado = await client.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});

Console.WriteLine(resultado.Data);
```

### Modo Sandbox

```csharp
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

// Via .env (modo sandbox)
var client = ConsultadeveiculosSDK.FromEnvSandbox();

// Ou via código
var client = new ConsultadeveiculosSDK(new SDKOptions
{
    Sandbox = true
});

// As chamadas retornam respostas simuladas
var resultado = await client.ExecuteAsync("veiculos_agregados", new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});

Console.WriteLine(resultado.Data); // Resposta de exemplo do Postman
```

### Híbrido: `.env` + opções manuais

```csharp
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;

// Carrega .env para variáveis de ambiente
DotEnv.Load();

// Cria options a partir do .env e complementa
var options = DotEnv.CreateOptionsFromEnvironment();
options.Timeout = 60000;  // Sobrescreve timeout

var client = new ConsultadeveiculosSDK(options);
```

### Usando Dynamic (similar ao JavaScript)

```csharp
var client = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
dynamic sdk = client.AsDynamic();

// Chamada idêntica ao JavaScript
var resultado = await sdk.veiculos_agregados(new { placa = "ABC1234" });
Console.WriteLine(resultado.Status);
```

### Usando Indexador

```csharp
var client = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });

var resultado = await client["veiculos_debitos_sp"].ExecuteAsync(new Dictionary<string, object?>
{
    ["placa"] = "ABC1234"
});
```

## 📖 API

### Inicialização

```csharp
// OPÇÃO 1: Via .env (recomendado)
var client = ConsultadeveiculosSDK.FromEnv();

// OPÇÃO 2: Via código
var client = new ConsultadeveiculosSDK(new SDKOptions
{
    AuthToken = "TOKEN",      // Obrigatório em produção
    Sandbox = false,          // Modo sandbox (padrão: false)
    BaseUrl = "URL",          // URL base customizada (opcional)
    Timeout = 30000,          // Timeout em ms (padrão: 30000)
    MaxRetries = 3            // Máximo de retries (padrão: 3)
});
```

### Variáveis de Ambiente (`.env`)

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `API_TOKEN` | Token de autenticação | (obrigatório) |
| `API_BASE_URL` | URL base da API | `https://api.consultasdeveiculos.com` |
| `DOWNLOAD_URL` | URL de download do postman.json | `https://painel.consultasdeveiculos.com/download-postman` |
| `API_TIMEOUT` | Timeout em ms | `30000` |
| `API_MAX_RETRIES` | Máximo de retries | `3` |

### Como Chamar Endpoints

O slug é derivado da URL do endpoint, substituindo `/` e `-` por `_`:

| URL do Endpoint | Slug para Chamar |
|-----------------|------------------|
| `/veiculos/agregados` | `ExecuteAsync("veiculos_agregados")` |
| `/veiculos/debitos-sp` | `ExecuteAsync("veiculos_debitos_sp")` |
| `/cnh/nacional/simples` | `ExecuteAsync("cnh_nacional_simples")` |
| `/pessoas/nome` | `ExecuteAsync("pessoas_nome")` |

### Métodos Disponíveis

| Método | Descrição |
|--------|-----------|
| `ExecuteAsync(slug, params)` | Executa endpoint pelo slug |
| `GetInfo()` | Informações do SDK |
| `ListEndpoints()` | Lista todos os endpoints |
| `ListSlugs()` | Lista apenas os slugs |
| `SearchEndpoints(pattern)` | Busca endpoints por padrão regex |
| `AsDynamic()` | Wrapper dinâmico para chamadas fluentes |

### Tratamento de Erros

```csharp
using ConsultasDeVeiculos.SDK.Errors;

try
{
    var result = await client.ExecuteAsync("veiculos_agregados", params);
}
catch (AuthenticationException ex)
{
    // Token inválido ou expirado
}
catch (ValidationException ex)
{
    // Dados de entrada inválidos
}
catch (RateLimitException ex)
{
    // Rate limit - aguardar ex.RetryAfter segundos
}
catch (EndpointNotFoundException ex)
{
    // Slug não encontrado
}
catch (SDKException ex)
{
    // Outro erro genérico da SDK
}
```

## 🖥️ CLI

```bash
# Lista todos os endpoints
consultas-de-veiculos-sdk endpoints

# Filtra endpoints
consultas-de-veiculos-sdk endpoints veiculos

# Com descrições
consultas-de-veiculos-sdk endpoints --verbose

# Versão
consultas-de-veiculos-sdk version

# Diagnóstico
consultas-de-veiculos-sdk doctor

# Atualizar especificação
consultas-de-veiculos-sdk update

# Limpar cache
consultas-de-veiculos-sdk clear-cache
```

## 🏗️ Estrutura do Projeto

```
ConsultasDeVeiculos.SDK/
├── src/
│   ├── ConsultasDeVeiculos.SDK/           # Biblioteca principal
│   │   ├── Core/                          # Classes core (Models, Config, Registry)
│   │   ├── Transport/                     # Camada de transporte (HTTP, Sandbox)
│   │   ├── Parser/                        # Parsers de coleção Postman
│   │   ├── Errors/                        # Exceções tipadas
│   │   ├── ConsultadeveiculosSDK.cs       # Classe principal
│   │   └── DynamicSDK.cs                  # Wrapper dinâmico
│   └── ConsultasDeVeiculos.SDK.Cli/       # Ferramenta CLI
├── tests/
│   └── ConsultasDeVeiculos.SDK.Tests/     # Testes unitários (xUnit)
├── examples/                              # Exemplos de uso
├── spec/                                  # Especificação Postman
└── ConsultasDeVeiculos.SDK.sln            # Solution
```

## 🔧 Desenvolvimento

### Pré-requisitos

- .NET 10 SDK ou superior

### Build

```bash
dotnet build
```

### Testes

```bash
dotnet test
```

### Executar CLI

```bash
dotnet run --project src/ConsultasDeVeiculos.SDK.Cli -- endpoints
```

## 📋 Compatibilidade

| Plataforma | Suporte |
|------------|---------|
| Windows x64 | ✅ |
| Windows ARM64 | ✅ |
| Linux x64 | ✅ |
| Linux ARM64 | ✅ |
| macOS x64 | ✅ |
| macOS ARM64 (Apple Silicon) | ✅ |

## 📄 Licença

MIT - DATACUBE SERVICO DE INFORMACAO VIA WEB LTDA

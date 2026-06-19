// Exemplo Interativo: Consulta completa de veículo e débitos por placa
using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsultasDeVeiculos.SDK.Examples;

public class InteractiveConsole
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        Console.WriteLine("=================================================================");
        Console.WriteLine("🚘 SDK Consultas de Veículos - Exemplo de Fluxo Completo 🚘");
        Console.WriteLine("=================================================================\n");

        // 1. Carrega as variáveis do arquivo .env
        DotEnv.Load();

        // 2. Prompt da placa e documento a serem consultados
        string? placa = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("TEST_PLACA");
        string? documento = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable("TEST_DOCUMENTO");

        if (string.IsNullOrEmpty(placa))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("👉 Digite a placa do veículo a ser consultado (ex: ABC1234): ");
            Console.ResetColor();
            placa = Console.ReadLine()?.Trim().ToUpper();
        }
        else
        {
            Console.WriteLine($"📋 Placa fornecida via argumento/ambiente: {placa}");
        }

        if (string.IsNullOrEmpty(placa))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Placa é obrigatória para executar as consultas.");
            Console.ResetColor();
            return;
        }

        // Normaliza a placa (remove espaços e hífens)
        placa = placa.Replace(" ", "").Replace("-", "").ToUpper();


        if (string.IsNullOrEmpty(documento))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("👉 Digite o documento (CPF/CNPJ, opcional mas exigido por algumas UFs como RJ, SP, etc.): ");
            Console.ResetColor();
            documento = Console.ReadLine()?.Trim();
        }
        else
        {
            Console.WriteLine($"📋 Documento fornecido via argumento/ambiente: {documento}");
        }

        // ==========================================
        // ETAPA I: Execução em Homologação (Sandbox)
        // ==========================================
        Console.WriteLine("\n-----------------------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🧪 ETAPA I: Executando em Homologação (Sandbox = true)");
        Console.ResetColor();
        Console.WriteLine("-----------------------------------------------------------------");

        var clientSandbox = new ConsultadeveiculosSDK(new SDKOptions 
        { 
            Sandbox = true,
            SandboxDelay = 50 
        });

        await ExecutarFluxoCompleto(clientSandbox, placa, documento, true);

        // ==========================================
        // ETAPA II: Execução em Produção
        // ==========================================
        Console.WriteLine("\n-----------------------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🔑 ETAPA II: Executando em Produção");
        Console.ResetColor();
        Console.WriteLine("-----------------------------------------------------------------");

        var envToken = Environment.GetEnvironmentVariable("API_TOKEN");
        if (string.IsNullOrEmpty(envToken))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Aviso: API_TOKEN não encontrado no ambiente/.env.");
            Console.WriteLine("Para rodar a produção de verdade, defina o token em .env.");
            Console.WriteLine("Criando cliente de produção com token simulado para demonstrar o tratamento de erro.");
            Console.ResetColor();
            
            // Define um token temporário se não houver no env para não quebrar a instanciação de produção
            Environment.SetEnvironmentVariable("API_TOKEN", "DUMMY_PRODUCTION_TOKEN");
        }

        try
        {
            var clientProduction = ConsultadeveiculosSDK.FromEnv();
            await ExecutarFluxoCompleto(clientProduction, placa, documento, false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro ao inicializar ou executar em Produção: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine("\n=================================================================");
        Console.WriteLine("✅ Execução do Exemplo Interativo Finalizada.");
        Console.WriteLine("=================================================================");
    }

    private static async Task ExecutarFluxoCompleto(ConsultadeveiculosSDK client, string placa, string? documentoFornecido, bool isSandbox)

    {
        dynamic sdk = client.AsDynamic();

        // 1. Consulta de Renavam usando somente a placa
        Console.WriteLine($"\n📡 1. Consultando RENAVAM para a placa: {placa}...");
        
        SDKResponse resultadoRenavam;
        try
        {
            // Usamos veiculos_renavam, pois o slug no spec do Postman é "veiculos_renavam"
            resultadoRenavam = await sdk.veiculos_renavam(new { placa = placa });
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Status HTTP: {resultadoRenavam.Status}");
            Console.ResetColor();
            Console.WriteLine("📋 Resultado da consulta de Renavam:");
            ImprimirJson(resultadoRenavam.Data);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Falha na consulta de Renavam: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // 2. Consulta de UF da placa
        Console.WriteLine($"\n📡 2. Consultando UF para a placa: {placa}...");
        SDKResponse resultadoUf;
        try
        {
            resultadoUf = await sdk.veiculos_uf_placa(new { placa = placa });
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Status HTTP: {resultadoUf.Status}");
            Console.ResetColor();
            Console.WriteLine("📋 Resultado da consulta de UF da placa:");
            ImprimirJson(resultadoUf.Data);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Falha na consulta de UF: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // 3. Extrair dados das respostas anteriores para a consulta de débitos
        string? renavam = ExtractString(resultadoRenavam.Data, "renavam");
        string? chassi = ExtractString(resultadoRenavam.Data, "chassi");
        string? uf = ExtractString(resultadoUf.Data, "uf_jurisdicao");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n🔍 Dados Extraídos:");
        Console.WriteLine($"   - Placa:   {placa}");
        Console.WriteLine($"   - Renavam: {renavam ?? "Não encontrado"}");
        Console.WriteLine($"   - Chassi:  {chassi ?? "Não encontrado"}");
        Console.WriteLine($"   - UF:      {uf ?? "Não encontrado"}");
        Console.ResetColor();

        // Tratamento especial para o modo Sandbox onde a UF retorna como "XX"
        if (isSandbox && (string.IsNullOrEmpty(uf) || uf == "XX" || uf.Contains("Indispon")))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("💡 [Sandbox] UF retornada é 'XX' ou 'Indisponível'. Usaremos 'SP' como UF padrão para demonstrar o fluxo de débitos.");
            Console.ResetColor();
            uf = "SP";
        }

        if (string.IsNullOrEmpty(renavam) || renavam.Contains("XXXX"))
        {
            if (isSandbox)
            {
                renavam = "123456789"; // Renavam fictício para sandbox funcionar
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Não foi possível obter um RENAVAM válido para prosseguir com a consulta de débitos.");
                Console.ResetColor();
                return;
            }
        }

        if (string.IsNullOrEmpty(uf))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Não foi possível obter uma UF válida para prosseguir com a consulta de débitos.");
            Console.ResetColor();
            return;
        }

        // 4. Mapear UF para a respectiva função de débitos
        string ufUpper = uf.ToUpper().Trim();
        string? debitEndpoint = MapUfToDebitEndpoint(ufUpper);

        if (string.IsNullOrEmpty(debitEndpoint))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ A UF '{ufUpper}' não possui um endpoint de débitos mapeado.");
            Console.ResetColor();
            return;
        }

        // Preparar parâmetros da requisição de débitos
        var debitParams = new Dictionary<string, object?>();
        
        // A maioria dos endpoints precisa de placa e renavam
        if (debitEndpoint != "debitos_pr")
        {
            debitParams["placa"] = placa;
        }
        debitParams["renavam"] = renavam;

        // Se SC e tiver chassi
        if (debitEndpoint == "debitos_sc" && !string.IsNullOrEmpty(chassi))
        {
            debitParams["chassi"] = chassi;
        }
        
        // Se a UF necessitar de documento
        var ufsQueExigemDocumento = new[] { "CE", "MA", "MT", "MS", "PB", "RJ", "RO", "TO" };
        if (ufsQueExigemDocumento.Contains(ufUpper))
        {
            string? documento = documentoFornecido;
            if (string.IsNullOrEmpty(documento))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"👉 A UF '{ufUpper}' exige o campo 'documento' (CPF/CNPJ). Digite o documento: ");
                Console.ResetColor();
                documento = Console.ReadLine()?.Trim();
            }
            
            if (string.IsNullOrEmpty(documento))
            {
                documento = "00000000000"; // Fallback fictício
            }
            debitParams["documento"] = documento;
        }

        Console.WriteLine($"\n📡 3. Executando consulta de débitos via endpoint: '{debitEndpoint}'...");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Parâmetros: {JsonSerializer.Serialize(debitParams)}");
        Console.ResetColor();

        try
        {
            SDKResponse resultadoDebitos = await client.ExecuteAsync(debitEndpoint, debitParams);
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Status HTTP: {resultadoDebitos.Status}");
            Console.ResetColor();
            Console.WriteLine($"📋 Resultado da consulta de débitos ({debitEndpoint}):");
            ImprimirJson(resultadoDebitos.Data);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Falha na consulta de débitos: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string? MapUfToDebitEndpoint(string uf)
    {
        return uf switch
        {
            "AP" => "debitos_ap",
            "AC" => "debitos_ac",
            "AL" => "debitos_al",
            "AM" => "debitos_am",
            "CE" => "debitos_ce",
            "DF" => "debitos_df",
            "ES" => "debitos_es",
            "GO" => "debitos_go",
            "MG" => "debitos_mg_simples",
            "MA" => "debitos_ma",
            "MT" => "debitos_mt",
            "MS" => "debitos_ms",
            "PA" => "debitos_pa",
            "PB" => "debitos_pb",
            "PR" => "debitos_pr",
            "PI" => "debitos_pi",
            "RJ" => "debitos_rj",
            "RN" => "debitos_rn",
            "RO" => "debitos_ro",
            "RR" => "debitos_rr",
            "SC" => "debitos_sc_v2", // debitos_sc_v2({ placa, renavam }), debitos_sc({ placa, renavam, chassi })
            "SP" => "debitos_sp",
            "TO" => "debitos_to",
            _ => null
        };
    }

    private static string? ExtractString(object? data, string key)
    {
        if (data is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Object)
                {
                    if (resultProp.TryGetProperty(key, out var valProp))
                    {
                        return valProp.ToString();
                    }
                }
                if (element.TryGetProperty(key, out var topValProp))
                {
                    return topValProp.ToString();
                }
            }
        }
        return null;
    }

    private static void ImprimirJson(object? data)
    {
        if (data == null)
        {
            Console.WriteLine("null");
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(data, options));
        }
        catch
        {
            Console.WriteLine(data.ToString());
        }
    }
}

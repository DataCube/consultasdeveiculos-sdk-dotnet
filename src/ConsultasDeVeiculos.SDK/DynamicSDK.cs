using System.Dynamic;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;

namespace ConsultasDeVeiculos.SDK;

/// <summary>
/// Wrapper dinâmico que permite chamadas como:
/// dynamic client = sdk.Dynamic;
/// var result = await client.veiculos_agregados(new { placa = "ABC1234" });
/// </summary>
public class DynamicSDK : DynamicObject
{
    private readonly ConsultadeveiculosSDK _sdk;

    internal DynamicSDK(ConsultadeveiculosSDK sdk)
    {
        _sdk = sdk;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var slug = binder.Name;

        Dictionary<string, object?>? parameters = null;
        if (args != null && args.Length > 0 && args[0] != null)
        {
            if (args[0] is Dictionary<string, object?> dict)
            {
                parameters = dict;
            }
            else
            {
                // Converte objeto anônimo em dicionário
                parameters = new Dictionary<string, object?>();
                foreach (var prop in args[0]!.GetType().GetProperties())
                {
                    parameters[prop.Name] = prop.GetValue(args[0]);
                }
            }
        }

        result = _sdk.ExecuteAsync(slug, parameters);
        return true;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        // Permite acessar como propriedade para obter o accessor
        result = new EndpointAccessor(_sdk, binder.Name);
        return true;
    }
}

/// <summary>
/// Extensões para ConsultadeveiculosSDK
/// </summary>
public static class ConsultadeveiculosSDKExtensions
{
    /// <summary>
    /// Obtém um wrapper dinâmico da SDK para chamadas fluentes
    /// </summary>
    public static dynamic AsDynamic(this ConsultadeveiculosSDK sdk)
    {
        return new DynamicSDK(sdk);
    }
}

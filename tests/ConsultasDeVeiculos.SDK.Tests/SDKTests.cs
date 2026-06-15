using ConsultasDeVeiculos.SDK;
using ConsultasDeVeiculos.SDK.Core;
using ConsultasDeVeiculos.SDK.Errors;
using Xunit;

namespace ConsultasDeVeiculos.SDK.Tests;

public class SDKTests
{
    [Fact]
    public void Should_Throw_When_No_Token_In_Production_Mode()
    {
        var ex = Assert.Throws<AuthenticationException>(() =>
            new ConsultadeveiculosSDK(new SDKOptions { AuthToken = null }));

        Assert.Contains("AuthToken é obrigatório", ex.Message);
    }

    [Fact]
    public void Should_Initialize_In_Sandbox_Mode()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });

        Assert.True(sdk.Initialized);
        Assert.True(sdk.IsSandbox);
    }

    [Fact]
    public void Should_Have_Endpoints()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var info = sdk.GetInfo();

        Assert.True(info.EndpointsCount > 0);
        Assert.True(info.SlugsCount > 0);
    }

    [Fact]
    public void Should_List_Endpoints()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var endpoints = sdk.ListEndpoints();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, ep =>
        {
            Assert.False(string.IsNullOrEmpty(ep.Slug));
            Assert.False(string.IsNullOrEmpty(ep.Name));
        });
    }

    [Fact]
    public void Should_List_Slugs()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var slugs = sdk.ListSlugs();

        Assert.NotEmpty(slugs);
        Assert.All(slugs, slug => Assert.False(string.IsNullOrEmpty(slug)));
    }

    [Fact]
    public void Should_Search_Endpoints()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var results = sdk.SearchEndpoints("veiculos");

        Assert.NotEmpty(results);
        Assert.All(results, ep =>
            Assert.True(
                ep.Slug.Contains("veiculos", StringComparison.OrdinalIgnoreCase) ||
                ep.Name.Contains("veiculos", StringComparison.OrdinalIgnoreCase) ||
                ep.Name.Contains("Veículos", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Should_Execute_Endpoint_In_Sandbox()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var slugs = sdk.ListSlugs();
        var firstSlug = slugs.First();

        var result = await sdk.ExecuteAsync(firstSlug, new Dictionary<string, object?>
        {
            ["placa"] = "ABC1234"
        });

        Assert.True(result.Success);
        Assert.Equal(200, result.Status);
        Assert.True(result.Sandbox);
    }

    [Fact]
    public async Task Should_Throw_For_Unknown_Endpoint()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });

        await Assert.ThrowsAsync<EndpointNotFoundException>(
            () => sdk.ExecuteAsync("endpoint_que_nao_existe"));
    }

    [Fact]
    public void Should_Return_SDK_Info()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var info = sdk.GetInfo();

        Assert.Equal(ConsultadeveiculosSDK.VERSION, info.RuntimeVersion);
        Assert.True(info.Sandbox);
        Assert.NotEmpty(info.Namespaces);
    }

    [Fact]
    public void UrlToSlug_Should_Convert_Correctly()
    {
        Assert.Equal("veiculos_debitos_sp", ConsultadeveiculosSDK.UrlToSlug("/veiculos/debitos-sp"));
        Assert.Equal("cnh_nacional_simples", ConsultadeveiculosSDK.UrlToSlug("/cnh/nacional/simples"));
        Assert.Equal("veiculos_agregados", ConsultadeveiculosSDK.UrlToSlug("https://api.com/veiculos/agregados"));
        Assert.Null(ConsultadeveiculosSDK.UrlToSlug(""));
        Assert.Null(ConsultadeveiculosSDK.UrlToSlug(null!));
    }

    [Fact]
    public async Task Should_Support_Indexer_Access()
    {
        var sdk = new ConsultadeveiculosSDK(new SDKOptions { Sandbox = true });
        var slugs = sdk.ListSlugs();
        var firstSlug = slugs.First();

        var result = await sdk[firstSlug].ExecuteAsync(new Dictionary<string, object?>
        {
            ["placa"] = "TEST1234"
        });

        Assert.True(result.Success);
        Assert.True(result.Sandbox);
    }
}

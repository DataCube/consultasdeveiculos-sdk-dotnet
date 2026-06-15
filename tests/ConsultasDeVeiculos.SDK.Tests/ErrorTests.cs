using ConsultasDeVeiculos.SDK.Errors;
using Xunit;

namespace ConsultasDeVeiculos.SDK.Tests;

public class ErrorTests
{
    [Fact]
    public void SDKException_Should_Sanitize_Sensitive_Data()
    {
        var details = new Dictionary<string, object?>
        {
            ["auth_token"] = "secret_value",
            ["placa"] = "ABC1234"
        };

        var ex = new SDKException("test", "TEST_ERROR", details);

        Assert.Equal("[REDACTED]", ex.Details!["auth_token"]);
        Assert.Equal("ABC1234", ex.Details["placa"]);
    }

    [Fact]
    public void SDKException_Should_Have_Timestamp()
    {
        var ex = new SDKException("test message");

        Assert.NotNull(ex.Timestamp);
        Assert.Equal("SDK_ERROR", ex.Code);
    }

    [Fact]
    public void AuthenticationException_Should_Have_Correct_Code()
    {
        var ex = new AuthenticationException("Token inválido");

        Assert.Equal("AUTHENTICATION_ERROR", ex.Code);
        Assert.Equal("Token inválido", ex.Message);
    }

    [Fact]
    public void ValidationException_Should_Have_Correct_Code()
    {
        var ex = new ValidationException("Campo obrigatório");

        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public void RateLimitException_Should_Have_RetryAfter()
    {
        var details = new Dictionary<string, object?> { ["retryAfter"] = 60 };
        var ex = new RateLimitException("Limite excedido", details);

        Assert.Equal("RATE_LIMIT_ERROR", ex.Code);
        Assert.Equal(60, ex.RetryAfter);
    }

    [Fact]
    public void SDKException_ToSerializable_Should_Not_Include_StackTrace()
    {
        var ex = new SDKException("test");
        var json = ex.ToSerializable();

        Assert.False(json.ContainsKey("stackTrace"));
        Assert.True(json.ContainsKey("message"));
        Assert.True(json.ContainsKey("code"));
    }
}

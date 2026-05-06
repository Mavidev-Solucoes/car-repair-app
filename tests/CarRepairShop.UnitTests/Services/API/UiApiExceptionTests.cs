using CarRepairShop.API.Services;

namespace CarRepairShop.UnitTests.Services.API;

public class UiApiExceptionTests
{
    private static readonly string[] NameIsRequiredError = ["Name is required."];
    [Fact]
    public void Constructor_SetsMessageAndStatusCode()
    {
        var ex = new UiApiException("Bad Request", 400);

        Assert.Equal("Bad Request", ex.Message);
        Assert.Equal(400, ex.StatusCode);
        Assert.Null(ex.Errors);
    }

    [Fact]
    public void Constructor_SetsErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", NameIsRequiredError }
        };
        var ex = new UiApiException("Validation failed", 422, errors);

        Assert.Equal("Validation failed", ex.Message);
        Assert.Equal(422, ex.StatusCode);
        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors!);
        Assert.Equal(NameIsRequiredError, ex.Errors!["Name"]);
    }

    [Fact]
    public void Constructor_WithoutErrors_ErrorsIsNull()
    {
        var ex = new UiApiException("Not found", 404);

        Assert.Null(ex.Errors);
    }
}

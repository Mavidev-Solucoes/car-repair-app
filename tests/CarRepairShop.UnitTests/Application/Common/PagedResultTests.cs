using CarRepairShop.Application.Common;

namespace CarRepairShop.UnitTests.Application.Common;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_WhenPageSizeIsZero_ReturnsZero()
    {
        var result = new PagedResult<string>(["a", "b"], 2, 1, 0);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenTotalCountFitsInOnePage_ReturnsOne()
    {
        var result = new PagedResult<string>(["a", "b"], 2, 1, 10);

        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenTotalCountSpansMultiplePages_ReturnsCorrectValue()
    {
        var result = new PagedResult<string>([], 25, 1, 10);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenTotalCountIsZero_ReturnsZero()
    {
        var result = new PagedResult<string>([], 0, 1, 10);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var result = new PagedResult<string>();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.Page);
        Assert.Equal(0, result.PageSize);
    }
}

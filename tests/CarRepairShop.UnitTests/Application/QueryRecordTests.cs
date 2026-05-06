using CarRepairShop.Application.ServiceOrders.Queries;
using CarRepairShop.Application.Users.Queries;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.Application;

/// <summary>
/// Exercises record equality, GetHashCode, and ToString for query record types
/// that have no properties (empty records) or default-valued properties.
/// </summary>
public class QueryRecordTests
{
    // ── GetAllServiceOrdersQuery ───────────────────────────────────────────────

    [Fact]
    public void GetAllServiceOrdersQuery_TwoInstances_AreEqual()
    {
        var a = new GetAllServiceOrdersQuery();
        var b = new GetAllServiceOrdersQuery();

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void GetAllServiceOrdersQuery_NullComparison_NotEqual()
    {
        var a = new GetAllServiceOrdersQuery();

        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
    }

    [Fact]
    public void GetAllServiceOrdersQuery_GetHashCode_SameForEqualInstances()
    {
        var a = new GetAllServiceOrdersQuery();
        var b = new GetAllServiceOrdersQuery();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetAllServiceOrdersQuery_ToString_ContainsTypeName()
    {
        var q = new GetAllServiceOrdersQuery();
        Assert.Contains("GetAllServiceOrdersQuery", q.ToString());
    }

    [Fact]
    public void GetAllServiceOrdersQuery_WithExpression_CreatesNewInstance()
    {
        var a = new GetAllServiceOrdersQuery();
        var b = a with { };

        Assert.Equal(a, b);
        Assert.NotSame(a, b);
    }

    // ── GetServiceOrderByIdQuery ───────────────────────────────────────────────

    [Fact]
    public void GetServiceOrderByIdQuery_EqualityAndHashCode()
    {
        var id = Guid.NewGuid();
        var a = new GetServiceOrderByIdQuery(id);
        var b = new GetServiceOrderByIdQuery(id);
        var c = new GetServiceOrderByIdQuery(Guid.NewGuid());

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a == b);
        Assert.False(a == c);
    }

    // ── GetAllUsersQuery ───────────────────────────────────────────────────────

    [Fact]
    public void GetAllUsersQuery_TwoInstances_AreEqual()
    {
        var a = new GetAllUsersQuery();
        var b = new GetAllUsersQuery();

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void GetAllUsersQuery_NullComparison_NotEqual()
    {
        var a = new GetAllUsersQuery();

        Assert.False(a.Equals(null));
        Assert.False(a == null);
    }

    [Fact]
    public void GetAllUsersQuery_GetHashCode_SameForEqualInstances()
    {
        var a = new GetAllUsersQuery();
        var b = new GetAllUsersQuery();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetAllUsersQuery_ToString_ContainsTypeName()
    {
        var q = new GetAllUsersQuery();
        Assert.Contains("GetAllUsersQuery", q.ToString());
    }

    [Fact]
    public void GetAllUsersQuery_WithExpression_CreatesNewInstance()
    {
        var a = new GetAllUsersQuery();
        var b = a with { };

        Assert.Equal(a, b);
        Assert.NotSame(a, b);
    }

    // ── GetUsersQuery ──────────────────────────────────────────────────────────

    [Fact]
    public void GetUsersQuery_DefaultValues()
    {
        var q = new GetUsersQuery();

        Assert.Equal(1, q.Page);
        Assert.Equal(10, q.PageSize);
        Assert.Null(q.Search);
        Assert.Null(q.Role);
        Assert.Null(q.IsActive);
    }

    [Fact]
    public void GetUsersQuery_EqualityWithAllProperties()
    {
        var a = new GetUsersQuery { Page = 2, PageSize = 20, Search = "Alice", Role = UserRole.Admin, IsActive = true };
        var b = new GetUsersQuery { Page = 2, PageSize = 20, Search = "Alice", Role = UserRole.Admin, IsActive = true };
        var c = new GetUsersQuery { Page = 2, PageSize = 20, Search = "Bob", Role = UserRole.Admin, IsActive = true };

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetUsersQuery_WithNullOptionals_EqualToDefault()
    {
        var a = new GetUsersQuery { Page = 1, PageSize = 10, Search = null, Role = null, IsActive = null };
        var b = new GetUsersQuery();

        Assert.Equal(a, b);
    }

    [Fact]
    public void GetUsersQuery_WithExpression_ChangesProperty()
    {
        var original = new GetUsersQuery { Page = 1, PageSize = 10 };
        var modified = original with { Page = 3 };

        Assert.Equal(3, modified.Page);
        Assert.Equal(10, modified.PageSize);
    }
}

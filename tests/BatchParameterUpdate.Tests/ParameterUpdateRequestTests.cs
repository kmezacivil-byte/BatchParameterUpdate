using BatchParameterUpdate.Core;
using Xunit;

namespace BatchParameterUpdate.Tests;

public class ParameterUpdateRequestTests
{
    [Theory]
    [InlineData("Comments")]
    [InlineData("  Comments  ")]
    public void IsValid_true_for_non_blank_name(string name)
    {
        var request = new ParameterUpdateRequest(name, "some value");

        Assert.True(request.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_false_for_blank_name(string? name)
    {
        var request = new ParameterUpdateRequest(name!, "some value");

        Assert.False(request.IsValid);
    }

    [Fact]
    public void Empty_new_value_is_valid_clearing_a_parameter_is_a_legitimate_case()
    {
        var request = new ParameterUpdateRequest("Comments", "");

        Assert.True(request.IsValid);
    }

    [Fact]
    public void TrimmedParameterName_removes_surrounding_whitespace()
    {
        var request = new ParameterUpdateRequest("  Comments  ", "value");

        Assert.Equal("Comments", request.TrimmedParameterName);
    }
}

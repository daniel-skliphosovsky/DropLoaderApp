using System.Globalization;
using DropLoaderApp.Converters;
using FluentAssertions;

namespace DropLoaderApp.Tests.Converters;

public class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _converter = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_ShouldInvert(bool input, bool expected)
    {
        var result = _converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConvertBack_ShouldInvert(bool input, bool expected)
    {
        var result = _converter.ConvertBack(input, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_NonBool_ReturnsFalse()
    {
        var result = _converter.Convert("string", typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }
}

using System.Globalization;
using DropLoaderApp.Converters;
using FluentAssertions;

namespace DropLoaderApp.Tests.Converters;

public class StringNotEmptyConverterTests
{
    private readonly StringNotEmptyConverter _converter = new();

    [Theory]
    [InlineData("https://tiktok.com", true)]
    [InlineData("abc", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public void Convert_ShouldCheckNotEmpty(string? input, bool expected)
    {
        var result = _converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }
}

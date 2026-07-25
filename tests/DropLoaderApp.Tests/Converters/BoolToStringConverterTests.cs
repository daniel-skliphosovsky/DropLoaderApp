using System.Globalization;
using DropLoaderApp.Converters;
using FluentAssertions;

namespace DropLoaderApp.Tests.Converters;

public class BoolToStringConverterTests
{
    private readonly BoolToStringConverter _converter = new();

    [Theory]
    [InlineData(true, "Downloading...", "Downloading...")]
    [InlineData(false, "Downloading...", "Download")]
    [InlineData(true, null, "Download")]
    public void Convert_ShouldReturnCorrectString(bool input, string? parameter, string expected)
    {
        var result = _converter.Convert(input, typeof(string), parameter, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Action act = () => _converter.ConvertBack(true, typeof(string), null, CultureInfo.InvariantCulture);
        act.Should().Throw<NotSupportedException>();
    }
}

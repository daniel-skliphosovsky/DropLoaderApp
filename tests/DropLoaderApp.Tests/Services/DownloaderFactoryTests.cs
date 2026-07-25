using DropLoaderApp.Services.Downloaders;
using DropLoaderApp.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace DropLoaderApp.Tests.Services;

#pragma warning disable CS8604
#pragma warning disable xUnit1012

public class DownloaderFactoryTests
{
    private static DownloaderFactory CreateFactory()
    {
        var tikTok = new Mock<IDownloader>();
        tikTok.Setup(d => d.CanHandle(It.Is<string>(s => s != null && (s.Contains("tiktok.com") || s.Contains("vm.tiktok.com"))))).Returns(true);
        tikTok.SetupGet(d => d.PlatformName).Returns("TikTok");

        var youTube = new Mock<IDownloader>();
        youTube.Setup(d => d.CanHandle(It.Is<string>(s => s != null && (s.Contains("youtube.com") || s.Contains("youtu.be"))))).Returns(true);
        youTube.SetupGet(d => d.PlatformName).Returns("YouTube");

        var soundCloud = new Mock<IDownloader>();
        soundCloud.Setup(d => d.CanHandle(It.Is<string>(s => s != null && s.Contains("soundcloud.com")))).Returns(true);
        soundCloud.SetupGet(d => d.PlatformName).Returns("SoundCloud");

        return new DownloaderFactory(new[] { tikTok.Object, youTube.Object, soundCloud.Object });
    }

    [Theory]
    [InlineData("https://www.tiktok.com/@user/video/123", "TikTok")]
    [InlineData("https://vm.tiktok.com/ABC123", "TikTok")]
    [InlineData("https://tiktok.com/@user/photo/456", "TikTok")]
    [InlineData("https://youtube.com/watch?v=abc123", "YouTube")]
    [InlineData("https://youtu.be/abc123", "YouTube")]
    [InlineData("https://www.youtube.com/watch?v=xyz", "YouTube")]
    [InlineData("https://soundcloud.com/user/track-name", "SoundCloud")]
    [InlineData("https://soundcloud.com/user/sets/playlist", "SoundCloud")]
    [InlineData("https://instagram.com/p/ABC", "Unknown")]
    [InlineData("https://example.com", "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    public void GetPlatformName_VariousUrls_ReturnsCorrectPlatform(string url, string expected)
    {
        var factory = CreateFactory();
        var platform = factory.GetPlatformName(url);
        platform.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://www.tiktok.com/@user/video/123", true)]
    [InlineData("https://youtube.com/watch?v=abc", true)]
    [InlineData("https://soundcloud.com/user/track", true)]
    [InlineData("https://example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CanDownload_VariousUrls_ReturnsCorrectResult(string url, bool expected)
    {
        var factory = CreateFactory();
        var result = factory.CanDownload(url);
        result.Should().Be(expected);
    }
}

#pragma warning restore CS8604

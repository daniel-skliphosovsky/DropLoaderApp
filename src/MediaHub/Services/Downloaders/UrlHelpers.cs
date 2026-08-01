namespace MediaHub.Services.Downloaders;

internal static class UrlHelpers
{
    /// <summary>
    /// Returns the lowercased host of the URL, tolerating a missing scheme
    /// (e.g. "tiktok.com/@user/video/123" or "vm.tiktok.com/xyz").
    /// </summary>
    private static string? GetHost(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Media links never contain legit whitespace; dropping it also
        // tolerates stray newlines or spaces around the pasted URL.
        var value = new string(url.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate("https://" + value, UriKind.Absolute, out uri))
            return null;

        return uri.Host.ToLowerInvariant();
    }

    /// <summary>
    /// Normalized host of the URL, or an empty string when it cannot be parsed.
    /// Cheap enough to call on every keystroke; used to detect domain changes.
    /// </summary>
    public static string GetDomain(string url) => GetHost(url) ?? string.Empty;

    /// <summary>
    /// Checks whether the URL belongs to any of the given domains.
    /// Matches the bare domain and subdomains (www., m., short links like vm./youtu.be).
    /// </summary>
    public static bool UrlBelongsTo(string url, params string[] domains)
    {
        var host = GetHost(url);
        if (host is null)
            return false;

        foreach (var domain in domains)
        {
            var d = domain.ToLowerInvariant();
            if (host == d || host.EndsWith("." + d, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}

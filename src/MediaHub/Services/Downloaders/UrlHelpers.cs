using System.Text.RegularExpressions;

namespace MediaHub.Services.Downloaders;

internal static class UrlHelpers
{
    // VK video ids are <oid>_<vid>; negative oids (community videos) carry a
    // leading minus, e.g. video-145052891_456247130.
    private static readonly Regex VkResourcePath = new(@"^(video|clip)-?\d+_\d+");

    /// <summary>
    /// Returns the lowercased host of the URL, tolerating a missing scheme
    /// (e.g. "tiktok.com/@user/video/123" or "vm.tiktok.com/xyz").
    /// </summary>
    private static string? GetHost(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Media links never contain legit whitespace; dropping it also
        // tolerates loose newlines or spaces around the pasted URL.
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

    /// <summary>
    /// True when the URL query contains the given parameter with a non-empty
    /// value (e.g. "v=abc" but not a bare "v" or "v=").
    /// </summary>
    private static bool HasQueryParam(string query, string name)
    {
        if (string.IsNullOrEmpty(query))
            return false;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0 && pair.Length > eq + 1 && pair[..eq] == name)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Heuristic: does the URL point at an actual media item (video, track,
    /// playlist) rather than a bare domain or a profile/search page? A naked
    /// "youtube.com" must not enable the download button or show a preview
    /// card, only links that carry a real resource id should. The platform
    /// libraries still confirm it during parsing; this only gates the early
    /// UI state.
    /// </summary>
    public static bool LooksLikeContentUrl(string url)
    {
        var value = string.IsNullOrWhiteSpace(url) ? string.Empty : url.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate("https://" + value, UriKind.Absolute, out uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.Trim('/');

        // YouTube: youtu.be short links carry the id in the path; watch/shorts/
        // embed/live pages and playlists (list=...) are real resources.
        if (host == "youtu.be" || host.EndsWith(".youtu.be", StringComparison.Ordinal))
            return path.Length > 0;

        if (host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal))
        {
            if (path.StartsWith("shorts/", StringComparison.Ordinal) ||
                path.StartsWith("embed/", StringComparison.Ordinal) ||
                path.StartsWith("live/", StringComparison.Ordinal))
                return true;

            // watch/playlist pages only count with their id parameter in the
            // query: bare "youtube.com/watch" or "youtube.com/playlist" is not
            // a media resource.
            if ((path.StartsWith("watch", StringComparison.Ordinal) && HasQueryParam(uri.Query, "v")) ||
                (path.StartsWith("playlist", StringComparison.Ordinal) && HasQueryParam(uri.Query, "list")))
                return true;

            return uri.Query.Contains("list=", StringComparison.OrdinalIgnoreCase);
        }

        // SoundCloud: on.soundcloud.com short links are tracks; full links
        // need at least two path segments (user/track or user/sets/name).
        if (host == "on.soundcloud.com" || host.EndsWith(".on.soundcloud.com", StringComparison.Ordinal))
            return path.Length > 0;

        if (host == "soundcloud.com" || host.EndsWith(".soundcloud.com", StringComparison.Ordinal))
            return path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length >= 2;

        // TikTok: short links carry the id in the path; full links need a
        // /@user/video/ or /@user/photo/ resource, a bare profile is not media.
        if (host == "tiktok.com" || host.EndsWith(".tiktok.com", StringComparison.Ordinal))
        {
            if ((host == "vm.tiktok.com" || host == "vt.tiktok.com") && path.Length > 0)
                return true;

            return path.Contains("/video/", StringComparison.Ordinal) ||
                   path.Contains("/photo/", StringComparison.Ordinal) ||
                   path.StartsWith("video/", StringComparison.Ordinal);
        }

        // VK: /video<oid>_<vid> (and clips) are the media resources; a bare
        // vk.com or a profile page is not.
        if (host == "vk.com" || host == "m.vk.com" ||
            host == "vkvideo.ru" || host == "m.vkvideo.ru" ||
            host.EndsWith(".vk.com", StringComparison.Ordinal) ||
            host.EndsWith(".vkvideo.ru", StringComparison.Ordinal))
        {
            // vk.com/video and vkvideo.ru/video are the video section (or the
            // main page), not media items; only video<oid>_<vid> and
            // clip<oid>_<vid> paths carry a real resource id.
            return VkResourcePath.IsMatch(path);
        }

        return false;
    }
}

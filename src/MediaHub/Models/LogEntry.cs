namespace MediaHub.Models;

/// <summary>
/// One row in the bottom activity log: a message with a status kind, the
/// platform it belongs to (when relevant) and the time it happened.
/// </summary>
public sealed class LogEntry
{
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// "success", "error" or "muted"; drives the row color and icon.
    /// </summary>
    public string Kind { get; init; } = "muted";

    /// <summary>
    /// "tiktok", "youtube", "soundcloud", "vk" or empty for generic rows.
    /// </summary>
    public string PlatformKey { get; init; } = string.Empty;

    /// <summary>
    /// "HH:mm:ss" of when the entry was created.
    /// </summary>
    public string TimeText { get; init; } = string.Empty;

    /// <summary>
    /// SVG path data for the row icon (platform logo or status glyph),
    /// resolved at creation time so the template stays a single Path.
    /// </summary>
    public string IconData { get; init; } = string.Empty;
}

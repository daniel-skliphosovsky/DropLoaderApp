namespace MediaHub.Services.Downloaders;

/// <summary>
/// Extracts the first balanced JSON value (object or array) that appears
/// after the given anchor text, e.g. the object behind `"progressive"`.
/// Understands string escapes, so braces inside values do not break it.
/// </summary>
internal static class JsonExtractor
{
    public static bool TryExtract(string html, string anchor, out string json)
    {
        json = string.Empty;

        var start = html.IndexOf(anchor, StringComparison.Ordinal);
        if (start < 0)
            return false;

        var open = html.IndexOfAny(['{', '['], start);
        if (open < 0)
            return false;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = open; i < html.Length; i++)
        {
            var c = html[i];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                case '[':
                    depth++;
                    break;
                case '}':
                case ']':
                    depth--;
                    if (depth == 0)
                    {
                        json = html[open..(i + 1)];
                        return true;
                    }
                    break;
            }
        }

        return false;
    }
}

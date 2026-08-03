namespace MediaHub.Models;

/// <summary>
/// One labeled metadata row shown in the expandable "Information" section of
/// the preview card. Value is always the final display text.
/// </summary>
public readonly record struct ResourceDetail(string Label, string Value)
{
    /// <summary>
    /// Appends the row only when the value is not blank, so platforms that
    /// expose no extra metadata simply produce no rows.
    /// </summary>
    public static void AddIfPresent(List<ResourceDetail> list, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            list.Add(new ResourceDetail(label, value));
    }
}

using System.Collections.Generic;

public static class TagUtility
{
    // Returns true if the tag list contains the specified tag (case-insensitive).
    public static bool HasTag(List<string> tags, string tag)
    {
        if (tags == null || string.IsNullOrEmpty(tag)) return false;
        foreach (var t in tags)
            if (string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // Returns true if the tag list contains ANY of the tags in the query list.
    // Used by soil compatibility checks, fertilizer filters, and future systems.
    public static bool HasAnyTag(List<string> tags, List<string> query)
    {
        if (tags == null || query == null || query.Count == 0) return false;
        foreach (var q in query)
            if (HasTag(tags, q)) return true;
        return false;
    }
}

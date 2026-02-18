using System.Text.RegularExpressions;

/// <summary>
/// Utility to normalize item IDs for comparisons (case/whitespace/"(Clone)" tolerant).
/// </summary>
public static class ItemIdUtils
{
    private static readonly Regex _whitespace = new Regex(@"\s+", RegexOptions.Compiled);
    private static readonly Regex _cloneSuffix = new Regex(@"\s*\(clone\)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _underscores = new Regex(@"_+", RegexOptions.Compiled);

    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var s = raw.Trim();

        // Remove "(Clone)" at the end (Unity instantiation suffix)
        s = _cloneSuffix.Replace(s, "");

        // Convert whitespace to single underscore
        s = _whitespace.Replace(s, "_");

        // Collapse multiple underscores
        s = _underscores.Replace(s, "_");

        s = s.Trim('_');

        return s.ToLowerInvariant();
    }
}

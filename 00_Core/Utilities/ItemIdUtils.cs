using System.Text.RegularExpressions;

public static class ItemIdUtils
{
    // Нормализуем ID для сравнения (UX: " carrot ", "Carrot", "CARROT(Clone)")
    public static string Normalize(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        id = id.Trim().ToLowerInvariant();
        id = id.Replace("(clone)", "").Trim();
        id = Regex.Replace(id, @"\s+", "_");      // пробелы -> _
        id = Regex.Replace(id, @"_+", "_");       // __ -> _
        return id;
    }
}
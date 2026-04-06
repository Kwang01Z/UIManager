
public static class TextUtility
{
    public static string GetI2(string term)
    {
        return term;
    }

    public static bool IsNullOrWhitespace(this string term)
    {
        return string.IsNullOrEmpty(term) || string.IsNullOrWhiteSpace(term);
    }
}

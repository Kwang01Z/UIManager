using I2.Loc;

public static class TextUtility
{
    public static string GetI2(string term)
    {
        try
        {
            var translation = LocalizationManager.GetTranslation(term, false);
            return string.IsNullOrEmpty(translation) ? term : translation;
        }
        catch
        {
            return term;
        }
    }
}

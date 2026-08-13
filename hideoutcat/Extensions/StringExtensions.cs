using JetBrains.Annotations;

namespace HideoutCat.Extensions;

public static class StringExtensions
{
    [UsedImplicitly]
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
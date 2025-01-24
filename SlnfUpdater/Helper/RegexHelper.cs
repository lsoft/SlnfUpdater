using System;
using System.Text.RegularExpressions;

namespace SlnfUpdater.Helper
{
    public static class RegexHelper
    {
        public static string WildCardToRegular(
            this string value
            )
        {
            ArgumentNullException.ThrowIfNull(value);

            return
                "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}

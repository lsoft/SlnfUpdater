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

        /// <summary>
        /// Matches a full path against a wildcard; separators in the wildcard may be of any style,
        /// case sensitivity follows the OS file system.
        /// </summary>
        public static bool MatchesWildcard(
            this string fullPath,
            string wildcard
            )
        {
            ArgumentNullException.ThrowIfNull(fullPath);
            ArgumentNullException.ThrowIfNull(wildcard);

            var options = OperatingSystem.IsWindows()
                ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                : RegexOptions.CultureInvariant;

            return Regex.IsMatch(
                fullPath,
                wildcard.ToOsSeparators().WildCardToRegular(),
                options
                );
        }
    }
}

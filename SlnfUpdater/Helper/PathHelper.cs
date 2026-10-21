using System;
using System.IO;

namespace SlnfUpdater.Helper
{
    public static class PathHelper
    {
        /// <summary>
        /// Comparer for full file paths: Windows file system is case-insensitive.
        /// </summary>
        public static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>
        /// slnf files written by Visual Studio always use backslashes.
        /// </summary>
        public static string ToSlnfSeparators(
            this string path
            )
        {
            return path.Replace('/', '\\');
        }

        public static string ToOsSeparators(
            this string path
            )
        {
            return path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}

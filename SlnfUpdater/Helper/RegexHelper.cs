using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SlnfUpdater.Helper
{
    public static class RegexHelper
    {
        public static string WildCardToRegular(
            this string value
            )
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return
                "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

    }
}

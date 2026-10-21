using System;
using System.Collections.Generic;

namespace SlnfUpdater.Helper
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// Joins messages of the exception and all its inner exceptions.
        /// </summary>
        public static string BuildMessageChain(
            this Exception exception
            )
        {
            ArgumentNullException.ThrowIfNull(exception);

            var messages = new List<string>();
            for (var e = exception; e is not null; e = e.InnerException)
            {
                messages.Add(e.Message);
            }

            return string.Join(Environment.NewLine + " ---> ", messages);
        }
    }
}

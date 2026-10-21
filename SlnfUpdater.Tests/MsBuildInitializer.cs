using System;
using System.Runtime.CompilerServices;

namespace SlnfUpdater.Tests
{
    internal static class MsBuildInitializer
    {
        /// <summary>
        /// MSBuild must be registered before any test touches Microsoft.Build types.
        /// </summary>
        [ModuleInitializer]
        internal static void Initialize()
        {
            if (!MsBuildRegistration.TryRegister(out var error))
            {
                throw new InvalidOperationException(error);
            }
        }
    }
}

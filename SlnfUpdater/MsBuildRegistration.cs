using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Locator;

namespace SlnfUpdater
{
    public static class MsBuildRegistration
    {
        /// <summary>
        /// Registers MSBuild from the installed .NET SDK.
        /// Must be called before any Microsoft.Build type is touched.
        /// </summary>
        public static bool TryRegister(
            [NotNullWhen(false)] out string? error
            )
        {
            error = null;

            if (MSBuildLocator.IsRegistered)
            {
                return true;
            }

            try
            {
                MSBuildLocator.RegisterDefaults();
                return true;
            }
            catch (Exception excp)
            {
                error = $"Failed to locate MSBuild, make sure .NET SDK is installed: {excp.Message}";
                return false;
            }
        }
    }
}

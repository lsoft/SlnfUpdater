using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SlnfUpdater
{
    public sealed class CommandLineOptions
    {
        public const string RebuildFromRootsKey = "-rebuild-from-roots";
        public const string AdditionalRootsKey = "-additional-roots:";

        public const string Usage = $"""
            Usage: SlnfUpdater "path to your slnf folder" "mask for your slnf files" [{RebuildFromRootsKey} {AdditionalRootsKey}<wildcard>[;<wildcard>...]]
            """;

        public string SlnfFolderPath
        {
            get;
        }

        public string SlnfFileMask
        {
            get;
        }

        public BuildFromRootsMode FromRootsMode
        {
            get;
        }

        private CommandLineOptions(
            string slnfFolderPath,
            string slnfFileMask,
            BuildFromRootsMode fromRootsMode
            )
        {
            SlnfFolderPath = slnfFolderPath;
            SlnfFileMask = slnfFileMask;
            FromRootsMode = fromRootsMode;
        }

        public static bool TryParse(
            string[]? args,
            [NotNullWhen(true)] out CommandLineOptions? options,
            [NotNullWhen(false)] out string? error
            )
        {
            options = null;

            if (args is null || args.Length < 2)
            {
                error = "You need to provide at least 2 arguments: path to the root directory and slnf file mask!";
                return false;
            }

            var rebuildFromRoots = false;
            var additionalRootsKeyFound = false;
            var roots = new List<string>();

            for (var ai = 2; ai < args.Length; ai++)
            {
                var arg = args[ai];

                if (string.Equals(arg, RebuildFromRootsKey, StringComparison.Ordinal))
                {
                    rebuildFromRoots = true;
                }
                else if (arg.StartsWith(AdditionalRootsKey, StringComparison.Ordinal))
                {
                    additionalRootsKeyFound = true;
                    roots.AddRange(
                        arg[AdditionalRootsKey.Length..].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        );
                }
                else
                {
                    //an unknown (e.g. mistyped) key must not be silently ignored
                    error = $"Unknown argument: {arg}";
                    return false;
                }
            }

            if (additionalRootsKeyFound && !rebuildFromRoots)
            {
                error = $"{AdditionalRootsKey} can be used only together with {RebuildFromRootsKey}.";
                return false;
            }

            if (rebuildFromRoots && roots.Count == 0)
            {
                //otherwise all projects would be removed from slnf
                error = $"{RebuildFromRootsKey} requires at least one root wildcard defined with {AdditionalRootsKey}.";
                return false;
            }

            options = new CommandLineOptions(
                args[0],
                args[1],
                rebuildFromRoots
                    ? BuildFromRootsMode.WithAdditionalRoots(roots)
                    : BuildFromRootsMode.Disabled
                );
            error = null;
            return true;
        }
    }
}

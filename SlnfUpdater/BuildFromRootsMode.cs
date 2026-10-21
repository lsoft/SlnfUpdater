using System;
using System.Collections.Generic;

namespace SlnfUpdater
{
    public sealed class BuildFromRootsMode
    {
        public static readonly BuildFromRootsMode Disabled = new();
        public static BuildFromRootsMode WithAdditionalRoots(List<string> additionalRootWildcards) => new(additionalRootWildcards);

        public bool IsEnabled
        {
            get;
        }

        public IReadOnlyList<string> AdditionalRootWildcards
        {
            get;
        }

        private BuildFromRootsMode()
        {
            IsEnabled = false;
            AdditionalRootWildcards = new List<string>();
        }

        private BuildFromRootsMode(
            List<string> additionalRootWildcards
            )
        {
            ArgumentNullException.ThrowIfNull(additionalRootWildcards);

            //without roots the mode would remove every project from slnf
            if (additionalRootWildcards.Count == 0)
            {
                throw new ArgumentException("At least one root wildcard must be provided.", nameof(additionalRootWildcards));
            }

            IsEnabled = true;
            AdditionalRootWildcards = additionalRootWildcards;
        }
    }
}

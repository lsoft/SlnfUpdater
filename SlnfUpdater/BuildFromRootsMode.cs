using System;
using System.Collections.Generic;

namespace SlnfUpdater
{
    public sealed class BuildFromRootsMode
    {
        public static readonly BuildFromRootsMode Disabled = new();
        public static readonly BuildFromRootsMode Enabled = new([]);
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
            IsEnabled = true;
            AdditionalRootWildcards = additionalRootWildcards ?? throw new ArgumentNullException(nameof(additionalRootWildcards));
        }
    }
}

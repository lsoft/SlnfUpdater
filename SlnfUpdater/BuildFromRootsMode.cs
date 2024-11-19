using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (additionalRootWildcards is null)
            {
                throw new ArgumentNullException(nameof(additionalRootWildcards));
            }

            IsEnabled = true;
            AdditionalRootWildcards = additionalRootWildcards;
        }
    }
}

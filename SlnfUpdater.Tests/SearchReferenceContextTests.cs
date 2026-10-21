using System;
using System.IO;
using Xunit;

namespace SlnfUpdater.Tests
{
    public sealed class SearchReferenceContextTests
    {
        private static readonly string SlnFolder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo", "build"));
        private static readonly string SlnPath = Path.Combine(SlnFolder, "All.sln");

        [Fact]
        public void NotNormalizedExistingPath_IsNotAddedAgain()
        {
            var notNormalized = Path.Combine(SlnFolder, "..", "src", "A", "A.csproj");
            var normalized = Path.GetFullPath(notNormalized);

            var context = new SearchReferenceContext(SlnPath, [notNormalized]);
            context.AddReferenceFullPathIfNew(normalized);

            Assert.False(context.HasChanges);
            Assert.True(context.IsProcessed(notNormalized));
        }

        [Fact]
        public void CaseInsensitiveComparer_DifferentCaseIsNotAddedAgain_ExistingSpellingIsKept()
        {
            var existing = Path.Combine(SlnFolder, "..", "src", "A", "A.csproj");
            var otherCase = Path.Combine(SlnFolder, "..", "SRC", "a", "a.csproj");

            var context = new SearchReferenceContext(SlnPath, [existing], StringComparer.OrdinalIgnoreCase);
            context.AddReferenceFullPathIfNew(otherCase);

            Assert.False(context.HasChanges);
        }

        [Fact]
        public void CaseSensitiveComparer_DifferentCaseIsAdded()
        {
            var existing = Path.Combine(SlnFolder, "..", "src", "A", "A.csproj");
            var otherCase = Path.Combine(SlnFolder, "..", "SRC", "a", "a.csproj");

            var context = new SearchReferenceContext(SlnPath, [existing], StringComparer.Ordinal);
            context.AddReferenceFullPathIfNew(otherCase);

            Assert.True(context.HasChanges);
        }

        [Fact]
        public void DeletingNotExistingReference_IsNotAChange()
        {
            var existing = Path.Combine(SlnFolder, "A", "A.csproj");

            var context = new SearchReferenceContext(SlnPath, [existing]);
            context.DeleteReference(Path.Combine(SlnFolder, "B", "B.csproj"));

            Assert.False(context.HasChanges);
        }
    }
}

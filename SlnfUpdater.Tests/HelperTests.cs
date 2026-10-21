using System;
using System.IO;
using SlnfUpdater.Helper;
using Xunit;

namespace SlnfUpdater.Tests
{
    public sealed class HelperTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));

        [Theory]
        [InlineData("*ProjectA.csproj", true)]
        [InlineData("*Project?.csproj", true)]
        [InlineData("*src/*/ProjectA.csproj", true)]
        [InlineData("*src\\*\\ProjectA.csproj", true)]
        [InlineData("ProjectA.csproj", false)]
        [InlineData("*ProjectB.csproj", false)]
        [InlineData("*Project.A.csproj", false)]
        public void MatchesWildcard(string wildcard, bool expected)
        {
            var fullPath = Path.Combine(RepoRoot, "src", "ProjectA", "ProjectA.csproj");

            Assert.Equal(expected, fullPath.MatchesWildcard(wildcard));
        }

        [Fact]
        public void MatchesWildcard_CaseSensitivityFollowsOs()
        {
            var fullPath = Path.Combine(RepoRoot, "src", "ProjectA", "ProjectA.csproj");

            Assert.Equal(OperatingSystem.IsWindows(), fullPath.MatchesWildcard("*projecta.csproj"));
        }

        [Fact]
        public void BuildMessageChain_ContainsAllInnerMessages()
        {
            var exception = new InvalidOperationException("outer", new InvalidOperationException("middle", new Exception("inner")));

            var message = exception.BuildMessageChain();

            Assert.Contains("outer", message);
            Assert.Contains("middle", message);
            Assert.Contains("inner", message);
        }
    }
}

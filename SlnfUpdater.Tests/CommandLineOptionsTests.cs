using Xunit;

namespace SlnfUpdater.Tests
{
    public sealed class CommandLineOptionsTests
    {
        [Theory]
        [InlineData]
        [InlineData(".")]
        public void TooFewArguments_Fails(params string[] args)
        {
            Assert.False(CommandLineOptions.TryParse(args, out _, out var error));
            Assert.Contains("at least 2 arguments", error);
        }

        [Fact]
        public void PositionalArgumentsOnly_RootsModeIsDisabled()
        {
            Assert.True(CommandLineOptions.TryParse([".", "*.slnf"], out var options, out _));

            Assert.Equal(".", options.SlnfFolderPath);
            Assert.Equal("*.slnf", options.SlnfFileMask);
            Assert.False(options.FromRootsMode.IsEnabled);
        }

        [Fact]
        public void RebuildFromRootsWithoutRoots_Fails()
        {
            Assert.False(CommandLineOptions.TryParse([".", "*.slnf", "-rebuild-from-roots"], out _, out var error));
            Assert.Contains("-additional-roots:", error);
        }

        [Fact]
        public void RebuildFromRootsWithEmptyRoots_Fails()
        {
            Assert.False(CommandLineOptions.TryParse([".", "*.slnf", "-rebuild-from-roots", "-additional-roots:;"], out _, out _));
        }

        [Fact]
        public void AdditionalRootsWithoutRebuildFromRoots_Fails()
        {
            Assert.False(CommandLineOptions.TryParse([".", "*.slnf", "-additional-roots:*A.csproj"], out _, out var error));
            Assert.Contains("-rebuild-from-roots", error);
        }

        [Theory]
        [InlineData("-additional-root:*A.csproj")]
        [InlineData("-rebuild-from-root")]
        [InlineData("-rebuild-from-roots-please")]
        public void UnknownArgument_Fails(string unknownArgument)
        {
            Assert.False(CommandLineOptions.TryParse([".", "*.slnf", "-rebuild-from-roots", "-additional-roots:*A.csproj", unknownArgument], out _, out var error));
            Assert.Contains(unknownArgument, error);
        }

        [Fact]
        public void RootsAreCollectedFromAllKeys()
        {
            Assert.True(
                CommandLineOptions.TryParse(
                    [".", "*.slnf", "-rebuild-from-roots", "-additional-roots:*A.csproj;*B.csproj;", "-additional-roots: *C.csproj "],
                    out var options,
                    out _
                    )
                );

            Assert.True(options.FromRootsMode.IsEnabled);
            Assert.Equal(["*A.csproj", "*B.csproj", "*C.csproj"], options.FromRootsMode.AdditionalRootWildcards);
        }
    }
}

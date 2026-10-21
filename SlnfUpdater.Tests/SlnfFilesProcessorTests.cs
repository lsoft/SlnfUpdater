using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SlnfUpdater.Tests
{
    public sealed class SlnfFilesProcessorTests : IDisposable
    {
        private readonly TestWorkspace _workspace = new();

        public void Dispose()
        {
            _workspace.Dispose();
        }

        [Fact]
        public void TransitiveReferencesAreAdded()
        {
            _workspace.WriteProject(@"A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"B\B.csproj", @"..\C\C.csproj");
            _workspace.WriteProject(@"C\C.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal(
                [@"A\A.csproj", @"B\B.csproj", @"C\C.csproj"],
                _workspace.ReadSlnfProjects("Test.slnf")
                );
        }

        [Fact]
        public void SlnInSubfolder_ParentRelativePaths_DoNotProduceDuplicates()
        {
            _workspace.WriteProject(@"src\A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"src\B\B.csproj");
            _workspace.WriteSlnf("Test.slnf", @"build\All.sln", @"..\src\A\A.csproj", @"..\src\B\B.csproj");
            var before = _workspace.ReadFile("Test.slnf");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            //nothing has changed, so the file must not be touched
            Assert.Equal(before, _workspace.ReadFile("Test.slnf"));
        }

        [Fact]
        public void SlnInSubfolder_MissingReferenceIsAddedRelativeToSln()
        {
            _workspace.WriteProject(@"src\A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"src\B\B.csproj");
            _workspace.WriteSlnf(@"filters\Test.slnf", @"..\build\All.sln", @"..\src\A\A.csproj");

            Assert.Equal(0, _workspace.Process("filters", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal(
                [@"..\src\A\A.csproj", @"..\src\B\B.csproj"],
                _workspace.ReadSlnfProjects(@"filters\Test.slnf")
                );
        }

        [Fact]
        public void NotExistingProjectIsRemoved()
        {
            _workspace.WriteProject(@"A\A.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"Deleted\Deleted.csproj");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal([@"A\A.csproj"], _workspace.ReadSlnfProjects("Test.slnf"));
        }

        [Fact]
        public void ReferenceToNotExistingProject_FailsAndLeavesFileUntouched()
        {
            //B is listed after A, so A is processed first and must not depend on processing order
            _workspace.WriteProject(@"A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"B\B.csproj");
            var before = _workspace.ReadFile("Test.slnf");

            Assert.Equal(1, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal(before, _workspace.ReadFile("Test.slnf"));
        }

        [Fact]
        public void ReferenceToNotExistingProject_FailsRegardlessOfProjectOrder()
        {
            _workspace.WriteProject(@"Z\Z.csproj", @"..\B\B.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"B\B.csproj", @"Z\Z.csproj");

            Assert.Equal(1, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));
        }

        [Fact]
        public void FailedSlnfDoesNotStopOtherSlnfFiles()
        {
            _workspace.WriteProject(@"A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"B\B.csproj");
            _workspace.WriteProject(@"Broken\Broken.csproj", @"..\Missing\Missing.csproj");
            _workspace.WriteSlnf("Broken.slnf", "Test.sln", @"Broken\Broken.csproj");
            _workspace.WriteSlnf("Good.slnf", "Test.sln", @"A\A.csproj");

            Assert.Equal(1, _workspace.Process(".", BuildFromRootsMode.Disabled, "Broken.slnf", "Good.slnf"));

            Assert.Equal([@"A\A.csproj", @"B\B.csproj"], _workspace.ReadSlnfProjects("Good.slnf"));
        }

        [Fact]
        public void ForwardSlashesAreWrittenAsBackslashes()
        {
            _workspace.WriteProject(@"A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"B\B.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", "A/A.csproj");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal([@"A\A.csproj", @"B\B.csproj"], _workspace.ReadSlnfProjects("Test.slnf"));
            Assert.DoesNotContain("/", _workspace.ReadFile("Test.slnf"));
        }

        [Fact]
        public void SolutionDirInProjectReferenceIsResolved()
        {
            _workspace.WriteProject(@"A\A.csproj", @"$(SolutionDir)B\B.csproj");
            _workspace.WriteProject(@"B\B.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "Test.slnf"));

            Assert.Equal([@"A\A.csproj", @"B\B.csproj"], _workspace.ReadSlnfProjects("Test.slnf"));
        }

        [Fact]
        public void RootsMode_StaleProjectsAreRemoved_RootDependenciesAreKept()
        {
            _workspace.WriteProject(@"A\A.csproj", @"..\B\B.csproj");
            _workspace.WriteProject(@"B\B.csproj");
            _workspace.WriteProject(@"Stale\Stale.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"B\B.csproj", @"Stale\Stale.csproj");

            var mode = BuildFromRootsMode.WithAdditionalRoots(["*A.csproj"]);
            Assert.Equal(0, _workspace.Process(".", mode, "Test.slnf"));

            Assert.Equal([@"A\A.csproj", @"B\B.csproj"], _workspace.ReadSlnfProjects("Test.slnf"));
        }

        [Fact]
        public void RootsMode_NoRootMatched_SlnfIsKeptAsIs()
        {
            _workspace.WriteProject(@"A\A.csproj");
            _workspace.WriteProject(@"Stale\Stale.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"Stale\Stale.csproj");
            var before = _workspace.ReadFile("Test.slnf");

            var mode = BuildFromRootsMode.WithAdditionalRoots(["*Unknown.csproj"]);
            Assert.Equal(0, _workspace.Process(".", mode, "Test.slnf"));

            Assert.Equal(before, _workspace.ReadFile("Test.slnf"));
        }

        [Fact]
        public void RootsMode_SeveralWildcardsMatchSameProject_NoDuplicates()
        {
            _workspace.WriteProject(@"A\A.csproj");
            _workspace.WriteProject(@"Stale\Stale.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"Stale\Stale.csproj");

            var mode = BuildFromRootsMode.WithAdditionalRoots(["*A.csproj", "*\\A\\*"]);
            Assert.Equal(0, _workspace.Process(".", mode, "Test.slnf"));

            Assert.Equal([@"A\A.csproj"], _workspace.ReadSlnfProjects("Test.slnf"));
        }

        [Fact]
        public void RootsMode_FailureLeavesFileUntouched()
        {
            _workspace.WriteProject(@"A\A.csproj", @"..\Missing\Missing.csproj");
            _workspace.WriteProject(@"Stale\Stale.csproj");
            _workspace.WriteSlnf("Test.slnf", "Test.sln", @"A\A.csproj", @"Stale\Stale.csproj");
            var before = _workspace.ReadFile("Test.slnf");

            var mode = BuildFromRootsMode.WithAdditionalRoots(["*A.csproj"]);
            Assert.Equal(1, _workspace.Process(".", mode, "Test.slnf"));

            Assert.Equal(before, _workspace.ReadFile("Test.slnf"));
        }

        [Fact]
        public void RootsMode_WithoutRootsIsNotAllowed()
        {
            Assert.Throws<ArgumentException>(() => BuildFromRootsMode.WithAdditionalRoots(new List<string>()));
        }

        [Fact]
        public void SdkStyleFixture_ConditionalReferenceIsAdded()
        {
            //repository Test folder contains SDK-style projects with a reference under TargetFramework condition
            CopyDirectory(FindRepositoryTestFolder(), _workspace.RootPath);
            _workspace.WriteSlnf("SlnfUpdater.Test.slnf", "SlnfUpdater.Test.sln", @"SlnfUpdater.Test.ProjectA\SlnfUpdater.Test.ProjectA.csproj");

            Assert.Equal(0, _workspace.Process(".", BuildFromRootsMode.Disabled, "SlnfUpdater.Test.slnf"));

            Assert.Equal(
                [
                    @"SlnfUpdater.Test.ProjectA\SlnfUpdater.Test.ProjectA.csproj",
                    @"SlnfUpdater.Test.ProjectAB\SlnfUpdater.Test.ProjectAB.csproj"
                ],
                _workspace.ReadSlnfProjects("SlnfUpdater.Test.slnf")
                );
        }

        private static string FindRepositoryTestFolder()
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            {
                var candidate = Path.Combine(folder.FullName, "Test");
                if (File.Exists(Path.Combine(candidate, "SlnfUpdater.Test.sln")))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Repository Test folder is not found.");
        }

        private static void CopyDirectory(string source, string target)
        {
            foreach (var sourceFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, sourceFile);
                var segments = relativePath.Split(Path.DirectorySeparatorChar);
                if (Array.Exists(segments, s => s is "bin" or "obj"))
                {
                    continue;
                }

                var targetFile = Path.Combine(target, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                File.Copy(sourceFile, targetFile, true);
            }
        }
    }
}

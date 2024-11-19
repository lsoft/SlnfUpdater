using Microsoft.Build.Construction;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using System.Text;

namespace SlnfUpdater
{
    public readonly struct Project2Paths : IEquatable<Project2Paths>
    {
        public readonly string FullPath;
        public readonly string RelativeSlnPath;

        private Project2Paths(
            string fullPath,
            string relativeSlnPath
            )
        {
            FullPath = fullPath;
            RelativeSlnPath = relativeSlnPath;
        }

        public override bool Equals(object? obj)
        {
            return obj is Project2Paths paths && Equals(paths);
        }

        public bool Equals(Project2Paths other)
        {
            return FullPath == other.FullPath &&
                   RelativeSlnPath == other.RelativeSlnPath;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullPath, RelativeSlnPath);
        }

        public static bool operator ==(Project2Paths left, Project2Paths right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Project2Paths left, Project2Paths right)
        {
            return !(left == right);
        }

        public static Project2Paths Create(
            string fullPath,
            string slnFullPath
            )
        {
            return new Project2Paths(fullPath, fullPath.MakeRelativeAgainst(slnFullPath));
        }

    }

    public sealed class SearchReferenceContext
    {
        private readonly HashSet<Project2Paths> _existReferences;

        private readonly HashSet<Project2Paths> _processedReferences;

        private readonly HashSet<Project2Paths> _addedReferences;
        private readonly HashSet<Project2Paths> _deletedReferences;

        public string SlnFullPath
        {
            get;
        }
        public string SlnfFilePath
        {
            get;
        }
        public string SlnfFileName
        {
            get;
        }
        public string SlnfFolderPath
        {
            get;
        }

        public bool HasChanges => _addedReferences.Count > 0 || _deletedReferences.Count > 0;

        public SearchReferenceContext(
            string slnFullPath,
            string slnfFilePath,
            IEnumerable<string> existReferences
            )
        {
            SlnFullPath = slnFullPath;
            SlnfFilePath = slnfFilePath;
            var slnfFileInfo = new FileInfo(slnfFilePath);
            SlnfFileName = slnfFileInfo.Name;
            SlnfFolderPath = slnfFileInfo.Directory.FullName;
            
            _existReferences = existReferences.Select(p => Create2PathsFrom(p)).ToHashSet();
            _processedReferences = new HashSet<Project2Paths>();
            _addedReferences = new HashSet<Project2Paths>();
            _deletedReferences = new HashSet<Project2Paths>();
        }

        public bool IsProcessed(string checkedFullFilePath)
        {
            //we must not check ExistReferences here
            //because any existing reference may not include all its children in slnf,
            //but we NEED to have all its children in slnf!

            var twoPaths = Create2PathsFrom(checkedFullFilePath);

            if (_processedReferences.Contains(twoPaths))
            {
                return true;
            }

            if (_addedReferences.Contains(twoPaths))
            {
                return true;
            }

            if (_deletedReferences.Contains(twoPaths))
            {
                return true;
            }

            return false;
        }

        public void DeleteReference(string deletedReferenceFullPath)
        {
            var twoPaths = Create2PathsFrom(deletedReferenceFullPath);
            
            _processedReferences.Add(twoPaths);

            if (!_existReferences.Contains(twoPaths))
            {
                return;
            }

            _deletedReferences.Add(twoPaths);
        }


        public void AddReferenceFullPathIfNew(string addedProjectFullPath)
        {
            var twoPaths = Create2PathsFrom(addedProjectFullPath);

            _processedReferences.Add(twoPaths);

            if (_existReferences.Contains(twoPaths))
            {
                return;
            }

            _addedReferences.Add(twoPaths);
        }

        public void ApplyChangesTo(SlnfJsonStructured structured)
        {
            structured.JsonBody.solution.projects =
                _addedReferences
                    .Union(_existReferences.Except(_deletedReferences))
                    .Select(twoPaths => twoPaths.RelativeSlnPath)
                    .OrderBy(a => a, StringComparer.Ordinal)
                    .ToArray()
                    ;
        }

        public string BuildResultMessage()
        {
            if (!HasChanges)
            {
                return $"   No references changed.".Pastel(ColorTable.NoReferenceColor);
            }

            var resultMessage = new StringBuilder();
            if (_addedReferences.Count > 0)
            {
                var addedReferences = string.Join(
                    Environment.NewLine,
                    _addedReferences.Select(r => "      " + r.RelativeSlnPath)
                    ).Pastel(ColorTable.AddedReferenceColor);

                resultMessage.AppendLine($"""
   Added references:
{addedReferences}
""");
            }
            if (_deletedReferences.Count > 0)
            {
                var deletedReferences = string.Join(
                    Environment.NewLine,
                    _deletedReferences.Select(r => "      " + r.RelativeSlnPath)
                    ).Pastel(ColorTable.DeletedReferenceColor);

                resultMessage.AppendLine($"""
   Deleted references:
{deletedReferences}
""");
            }

            return resultMessage.ToString();
        }


        private Project2Paths Create2PathsFrom(
            string fileFullPath
            )
        {
            return Project2Paths.Create(fileFullPath, SlnFullPath);
        }

    }
}

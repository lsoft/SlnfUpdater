using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using System.Text;

namespace SlnfUpdater
{
    public readonly struct Project2Paths
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

        public static Project2Paths Create(
            string fullPath,
            string slnFullFolderPath
            )
        {
            var normalizedFullPath = Path.GetFullPath(fullPath);
            return new Project2Paths(normalizedFullPath, Path.GetRelativePath(slnFullFolderPath, normalizedFullPath));
        }
    }

    /// <summary>
    /// Project identity is its full path only; relative path is derived from it.
    /// </summary>
    public sealed class Project2PathsComparer : IEqualityComparer<Project2Paths>
    {
        private readonly StringComparer _pathComparer;

        public Project2PathsComparer(StringComparer pathComparer)
        {
            _pathComparer = pathComparer ?? throw new ArgumentNullException(nameof(pathComparer));
        }

        public bool Equals(Project2Paths x, Project2Paths y)
        {
            return _pathComparer.Equals(x.FullPath, y.FullPath);
        }

        public int GetHashCode(Project2Paths obj)
        {
            return _pathComparer.GetHashCode(obj.FullPath);
        }
    }

    public sealed class SearchReferenceContext
    {
        private readonly Project2PathsComparer _comparer;

        private readonly HashSet<Project2Paths> _existReferences;

        private readonly HashSet<Project2Paths> _processedReferences;

        private readonly HashSet<Project2Paths> _addedReferences;
        private readonly HashSet<Project2Paths> _deletedReferences;

        public string SlnFullPath
        {
            get;
        }

        public string SlnFullFolderPath
        {
            get;
        }

        public bool HasChanges => _addedReferences.Count > 0 || _deletedReferences.Count > 0;

        public SearchReferenceContext(
            string slnFullPath,
            IEnumerable<string> existReferences
            ) : this(slnFullPath, existReferences, PathHelper.PathComparer)
        {
        }

        internal SearchReferenceContext(
            string slnFullPath,
            IEnumerable<string> existReferences,
            StringComparer pathComparer
            )
        {
            SlnFullPath = slnFullPath;
            SlnFullFolderPath = Path.GetDirectoryName(Path.GetFullPath(slnFullPath))!;

            _comparer = new Project2PathsComparer(pathComparer);
            _existReferences = existReferences.Select(Create2PathsFrom).ToHashSet(_comparer);
            _processedReferences = new HashSet<Project2Paths>(_comparer);
            _addedReferences = new HashSet<Project2Paths>(_comparer);
            _deletedReferences = new HashSet<Project2Paths>(_comparer);
        }

        public bool IsProcessed(string checkedFullFilePath)
        {
            //we must not check ExistReferences here
            //because any existing reference may not include all its children in slnf,
            //but we NEED to have all its children in slnf!

            //added and deleted references are always marked as processed
            return _processedReferences.Contains(Create2PathsFrom(checkedFullFilePath));
        }

        public void MarkProcessed(string processedFullFilePath)
        {
            _processedReferences.Add(Create2PathsFrom(processedFullFilePath));
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

        public void ApplyChangesTo(Slnf structured)
        {
            //existing references go first, so their spelling wins
            structured.JsonBody.Solution.Projects =
                _existReferences
                    .Except(_deletedReferences, _comparer)
                    .Union(_addedReferences, _comparer)
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
            return Project2Paths.Create(fileFullPath, SlnFullFolderPath);
        }
    }
}

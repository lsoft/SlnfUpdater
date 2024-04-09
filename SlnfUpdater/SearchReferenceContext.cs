using Microsoft.Build.Construction;
using SlnfUpdater.Helper;

namespace SlnfUpdater
{
    public class SearchReferenceContext
    {
        private readonly HashSet<string> _processedReferences;

        private readonly HashSet<string> _addedReferences;

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
        public SolutionFile SlnFile
        {
            get;
        }
        public IReadOnlySet<string> ExistReferences
        {
            get;
        }

        public IReadOnlySet<string> AddedReferences => _addedReferences;

        public SearchReferenceContext(
            string slnFullPath,
            string slnfFilePath,
            SolutionFile slnFile,
            IReadOnlySet<string> existReferences
            )
        {
            //var slnFolderPath = new FileInfo(slnFullPath).Directory.FullName;

            SlnFullPath = slnFullPath;
            SlnfFilePath = slnfFilePath;
            var slnfFileInfo = new FileInfo(slnfFilePath);
            SlnfFileName = slnfFileInfo.Name;
            SlnfFolderPath = slnfFileInfo.Directory.FullName;
            SlnFile = slnFile;
            ExistReferences = existReferences;

            _processedReferences = new HashSet<string>();
            _addedReferences = new HashSet<string>();
        }

        public bool IsProcessed(string addedReferenceFullPath)
        {
            var referenceProjectPathRelativeSln = addedReferenceFullPath.MakeRelativeAgainst(
                SlnFullPath
                );

            //we must not check ExistReferences here
            //because any existing reference may not include all its children in slnf

            if (_processedReferences.Contains(referenceProjectPathRelativeSln))
            {
                return true;
            }

            if (AddedReferences.Contains(referenceProjectPathRelativeSln))
            {
                return true;
            }

            return false;
        }

        public void AddReferenceFullPathIfNew(string addedReferenceFullPath)
        {
            var referenceProjectPathRelativeSln = addedReferenceFullPath.MakeRelativeAgainst(
                SlnFullPath
                );

            _processedReferences.Add(referenceProjectPathRelativeSln);

            if (ExistReferences.Contains(referenceProjectPathRelativeSln))
            {
                return;
            }

            _addedReferences.Add(referenceProjectPathRelativeSln);
        }

        public List<string> GetSortedProjects()
        {
            return AddedReferences
                .Union(ExistReferences)
                .OrderByDescending(a => a, StringComparer.Ordinal)
                .ToList()
                ;
        }
    }
}

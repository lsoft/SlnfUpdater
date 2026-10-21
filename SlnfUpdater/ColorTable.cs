using System.Drawing;

namespace SlnfUpdater
{
    public static class ColorTable
    {
        public static readonly Color SolutionProjectColor = Color.FromArgb(165, 229, 250);
        public static readonly Color TimeColor = Color.FromArgb(220, 110, 0);
        public static readonly Color NoReferenceColor = Color.FromArgb(255, 255, 0);
        public static readonly Color FilteredRootsColor = Color.FromArgb(205, 120, 165);
        public static readonly Color AddedReferenceColor = Color.FromArgb(0, 255, 0);
        public static readonly Color DeletedReferenceColor = Color.FromArgb(220, 80, 0);
        public static readonly Color ErrorColor = Color.FromArgb(255, 70, 70);
    }
}

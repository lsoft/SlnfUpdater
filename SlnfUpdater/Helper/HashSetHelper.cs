namespace SlnfUpdater.Helper
{
    public static class HashSetHelper
    {
        public static void AddRange<T>(this HashSet<T> target, IReadOnlySet<T> source)
        {
            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}

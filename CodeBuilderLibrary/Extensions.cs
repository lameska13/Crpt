namespace CodeBuilderLibrary
{
    public static class Extensions
    {
        public static T? Find<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            foreach (var current in enumerable)
            {
                if (predicate(current))
                {
                    return current;
                }
            }
            return default;
        }
    }
}
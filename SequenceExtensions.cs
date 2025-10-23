namespace SortingInMaze;

internal static class SequenceExtensions
{
    public static int SequenceCompareTo<T>(this IEnumerable<T> a, IEnumerable<T> b)
        where T : IComparable<T>
    {
        using var e1 = a.GetEnumerator();
        using var e2 = b.GetEnumerator();
        while (true)
        {
            var m1 = e1.MoveNext();
            var m2 = e2.MoveNext();
            if (!m1 && !m2) return 0;
            if (!m1) return -1;
            if (!m2) return 1;
            var cmp = e1.Current.CompareTo(e2.Current);
            if (cmp != 0) return cmp;
        }
    }
}
using System.Collections;

namespace MiniXmpp.Collections;

public sealed class FullJidComparer : IComparer, IComparer<Jid>
{
    public static FullJidComparer Shared { get; } = new();

    FullJidComparer()
    {

    }

    public static bool AreEquals(Jid? x, Jid? y)
        => CompareCore(x, y) == 0;

    public int Compare(Jid? x, Jid? y)
        => CompareCore(x, y);

    int IComparer.Compare(object? x, object? y)
        => CompareCore(x as Jid, y as Jid);

    static int CompareCore(Jid? x, Jid? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var result = string.Compare(x.Local, y.Local, StringComparison.OrdinalIgnoreCase);

        if (result != 0)
            return result;

        result = string.Compare(x.Domain, y.Domain, StringComparison.OrdinalIgnoreCase);

        if (result != 0)
            return result;

        return string.Compare(x.Resource, y.Resource, StringComparison.Ordinal);
    }
}
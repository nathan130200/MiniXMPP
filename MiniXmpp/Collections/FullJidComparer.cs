namespace MiniXmpp.Collections;

public class FullJidComparer : IComparer<Jid>
{
    public static FullJidComparer Shared { get; } = new();

    FullJidComparer()
    {

    }

    public static bool AreEquals(Jid left, Jid right)
        => Shared.Compare(left, right) == 0;

    public int Compare(Jid x, Jid y)
    {
        if (x == null && y == null) return 0;
        else if (x == null) return -1;
        else if (y == null) return 1;

        var result = string.Compare(x.Local, y.Local, StringComparison.OrdinalIgnoreCase);

        if (result != 0)
            return result;

        result = string.Compare(x.Domain, y.Domain, StringComparison.OrdinalIgnoreCase);

        if (result != 0)
            return result;

        return string.Compare(x.Resource, y.Resource, StringComparison.Ordinal);
    }
}
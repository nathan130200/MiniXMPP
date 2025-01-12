namespace MiniXmpp.Collections;

public class BareJidComparer : IComparer<Jid>
{
    public static BareJidComparer Shared { get; } = new();

    BareJidComparer()
    {

    }

    public static bool AreEquals(Jid left, Jid right)
        => Shared.Compare(left, right) == 0;

    public int Compare(Jid x, Jid y)
    {
        if (ReferenceEquals(x, y)) return 0;

        if (x == null) return -1;
        if (y == null) return 1;

        var result = string.Compare(x.Local, y.Local, StringComparison.OrdinalIgnoreCase);

        if (result != 0)
            return result;

        return string.Compare(x.Domain, y.Domain, StringComparison.OrdinalIgnoreCase);
    }
}

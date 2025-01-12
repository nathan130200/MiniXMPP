using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MiniXmpp;

public sealed record Jid
{
    public string? Local { get; init; }
    public string Domain { get; init; }
    public string? Resource { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (Local != null)
            sb.Append(Local).Append('@');

        if (Domain != null)
            sb.Append(Domain);

        if (Resource != null)
            sb.Append('/').Append(Resource);

        return sb.ToString();
    }

    public Jid(string jid)
    {
        jid.ThrowIfNull();

        if (jid.Length == 2)
        {
            if (jid[0] == '@' && jid[1] == '/')
            {
                Local = Domain = Resource = string.Empty;
                return;
            }
        }

        var at = jid.IndexOf('@');

        if (at != -1)
            Local = jid[0..at];

        var slash = jid.IndexOf('/');

        if (slash == -1)
            Domain = jid[(at + 1)..];
        else
        {
            Domain = jid[(at + 1)..slash];
            Resource = jid[(slash + 1)..];
        }
    }

    public bool IsServer => string.IsNullOrWhiteSpace(Local) && IsBare;
    public bool IsBare => string.IsNullOrWhiteSpace(Resource);

    [return: NotNullIfNotNull(nameof(s))]
    public static implicit operator Jid?(string? s)
        => s == null ? null : new(s);

    [return: NotNullIfNotNull(nameof(j))]
    public static implicit operator string?(Jid? j)
        => j?.ToString();
}

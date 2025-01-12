using System.Xml;

namespace MiniXmpp;

public sealed class XmppName : IEquatable<XmppName>
{
    private string _localName, _prefix;

    public XmppName()
    {

    }

    public bool IsNamespaceDeclaration
        => _prefix == "xmlns" || _localName == "xmlns";

    public XmppName(string qualifiedName)
    {
        qualifiedName.ThrowIfNullOrWhiteSpace();

        var ofs = qualifiedName.IndexOf(':');

        if (ofs > 0)
            Prefix = qualifiedName[0..ofs];

        LocalName = qualifiedName[(ofs + 1)..];
    }

    public XmppName(string prefix, string localName)
    {
        Prefix = prefix;
        LocalName = localName;
    }

    public XmppName(XmppName other)
    {
        _prefix = other._prefix;
        _localName = other._localName;
    }

    public bool HasPrefix => !string.IsNullOrWhiteSpace(_prefix);

    public string LocalName
    {
        get => _localName;
        internal set
        {
            value.ThrowIfNullOrWhiteSpace();
            _localName = XmlConvert.EncodeLocalName(value);
        }
    }

    public string Prefix
    {
        get => _prefix;
        internal set => _prefix = !string.IsNullOrWhiteSpace(value)
            ? XmlConvert.EncodeLocalName(value)
            : null;
    }

    public override string ToString()
    {
        if (_prefix == null)
            return _localName;

        return string.Concat(_prefix, ':', _localName);
    }

    public override int GetHashCode() => HashCode.Combine
    (
        LocalName?.GetHashCode() ?? 0,
        Prefix?.GetHashCode() ?? 0
    );

    public override bool Equals(object obj)
        => Equals(obj as XmppName);

    public bool Equals(XmppName other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        var result = string.Compare(_localName, other._localName, StringComparison.Ordinal);

        if (result != 0)
            return false;

        return string.Compare(_prefix, other._prefix, StringComparison.Ordinal) == 0;
    }

    public static implicit operator XmppName(string str) => new(str);
    public static implicit operator string(XmppName name) => name.ToString();

    public static XmppName operator +(XmppName left, string right)
    {
        return new XmppName
        {
            Prefix = left,
            LocalName = right
        };
    }

    public static bool operator !=(XmppName lhs, XmppName rhs) => !(lhs == rhs);

    public static bool operator ==(XmppName lhs, XmppName rhs)
    {
        if (ReferenceEquals(lhs, rhs))
            return true;

        if (lhs is null)
            return rhs is null;

        return lhs.Equals(rhs);
    }
}

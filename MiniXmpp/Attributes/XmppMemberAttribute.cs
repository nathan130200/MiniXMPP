namespace MiniXmpp.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmppMemberAttribute : Attribute
{
    public XmppMemberAttribute(string value)
        => Value = value;

    public string Value { get; }
}

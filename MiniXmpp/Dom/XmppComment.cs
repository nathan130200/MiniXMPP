using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp.Dom;

public class XmppComment : XmppContentNode
{
    public XmppComment()
    {
    }

    public XmppComment(string value) : base(value)
    {
    }

    public override XmppNode Clone() => new XmppComment(Value!);
}

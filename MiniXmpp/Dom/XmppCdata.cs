using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp.Dom;

public class XmppCdata : XmppContentNode
{
    public XmppCdata()
    {
    }

    public XmppCdata(string value) : base(value)
    {
    }

    public override XmppNode Clone() => new XmppCdata(Value!);
}

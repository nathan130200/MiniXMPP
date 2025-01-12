using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp.Dom;

public class XmppText : XmppContentNode
{
    public XmppText()
    {
    }

    public XmppText(string value) : base(value)
    {
    }

    public override XmppNode Clone() => new XmppText(Value);
}

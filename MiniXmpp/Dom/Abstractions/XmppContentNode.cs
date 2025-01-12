namespace MiniXmpp.Dom.Abstractions;

public abstract class XmppContentNode : XmppNode
{
    public XmppContentNode() : base()
    {

    }

    public XmppContentNode(string value) : base()
    {
        Value = value;
    }
}
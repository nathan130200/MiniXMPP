using MiniXmpp.Dom;

namespace MiniXmpp;

public class XmppStanza : XmppElement
{
    public XmppStanza(XmppStanza other) : base(other)
    {
    }

    public XmppStanza(string tagName, string xmlns = null, string value = null) : base(tagName, xmlns, value)
    {
    }

    internal XmppStanza()
    {
    }

    public string Id
    {
        get => Attributes["id"];
        set => Attributes["id"] = value;
    }

    public string Language
    {
        get => Attributes["xml:lang"];
        set => Attributes["xml:lang"] = value;
    }

    public Jid From
    {
        get => Attributes["from"];
        set => Attributes["from"] = value;
    }

    public Jid To
    {
        get => Attributes["to"];
        set => Attributes["to"] = value;
    }

    public void SwitchDirection()
    {
        var from = From;
        var to = To;
        To = from;
        From = to;
    }

    public string Type
    {
        get => Attributes["type"];
        set => Attributes["type"] = value;
    }

    public void GenerateId()
    {
        Id = string.Concat(
            DateTime.Now.Year.ToString("x2"),
            Random.Shared.Next(short.MaxValue, ushort.MaxValue).ToString("x2")
        );
    }
}

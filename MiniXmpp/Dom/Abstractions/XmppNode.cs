using System.Text;
using System.Xml;

namespace MiniXmpp.Dom.Abstractions;

public abstract class XmppNode
{
    internal XmppElement _parent;

    public virtual string Value { get; set; }

    internal XmppNode()
    {

    }

    public XmppElement Parent
    {
        get => _parent;
        set
        {
            _parent?.RemoveChild(this);
            value?.AddChild(this);
        }
    }

    public virtual void Remove()
    {
        _parent?.RemoveChild(this);
        _parent = null;
    }

    public abstract XmppNode Clone();

    public virtual void WriteTo(XmlWriter writer)
    {
        Xml.WriteXmlTree(this, writer);
    }

    public override string ToString() => ToString(false);

    public virtual string ToString(bool indented)
    {
        var flags = XmlFormatting.Default;
        if (indented) flags |= XmlFormatting.Indented;
        return ToString(flags);
    }

    public virtual string ToString(XmlFormatting formatting)
    {
        var sb = new StringBuilder();

        using (var writer = Xml.CreateWriter(sb, formatting))
            Xml.WriteXmlTree(this, writer);

        return sb.ToString();
    }
}

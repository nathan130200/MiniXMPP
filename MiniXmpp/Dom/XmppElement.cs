using System.Collections;
using System.Globalization;
using System.Text;
using MiniXmpp.Collections;
using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp.Dom;

public class XmppElement : XmppNode, IEnumerable<XmppNode>
{
    internal readonly List<XmppNode> ChildNodes = new();

    public XmppName TagName { get; set; }

    public string LocalName
    {
        get => TagName.LocalName;
        set => TagName.LocalName = value;
    }

    public string Prefix
    {
        get => TagName.Prefix;
        set => TagName.Prefix = value;
    }

    public XmppAttributeDictionary Attributes { get; }

    internal XmppElement() : base()
    {
        Attributes = new();
    }

    public XmppElement(XmppElement other) : this()
    {
        TagName = other.TagName;
    }

    public XmppElement(string tagName, string xmlns = default, string value = default) : this()
    {
        TagName = tagName;

        if (xmlns != null)
        {
            if (TagName.HasPrefix)
                SetNamespace(TagName.Prefix, xmlns);
            else
                SetNamespace(xmlns);
        }

        Value = value;
    }

    public override XmppNode Clone()
        => new XmppElement(this);

    public string GetNamespace(string prefix = default)
    {
        var key = prefix == null ? "xmlns" : $"xmlns:{prefix}";

        var result = Attributes[key];

        if (result != null)
            return result;

        return _parent?.GetNamespace(prefix);
    }

    public void SetNamespace(string value)
        => Attributes["xmlns"] = value;

    public void SetNamespace(string prefix, string value)
        => Attributes[$"xmlns:{prefix}"] = value;

    public string GetAttribute(string key, string defaultValue = default)
        => Attributes[key] ?? defaultValue;

#if NET8_0_OR_GREATER

    public T GetAttribute<T>(string key, T defaultValue = default, IFormatProvider formatProvider = default) where T : IParsable<T>
    {
        var temp = Attributes[key];

        if (temp == null)
            return defaultValue;

        if (T.TryParse(temp, formatProvider ?? CultureInfo.InvariantCulture, out var result))
            return result;

        return defaultValue;
    }
#else
    public T GetAttribute<T>(string key, T defaultValue = default)
    {
        var value = Attributes[key];

        if (value != default)
            return Xml.FromXmlString<T>(value, defaultValue);

        return defaultValue;
    }
#endif

    public void SetAttribute(string key, object value, IFormatProvider formatProvidedr = default)
    {
        if (value == null)
            Attributes[key] = null;
        else
            Attributes[key] = Convert.ToString(value, formatProvidedr ?? CultureInfo.InvariantCulture);
    }

    public string StartTag()
    {
        var sb = new StringBuilder();

        var flags = XmlFormatting.CheckCharacters
            | XmlFormatting.OmitDuplicatedNamespaces
            | XmlFormatting.OmitXmlDeclaration;

        using (var writer = Xml.CreateWriter(sb, flags))
            Xml.WriteStartElement(this, writer);

        return sb.ToString();
    }

    public string EndTag() => string.Concat("</", TagName, ">");

    static void CollectElements(XmppElement container, List<XmppElement> result)
    {
        foreach (var element in container.OfType<XmppElement>())
        {
            result.Add(element);
            CollectElements(element, result);
        }
    }

    public XmppElement Element(XmppName name)
        => Elements().FirstOrDefault(e => e.TagName == name);

    public IEnumerable<XmppElement> Elements()
        => this.OfType<XmppElement>();

    public virtual void RemoveAll()
    {
        var keys = Attributes.MapWhen(x => !x.Key.IsNamespaceDeclaration, x => x.Key);

        Attributes.RemoveAll(keys);

        lock (ChildNodes)
        {
            foreach (var item in ChildNodes)
                item._parent = null;

            ChildNodes.Clear();
        }
    }

    public virtual void AddChild(XmppNode node)
    {
        if (node == null)
            return;

        if (node._parent is not null)
            node = node.Clone();

        lock (ChildNodes)
        {
            ChildNodes.Add(node);
            node._parent = this;
        }
    }

    public virtual void RemoveChild(XmppNode node)
    {
        if (node?._parent != this)
            return;

        lock (ChildNodes)
        {
            ChildNodes.Remove(node);
            node._parent = null;
        }
    }

    static void CollectNodes(XmppElement element, List<XmppNode> result)
    {
        foreach (var node in element)
        {
            result.Add(node);

            if (node is XmppElement other)
                CollectNodes(other, result);
        }
    }

    public IEnumerable<XmppNode> DescendantNodes()
    {
        var result = new List<XmppNode>();
        CollectNodes(this, result);
        return result;
    }

    public IEnumerable<XmppNode> DescendantNodesAndSelf()
    {
        var result = new List<XmppNode> { this };
        CollectNodes(this, result);
        return result;
    }

    public IEnumerator<XmppNode> GetEnumerator()
    {
        lock (ChildNodes)
        {
            foreach (var node in ChildNodes)
                yield return node;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
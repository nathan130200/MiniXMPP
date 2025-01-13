using System.Globalization;
using System.Text;
using MiniXmpp.Collections;
using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp.Dom;

public class XmppElement : XmppNode
{
    internal readonly List<XmppNode> _childNodes = new();

    public XmppName TagName { get; set; } = default!;

    public string LocalName
    {
        get => TagName.LocalName;
        set => TagName.LocalName = value;
    }

    public string? Prefix
    {
        get => TagName.Prefix;
        set => TagName.Prefix = value;
    }

    public string? DefaultNamespace
    {
        get => GetNamespace();
        set => SetNamespace(value);
    }

    public string? Namespace
    {
        get => GetNamespace(Prefix);
        set
        {
            if (TagName.HasPrefix)
                SetNamespace(Prefix!, value);
            else
                SetNamespace(value);
        }
    }

    public void RemoveAttributes()
    {
        var keys = from attr in Attributes
                   where !attr.Key.IsNamespaceDeclaration
                   select attr.Key;

        Attributes.RemoveAll(keys);
    }

    public void RemoveNodes()
    {
        lock (_childNodes)
        {
            foreach (var item in _childNodes)
                item._parent = null;

            _childNodes.Clear();
        }
    }

    public override string? Value
    {
        get
        {
            return string.Concat(from n in Nodes().OfType<XmppText>()
                                 select n.Value);
        }
        set
        {
            RemoveNodes();

            if (value != null)
                Add(new XmppText(value));
        }
    }

    public XmppAttributeDictionary Attributes { get; }

    internal XmppElement() : base()
    {
        Attributes = new();
    }

    public XmppElement(XmppElement other) : this()
    {
        TagName = other.TagName;

        foreach (var (key, value) in other.Attributes)
            Attributes[new(key)] = value;

        foreach (var node in other.Nodes())
            Add(node.Clone());
    }

    public XmppElement(string tagName, string? xmlns = default, string? value = default) : this()
    {
        TagName = tagName;

        if (xmlns != null)
        {
            if (TagName.HasPrefix)
                SetNamespace(TagName.Prefix!, xmlns);
            else
                SetNamespace(xmlns);
        }

        Value = value;
    }

    public override XmppNode Clone()
        => new XmppElement(this);

    public string? GetNamespace(string? prefix = default)
    {
        var key = prefix == null ? "xmlns" : $"xmlns:{prefix}";

        var result = Attributes[key];

        if (result != null)
            return result;

        return _parent?.GetNamespace(prefix);
    }

    public void SetNamespace(string? value)
        => Attributes["xmlns"] = value;

    public void SetNamespace(string prefix, string? value)
    {
        prefix.ThrowIfNullOrWhiteSpace();
        Attributes[$"xmlns:{prefix}"] = value;
    }

    public string? GetAttribute(XmppName key, string? defaultValue = default)
        => Attributes[key] ?? defaultValue;

#pragma warning disable

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

#pragma warning restore

    public void SetAttribute(string key, object value, IFormatProvider? formatProvidedr = default)
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

    static void CollectElements(XmppElement self, List<XmppElement> result)
    {
        foreach (var element in self.Nodes().OfType<XmppElement>())
        {
            result.Add(element);
            CollectElements(element, result);
        }
    }

    public IEnumerable<XmppNode> Nodes()
    {
        lock (_childNodes)
        {
            foreach (var node in _childNodes)
                yield return node;
        }
    }

    public XmppElement? Element(XmppName name) => Elements().FirstOrDefault(e => e.TagName == name);
    public IEnumerable<XmppElement> Elements() => Nodes().OfType<XmppElement>();

    public virtual void RemoveAll()
    {
        RemoveAttributes();
        RemoveNodes();
    }

    public virtual void Add(XmppNode? node)
    {
        if (node is null)
            return;

        if (node._parent is not null)
            node = node.Clone();

        lock (_childNodes)
        {
            _childNodes.Add(node);
            node._parent = this;
        }
    }

    public virtual void Remove(XmppNode? node)
    {
        if (node is null)
            return;

        if (node._parent != this)
            return;

        lock (_childNodes)
        {
            _childNodes.Remove(node);
            node._parent = null;
        }
    }

    static void CollectNodes(XmppElement self, List<XmppNode> result)
    {
        foreach (var node in self.Nodes())
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
}
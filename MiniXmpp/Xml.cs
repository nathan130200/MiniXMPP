using System.Globalization;
using System.Text;
using System.Xml;
using MiniXmpp.Dom;
using MiniXmpp.Dom.Abstractions;

namespace MiniXmpp;

public delegate T ConvertFromXml<T>(string str);
public delegate string ConvertToXml<T>(T value);

public static class Xml
{
    internal static readonly Dictionary<Type, (Delegate ConvertFromString, Delegate ConvertToString)> s_XmlConverters = new();

    static Xml()
    {
        RegisterXmlConverter(XmlConvert.ToSingle, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToDouble, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToInt16, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToInt32, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToInt64, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToSByte, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToByte, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToUInt16, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToUInt32, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToUInt64, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToTimeSpan, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToDateTimeOffset, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToGuid, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToBoolean, XmlConvert.ToString);
        RegisterXmlConverter(XmlConvert.ToChar, XmlConvert.ToString);
    }

    public static void RegisterXmlConverter<T>(ConvertFromXml<T> convertFrom, ConvertToXml<T> convertTo, bool replaceExisting = false)
    {
        var key = typeof(T);

        lock (s_XmlConverters)
        {
            if (s_XmlConverters.ContainsKey(key) && !replaceExisting)
                return;

            s_XmlConverters[typeof(T)] = (convertFrom, convertTo);
        }
    }

    public static T FromXmlString<T>(string value, T defaultValue = default, bool throwOnError = true)
    {
        var type = typeof(T);

        if (!s_XmlConverters.TryGetValue(type, out var tpl))
            throw new NotImplementedException($"XML converter for type '{type}' is not registred.");

        try
        {
            return (T)tpl.ConvertFromString.DynamicInvoke(new[] { value });
        }
        catch
        {
            if (throwOnError)
                throw;
        }

        return defaultValue;
    }

    public static string ToXmlString<T>(T value, IFormatProvider formatProvider = default, bool throwOnError = true)
    {
        var type = typeof(T);

        if (!s_XmlConverters.TryGetValue(type, out var tpl))
            throw new NotImplementedException($"XML converter for type '{type}' is not registred.");

        try
        {
            return (string)tpl.ConvertToString.DynamicInvoke(new[] { value });
        }
        catch
        {
            if (throwOnError)
                throw;
        }

        return Convert.ToString(value, formatProvider ?? CultureInfo.InvariantCulture);
    }

    public static XmlWriter CreateWriter(StringBuilder textWriter, XmlFormatting formatting, Encoding encoding = default)
    {
        var conformanceLevel = ConformanceLevel.Fragment
            .Choose(
                condition: formatting.HasFlag(XmlFormatting.OmitXmlDeclaration),
                whenFalse: ConformanceLevel.Document
            );

        var namespaceHandling = NamespaceHandling.OmitDuplicates
            .Choose(
                condition: formatting.HasFlag(XmlFormatting.OmitDuplicatedNamespaces),
                whenFalse: NamespaceHandling.Default
            );

        return XmlWriter.Create(textWriter, new XmlWriterSettings()
        {
            Indent = formatting.HasFlag(XmlFormatting.Indented),
            IndentChars = "\t",
            ConformanceLevel = conformanceLevel,
            CloseOutput = false,
            Encoding = encoding ?? Encoding.UTF8,
            NamespaceHandling = namespaceHandling,
            OmitXmlDeclaration = formatting.HasFlag(XmlFormatting.OmitXmlDeclaration),
            NewLineChars = "\n",
            NewLineOnAttributes = formatting.HasFlag(XmlFormatting.NewLineOnAttributes),
            DoNotEscapeUriAttributes = formatting.HasFlag(XmlFormatting.DoNotEscapeUriAttributes),
            CheckCharacters = formatting.HasFlag(XmlFormatting.CheckCharacters),
            WriteEndDocumentOnClose = formatting.HasFlag(XmlFormatting.WriteEndDocumentOnClose)
        });
    }

    internal static void WriteStartElement(XmppElement element, XmlWriter writer)
    {
        XmppName skipAttribute = element.Prefix != null
                ? string.Concat("xmlns:", element.Prefix)
                : "xmlns";

        writer.WriteStartElement(element.Prefix, element.LocalName, element.GetNamespace(element.Prefix));

        foreach (var (name, value) in element.Attributes)
        {
            if (name == skipAttribute)
                continue;

            if (!name.HasPrefix)
                writer.WriteAttributeString(name.LocalName, value);
            else
            {
                writer.WriteAttributeString(name.LocalName, name.Prefix switch
                {
                    "xml" => Namespaces.Xml,
                    "xmlns" => Namespaces.Xmlns,
                    _ => element.GetNamespace(name.Prefix) ?? string.Empty
                }, value);
            }
        }
    }

    internal static void WriteXmlTree(XmppNode node, XmlWriter writer)
    {
        /*if (node is XmppDocument document)
        {
            if (document.Standalone == XmppDocumentStandalone.Unspecified)
                writer.WriteStartDocument();
            else
                writer.WriteStartDocument(document.Standalone == XmppDocumentStandalone.Yes);

            foreach (var n in document)
                WriteXmlTree(n, writer);

            writer.WriteEndDocument();
        }*/

        if (node is XmppElement element)
        {
            WriteStartElement(element, writer);

            foreach (var childNode in element)
                WriteXmlTree(childNode, writer);

            writer.WriteEndElement();
        }

        if (node is XmppText text)
            writer.WriteString(text.Value);

        if (node is XmppComment comment)
            writer.WriteComment(comment.Value);

        if (node is XmppCdata cdata)
            writer.WriteCData(cdata.Value);
    }

    public static XmppElement C(this XmppElement parent, XmppName tagName, string xmlns = default, string value = default)
    {
        var child = new XmppElement(tagName, xmlns, value);
        parent.AddChild(child);
        return child;
    }

    public static XmppElement Up(this XmppElement child)
        => child.Parent as XmppElement;
}

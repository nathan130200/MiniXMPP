using System.Globalization;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;
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

#pragma warning disable

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

#pragma warning restore

    public static XmlWriter CreateWriter(StringBuilder textWriter, XmlFormatting formatting, Encoding? encoding = default)
    {
        var conformanceLevel = formatting.HasFlag(XmlFormatting.OmitXmlDeclaration)
            ? ConformanceLevel.Fragment
            : ConformanceLevel.Document;

        var namespaceHandling = formatting.HasFlag(XmlFormatting.OmitDuplicatedNamespaces)
            ? NamespaceHandling.OmitDuplicates
            : NamespaceHandling.Default;

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
            WriteEndDocumentOnClose = formatting.HasFlag(XmlFormatting.WriteEndDocumentOnClose),
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
        if (node is XmppElement element)
        {
            WriteStartElement(element, writer);

            foreach (var childNode in element.Nodes())
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

    public static XmppElement C(this XmppElement parent, XmppName tagName, Action<XmppElement> callback)
    {
        parent.ThrowIfNull();
        tagName.ThrowIfNull();
        callback.ThrowIfNull();

        var child = new XmppElement(tagName, parent.GetNamespace(tagName.Prefix));
        callback(child);
        parent.Add(child);
        return parent;
    }

    public static XmppElement C(this XmppElement parent, XmppName tagName, string? xmlns = default, string? value = default)
    {
        parent.ThrowIfNull();
        tagName.ThrowIfNull();

        var child = new XmppElement(tagName, xmlns ?? parent.GetNamespace(tagName.Prefix), value);
        parent.Add(child);
        return child;
    }

    public static XmppElement? Up(this XmppElement child)
    {
        child.ThrowIfNull();
        return child.Parent;
    }

    public static XmppElement? Parse(string xml)
    {
        using (var reader = new StringReader(xml))
            return ParseCore(reader);
    }

    public static XmppElement? Parse(Stream stream, Encoding? encoding = default, bool leaveOpen = true)
    {
        using (var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, true, XmppParser.DefaultBufferSize, leaveOpen))
            return ParseCore(reader);
    }

    internal static XmppElement? ParseCore(TextReader inputSource)
    {
        using var reader = XmlReader.Create(inputSource, new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            CheckCharacters = true,
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = XmlThrowingResolver.Shared,
            IgnoreWhitespace = true,
            ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes,
            CloseInput = true
        });

        XmppElement? current = default;

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        var newElement = new XmppElement(reader.Name);

                        while (reader.MoveToNextAttribute())
                            newElement.Attributes[reader.Name] = reader.Value;

                        reader.MoveToElement();

                        if (reader.IsEmptyElement)
                        {
                            if (current != null)
                                current.Add(newElement);
                            else
                                return newElement;
                        }
                        else
                        {
                            current?.Add(newElement);
                            current = newElement;
                        }
                    }
                    break;

                case XmlNodeType.EndElement:
                    {
                        var parent = current?.Parent;

                        if (parent is null) // root element closed
                            return current;

                        current = parent;
                    }
                    break;

                case XmlNodeType.Text:
                    current?.Add(new XmppText(reader.Value));
                    break;

                case XmlNodeType.Comment:
                    current?.Add(new XmppComment(reader.Value));
                    break;

                case XmlNodeType.CDATA:
                    current?.Add(new XmppCdata(reader.Value));
                    break;


            }
        }

        return current;
    }

    public static XmppElement StreamStream()
        => new("stream:stream", Namespaces.Stream);

    public static XmppElement StreamFeatures()
        => new("stream:features", Namespaces.Stream);

    public static XmppElement StreamError(string? condition)
    {
        var el = new XmppElement("stream:error", Namespaces.Stream);

        if (condition != null)
            el.C(condition, Namespaces.Streams);

        return el;
    }

    public static XmppElement Error(string? type, string? condition = default, string? text = default)
    {
        var el = new XmppElement("error")
        {
            Attributes =
            {
                ["type"] = type,
            }
        };

        if (condition != null)
            el.C(condition, Namespaces.Stanzas);

        if (text != null)
            el.C("text", Namespaces.Stanzas, text);

        return el;
    }
}

internal class XmlThrowingResolver : XmlResolver
{
    public static XmlThrowingResolver Shared { get; } = new();

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        => throw new InvalidOperationException();

    public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        => throw new InvalidOperationException();

    public override bool SupportsType(Uri absoluteUri, Type? type)
        => throw new InvalidOperationException();

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
        => throw new InvalidOperationException();

    public override ICredentials Credentials
    {
        set => throw new InvalidOperationException();
    }
}
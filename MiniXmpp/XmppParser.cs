using System.Text;
using System.Xml;
using System.Xml.Schema;
using MiniXmpp.Dom;

namespace MiniXmpp;

public delegate void XmppElementHandler(XmppElement e);

public sealed class XmppParser : IDisposable
{
    private volatile bool _disposed;
    private XmlReader? _reader;

    public event XmppElementHandler? OnStreamStart;
    public event XmppElementHandler? OnStreamElement;
    public event Action? OnStreamEnd;

    static volatile int s_DefaultCharBufferSize = 4096;

    public static int DefaultBufferSize
    {
        get => s_DefaultCharBufferSize;
        set => s_DefaultCharBufferSize = Math.Clamp(value, 1024, 9216);
    }

    public XmppParser(Stream stream, NameTable? nameTable = default)
    {
        var settings = new XmlReaderSettings
        {
            CloseInput = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            NameTable = nameTable,
            XmlResolver = XmlThrowingResolver.Shared,
            ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes
        };

        _reader = XmlReader.Create(new StreamReader(stream, Encoding.UTF8, true, s_DefaultCharBufferSize, true), settings);
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        XmppElement? current = default;

        try
        {
            while (!_disposed)
            {
                await Task.Delay(1, token);

                if (_reader == null)
                    break;

                var result = await _reader.ReadAsync();

                if (!result)
                    continue;

                switch (_reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            XmppElement newElement;

                            if (_reader.Name is "iq" or "message" or "presence")
                                newElement = new XmppStanza(_reader.Name);
                            else
                                newElement = new XmppElement(_reader.Name);

                            while (_reader.MoveToNextAttribute())
                                newElement.Attributes[_reader.Name] = _reader.Value;

                            _reader.MoveToElement();

                            if (newElement.TagName == "stream:stream" && newElement.GetNamespace("stream") == Namespaces.Stream)
                            {
                                OnStreamStart?.Invoke(newElement);
                            }
                            else
                            {
                                if (_reader.IsEmptyElement)
                                {
                                    if (current == null)
                                        OnStreamElement?.Invoke(newElement);
                                    else
                                        current.Add(newElement);
                                }
                                else
                                {
                                    current?.Add(newElement);
                                    current = newElement;
                                }
                            }
                        }
                        break;

                    case XmlNodeType.EndElement:
                        {
                            if (_reader.Name == "stream:stream")
                            {
                                OnStreamEnd?.Invoke();
                            }
                            else
                            {
                                var parent = current?.Parent;

                                if (parent is null)
                                    OnStreamElement?.Invoke(current!);

                                current = parent;
                            }
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                        current?.Add(new XmppText(_reader.Value));
                        break;

                    case XmlNodeType.CDATA:
                        current?.Add(new XmppCdata(_reader.Value));
                        break;

                    case XmlNodeType.Comment:
                        current?.Add(new XmppComment(_reader.Value));
                        break;
                }
            }
        }
        catch (TaskCanceledException) { /* skip */ }
        catch
        {
            if (!_disposed || !token.IsCancellationRequested)
                throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _reader?.Dispose();
        _reader = null;
    }
}

using System.Text;
using System.Xml;
using MiniXmpp.Dom;

namespace MiniXmpp;

public delegate void XmppElementHandler(XmppElement e);

public sealed class XmppParser : IDisposable
{
    private volatile bool _disposed;
    private XmlReader _reader;

    public event XmppElementHandler OnStreamStart;
    public event XmppElementHandler OnStreamElement;
    public event Action OnStreamEnd;

    static volatile int s_DefaultCharBufferSize = 4096;

    public static int DefaultCharBufferSize
    {
        get => s_DefaultCharBufferSize;
        set => s_DefaultCharBufferSize = Math.Clamp(value, 1024, 9216);
    }

    public XmppParser(Stream stream, NameTable nameTable = default)
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
        };

        _reader = XmlReader.Create(new StreamReader(stream, Encoding.UTF8, true, s_DefaultCharBufferSize, true), settings);
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        XmppElement root = default;

        try
        {
            while (!_disposed)
            {
                await Task.Delay(1);

                var process = await Task.Run(() => _reader.ReadAsync(), token);

                if (!process)
                    continue;

                switch (_reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            var current = new XmppElement(_reader.Name);

                            while (_reader.MoveToNextAttribute())
                                current.Attributes[_reader.Name] = _reader.Value;

                            _reader.MoveToElement();

                            if (current.TagName == "stream:stream" && current.GetNamespace("stream") == Namespaces.Stream)
                            {
                                OnStreamStart?.Invoke(current);
                            }
                            else
                            {
                                if (_reader.IsEmptyElement)
                                {
                                    if (root == null)
                                        OnStreamElement?.Invoke(current);
                                    else
                                        root.AddChild(current);
                                }
                                else
                                {
                                    root?.AddChild(current);
                                    root = current;
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
                                var parent = root?.Parent as XmppElement;

                                if (parent is null)
                                    OnStreamElement?.Invoke(root);

                                root = parent;
                            }
                        }
                        break;

                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Text:
                        root?.AddChild(new XmppText(_reader.Value));
                        break;

                    case XmlNodeType.CDATA:
                        root?.AddChild(new XmppCdata(_reader.Value));
                        break;

                    case XmlNodeType.Comment:
                        root?.AddChild(new XmppComment(_reader.Value));
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

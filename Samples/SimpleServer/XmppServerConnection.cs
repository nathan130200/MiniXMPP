using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using MiniXmpp;
using MiniXmpp.Dom;
using MiniXmpp.Enums;

namespace SimpleServer;

public sealed class XmppServerConnection : IDisposable
{
    private Socket _socket;
    private Stream _stream;
    private volatile bool _disposed;
    private XmppParser _parser;
    private FileAccess _access = FileAccess.ReadWrite;
    private ConcurrentQueue<Entry> _sendQueue = [];

    ~XmppServerConnection()
    {
        _sendQueue?.Clear();
        _sendQueue = null;
    }

    public string StreamId { get; private set; }
    public Jid Jid { get; private set; }
    public bool IsAuthenticated { get; private set; }

    readonly struct Entry
    {
        public string DebugXml { get; init; }
        public byte[] Buffer { get; init; }
    }

    public XmppServerConnection(Socket socket)
    {
        _socket = socket;
        _stream = new NetworkStream(_socket, true);
    }

    internal async Task InitializeAsync()
    {
        ResetParser();
        await Task.WhenAny(BeginSend(), BeginReceive());
    }

    public event Func<string, string, bool> OnAuth;
    public event Func<string, string> OnResourceBind;

    readonly static string XmppStreamEndTag = Xml.StreamStream().EndTag();

    void ResetParser()
    {
        _parser?.Dispose();
        _parser = new XmppParser(_stream);
        SetupParser();
    }

    void SetupParser()
    {
        _parser.OnStreamStart += e =>
        {
            Console.WriteLine("recv <<\n{0}\n", e.StartTag());

            Jid = new(e.To ?? Environment.MachineName.ToLower());

            e.SwitchDirection();

            if (StreamId != null)
                e.Id = StreamId;
            else
            {
                e.GenerateId();
                StreamId = e.Id;
            }

            Send(e.StartTag());

            var features = Xml.StreamFeatures();

            if (!IsAuthenticated)
            {
                features.C("mechanisms", Namespaces.Sasl)
                    .C("mechanism", value: "PLAIN");
            }
            else
            {
                features.C("bind", Namespaces.Bind);
                features.C("session", Namespaces.Session);
            }

            Send(features);
        };

        _parser.OnStreamEnd += () =>
        {
            Console.WriteLine("recv <<\n{0}\n", XmppStreamEndTag);

            _access &= ~FileAccess.Read;
            Send(XmppStreamEndTag);
            Dispose();
        };

        _parser.OnStreamElement += e =>
        {
            Console.WriteLine("recv <<\n{0}\n", e.ToString(true));

            if (!IsAuthenticated)
            {
                if (e.TagName == "auth" && e.GetNamespace() == Namespaces.Sasl)
                {
                    string error = default;

                    if (e.Attributes["mechanism"] != "PLAIN")
                    {
                        error = "invalid-mechanism";
                        goto error;
                    }

                    var sasl = Convert.FromBase64String(e.Value)
                        .GetString()
                        .Split('\0');

                    string user, pass;

                    if (sasl.Length == 2)
                    {
                        user = sasl[0];
                        pass = sasl[1];
                    }
                    else if (sasl.Length == 3)
                    {
                        user = sasl[1];
                        pass = sasl[2];
                    }
                    else
                    {
                        error = "invalid-encoding";
                        goto error;
                    }

                    if (OnAuth == null)
                        goto success;
                    else
                    {
                        if (!OnAuth(user, pass))
                        {
                            error = "not-authorized";
                            goto error;
                        }
                    }

                success:
                    {
                        Jid = Jid with { Local = user };
                        IsAuthenticated = true;
                        Send(new XmppElement("success", Namespaces.Sasl));
                        ResetParser();
                    }

                error:
                    {
                        var failure = new XmppElement("failure", Namespaces.Sasl)
                                .C(error)
                            .Up();

                        Send(failure);

                        Dispose();

                        return;
                    }
                }
            }
            else
            {
                if (e is XmppStanza stz)
                {
                    if (stz is { LocalName: "iq" } iq)
                    {
                        var query = iq.Elements().FirstOrDefault();

                        if (query == null)
                        {
                            iq.SwitchDirection();
                            iq.Type = "result";
                            Send(iq);
                            return;
                        }
                        else
                        {
                            if (query is { LocalName: "bind", Namespace: Namespaces.Bind } bind)
                            {
                                var resource = bind.Element("resource").Value;

                                if (string.IsNullOrWhiteSpace(resource))
                                    resource = Guid.NewGuid().ToString("D");

                                resource = OnResourceBind?.Invoke(resource);

                                iq.SwitchDirection();

                                if (resource == null)
                                {
                                    iq.Type = "error";
                                    iq.Add(Xml.StanzaError(StanzaErrorType.Cancel, StanzaErrorCondition.Conflict));
                                }
                                else
                                {
                                    iq.Type = "result";
                                    Jid = Jid with { Resource = resource };
                                    query.Element("resource").Remove();
                                    query.C("jid", value: Jid);
                                }

                                Send(iq);

                                return;
                            }

                            if (query is { LocalName: "session", Namespace: Namespaces.Session })
                            {
                                iq.SwitchDirection();
                                iq.Type = "result";
                                Send(iq);
                                return;
                            }
                        }
                    }

                    if (stz is { LocalName: "presence" or "message" }) // TODO: Handle presence and messages.
                        return;

                    stz.SwitchDirection();
                    stz.Type = "error";
                    stz.Add(Xml.StanzaError(StanzaErrorType.Cancel, StanzaErrorCondition.FeatureNotImplemented));
                    Send(stz);
                }
            }
        };
    }

    async Task BeginReceive()
    {
        while (!_disposed)
        {
            await Task.Delay(1);

            if (!_access.HasFlag(FileAccess.Read))
                continue;

            try
            {
                if (!await _parser.UpdateAsync())
                    break;
            }
            catch
            {
                if (_disposed)
                    throw;
            }
        }
    }

    async Task BeginSend()
    {
        while (!_disposed)
        {
            await Task.Delay(1);

            if (!_access.HasFlag(FileAccess.Write))
                continue;

            if (_sendQueue.TryDequeue(out var entry))
            {
                await _stream.WriteAsync(entry.Buffer);

                if (entry.DebugXml != null)
                    Console.WriteLine("send >>\n{0}\n", entry.DebugXml);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _access &= ~FileAccess.Read;

        _parser.Dispose();
        _parser = null;

        // Wait for pending packets.

        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            _stream.Dispose();
            _stream = null;

            _socket.Dispose();
            _socket = null;
        });
    }

    public void Send(string xml)
    {
        if (_disposed)
            return;

        _sendQueue.Enqueue(new()
        {
#if DEBUG
            DebugXml = xml,
#endif
            Buffer = xml.GetBytes()
        });
    }

    public void Send(XmppElement element)
    {
        if (_disposed)
            return;

        _sendQueue.Enqueue(new()
        {
#if DEBUG
            DebugXml = element.ToString(true),
#endif
            Buffer = element.ToString(false).GetBytes()
        });
    }
}
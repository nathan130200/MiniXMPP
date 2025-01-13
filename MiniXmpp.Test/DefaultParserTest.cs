using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MiniXmpp.Dom;
using Xunit.Abstractions;

namespace MiniXmpp.Test;

public class DefaultParserTest(ITestOutputHelper output)
{
    [Fact]
    public async Task ParseFromNetwork()
    {
        using var server = new Socket(SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Loopback, 5222));
        server.Listen(1);

        using var client = server.Accept();
        using var stream = new NetworkStream(client);

        var parser = new XmppParser(stream);
        using var cts = new CancellationTokenSource();

        var sendQueue = new ConcurrentQueue<object>();

        var isAuthenticated = false;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1);

                    if (sendQueue.TryDequeue(out var entry))
                    {
                        if (entry is not string str)
                            str = entry.ToString() ?? string.Empty;

                        await stream.WriteAsync(str.GetBytes());

                        output.WriteLine("send >>\n" + str + "\n");
                    }
                }
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
                cts.Cancel();
            }
        });

        parser.OnStreamStart += e =>
        {
            output.WriteLine("recv <<\n" + e.StartTag() + "\n");

            e.SwitchDirection();
            e.GenerateId();

            sendQueue.Enqueue(e.StartTag());

            var features = Xml.StreamFeatures();
            {
                if (!isAuthenticated)
                {
                    var mechanisms = features.C("mechanisms", Namespaces.Sasl);
                    mechanisms.C("mechanism", value: "PLAIN");
                }
                else
                {
                    features.C("bind", Namespaces.Bind);
                    features.C("session", Namespaces.Session);
                }
            }
            sendQueue.Enqueue(features);
        };

        parser.OnStreamElement += e =>
        {
            output.WriteLine("recv <<\n" + e.ToString(true) + "\n");

            if (e is { LocalName: "auth" })
            {
                sendQueue.Enqueue(new XmppElement("success", Namespaces.Sasl));

                parser.Dispose();
                parser = new XmppParser(stream);

                isAuthenticated = true;
            }

            if (e is XmppStanza stz)
            {
                if (stz is { LocalName: "iq" } && stz.Element("bind") != null) // Just testing children elements parsing.
                {
                    cts.Cancel();
                    return;
                }
            }
        };

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    await parser.UpdateAsync();
                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
            }
            finally
            {
                cts.Cancel();
            }
        });

        while (!cts.IsCancellationRequested)
            await Task.Delay(1);

        parser.Dispose();
        parser = null;
    }
}

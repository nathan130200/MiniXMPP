using System.Net;
using System.Net.Sockets;
using MiniXmpp.Collections;

namespace SimpleServer;

static class Program
{
    static readonly List<XmppServerConnection> s_Connections = [];

    static async Task Main(string[] args)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 5222));
        socket.Listen(10);

        while (true)
        {
            try
            {
                var client = await socket.AcceptAsync();
                _ = Task.Run(async () => await HandleConnection(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    static async Task HandleConnection(Socket s)
    {
        using var connection = new XmppServerConnection(s);

        try
        {
            connection.OnAuth += (user, pass) =>
            {
                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    return false;

                // TODO: Handle auth

                return true;
            };

            connection.OnResourceBind += resource =>
            {
                var search = connection.Jid with { Resource = resource };

                lock (s_Connections)
                {
                    if (s_Connections.Any(x => x.IsAuthenticated && FullJidComparer.AreEquals(x.Jid, search)))
                    {
                        // TODO: Handle resource conflict: assign new resource or drop current/previus connection.
                        return null;
                    }
                }

                return resource;
            };

            await connection.InitializeAsync();

            lock (s_Connections)
                s_Connections.Add(connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        lock (s_Connections)
            s_Connections.Remove(connection);
    }
}
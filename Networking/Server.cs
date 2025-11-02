using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Wasm;
using halloween.Simulation;

namespace halloween.Networking;

internal class Server
{
    private TcpListener _tcpListener = null;
    private UdpClient _udpListener = null;
    private bool _running = false;
    private Dictionary<int, TcpClientState> _tcpClients = [];
    private int _nextClientId = 1;
    private object _clientsLock = new();
    private bool _hostEntered = false;

    private class TcpClientState
    {
        public int ClientId { get; set; }
        public NetworkStream Stream { get; set; }
        public byte[] Buffer { get; set; }
        public TcpClient Client { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public bool isHost;
        public Team team;
    }

    public void Start(ushort port)
    {
        try
        {
            // Start the TCP Listener (Connection-oriented) 
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, null);

            // Start the UDP Listener (Connectionless)
            _udpListener = new UdpClient(port);
            _udpListener.BeginReceive(OnReceiveDataWithUDP, null);

            _running = true;

            Importer.ImportAll();

            Console.Write("\nServer is running...");
            while(_running)
            {
                // Server behaviour here...
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server exception: " + ex.Message);
        }
        finally
        {
            Console.WriteLine("\nShutting down server...");
            // Clean up all resources
            lock (_clientsLock)
            {
                foreach (var client in _tcpClients.Values)
                {
                    client.Client.Close();
                }
                _tcpClients.Clear();
            }

            _udpListener?.Close();
            _tcpListener.Stop();
        }
    }

    private void OnAcceptTcpClient(IAsyncResult result)
    {
        try
        {
            TcpClient client = _tcpListener.EndAcceptTcpClient(result);
            NetworkStream stream = client.GetStream();

            //Assign an ID to the client
            int clientId;
            lock (_clientsLock)
            {
                clientId = _nextClientId++;
            }

            var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"TCP Client connected (ID: {clientId}): {endPoint.Address}:{endPoint.Port}");

            TcpClientState state = new();
            state.Stream = stream;
            state.Client = client;
            state.Buffer = new byte[4096];
            state.ClientId = clientId;
            state.EndPoint = null;
            if (!_hostEntered)
            {
                state.isHost = true;
                _hostEntered = true;
                Console.WriteLine($"TCP Client @ {endPoint.Address}:{endPoint.Port} is now the match admin.");
            }
            state.team = new();

            _tcpClients.Add(clientId, state);



            stream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnReceiveDataWithTCP, state);

            _tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, null);

            if (!state.isHost)
            {
                using (Packet packet = new Packet())
                {
                    packet.WriteInt((int)PacketId.UNIT_LIST);
                    packet.WriteString(File.ReadAllText(Importer.UNITS_PATH));
                    var data = packet.GetByteArray();
                    stream.Write(data, 0, data.Length);
                }

            }

            Thread.Sleep(2000);
            RequestFromTcpClient(clientId, "team_name");
        }
        catch (ObjectDisposedException)
        {
            // Listener was stopped, ignore.
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error accepting TCP client: " + ex.Message);
        }
    }

    private void OnReceiveDataWithTCP(IAsyncResult result)
    {
        TcpClientState state = (TcpClientState)result.AsyncState;

        try
        {
            int bytesRead = state.Stream.EndRead(result);
            if (bytesRead > 0)
            {
                byte[] data = new byte[bytesRead];
                for (int i = 0; i < bytesRead; i++)
                {
                    data[i] = state.Buffer[i];
                }

                using (Packet packet = new Packet(data))
                {
                    int id = packet.ReadInt();
                    switch(id)
                    {
                        case (int) PacketId.MESSAGE:
                    
                            string message = packet.ReadString();
                            Console.WriteLine($"\n[TCP Received: {message} from client id: {state.ClientId}]");
                            break;

                        case (int)PacketId.RESPONSE:
                            string responseType = packet.ReadString();
                            switch(responseType)
                            {
                                case "team_name":
                                    if (!string.IsNullOrWhiteSpace(state.team.name))
                                    {
                                        break;
                                    }
                                    string name = packet.ReadString();
                                    bool nameAvailable = true;

                                    foreach (TcpClientState client in _tcpClients.Values)
                                    {
                                        if (client.team.name.Equals(name))
                                        {
                                            nameAvailable = false;
                                            break;
                                        }
                                    }
                                    
                                    if(nameAvailable)
                                    {
                                        state.team.name = name;
                                        SendMessageToTcpClient(state.ClientId, $"Team name set to: {state.team.name}");
                                    }else
                                    {
                                        SendMessageToTcpClient(state.ClientId, $"Team name  \"{name}\" is unavailable, try again.");
                                        Thread.Sleep(1000);
                                        RequestFromTcpClient(state.ClientId, "team_name");
                                    }

                                    break;
                            }
                            break;
                    }
                }

                state.Buffer = new byte[4096];
                state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnReceiveDataWithTCP, state);
            }else
            {
                throw new IOException();
            }
        }
        catch (ObjectDisposedException)
        {
            // Client was closed, ignore
        }
        catch (IOException)
        {
            // Client disconnected unexpectedly
            RemoveTcpClient(state.ClientId);
            IPEndPoint clientEndPoint = (IPEndPoint)state.Client.Client.RemoteEndPoint;
            Console.WriteLine($"TCP Client {state.ClientId} disconnected unexpectedly: {clientEndPoint.Address}:{clientEndPoint.Port}");
            state.Client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling TCP data from client {state.ClientId}: " + ex.Message);
            RemoveTcpClient(state.ClientId);
            state.Client.Close();
        }
    }

    private void OnReceiveDataWithUDP(IAsyncResult result)
    {
        try
        {
            // Create an endpoint to capture the sender's information
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData = _udpListener.EndReceive(result, ref endPoint);

            using (Packet packet = new Packet(receivedData))
            {
                int id = packet.ReadInt();

                if (id == (int) PacketId.MESSAGE)
                {
                    string message = packet.ReadString();
                    Console.WriteLine($"\n[UDP Received: {message}]");
                }

                foreach (var client in _tcpClients.Values)
                {
                    if (client.EndPoint != null)
                    {
                        continue;
                    }
                    IPEndPoint tcpEndPoint = (IPEndPoint)client.Client.Client.RemoteEndPoint;
                    if (tcpEndPoint.Address.Equals(endPoint.Address))
                    {
                        // Found matching client by IP address, set the UDP endpoint
                        client.EndPoint = endPoint;
                        Console.WriteLine($"Associated UDP endpoint for client {client.ClientId}: {endPoint}");
                        break;
                    }
                }
            }

            _udpListener.BeginReceive(OnReceiveDataWithUDP, null);
        }
        catch (ObjectDisposedException)
        {
            // Listener was closed, ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling UDP data: " + ex.Message);
            if (_running)
            {
                // Try to keep listening for UDP even if one packet failed
                _udpListener.BeginReceive(OnReceiveDataWithUDP, null);
            }
        }
    }

    private void SendToAllWithTCP()
    {
        string message = "Hello with TCP at" + DateTime.Now.ToString("HH:mm:ss.fff");
        using (Packet packet = new Packet())
        {
            packet.WriteInt((int) PacketId.MESSAGE); // Packet ID
            packet.WriteString(message); // String message
            var data = packet.GetByteArray();
            foreach (var client in _tcpClients)
            {
                client.Value.Stream.Write(data, 0, data.Length);
            }
        }
    }

    private void SendToAllWithUDP()
    {
        string message = "Hello with UDP at" + DateTime.Now.ToString("HH:mm:ss.fff");
        using (Packet packet = new Packet())
        {
            packet.WriteInt((int) PacketId.MESSAGE); // Packet ID
            packet.WriteString(message); // String message
            var data = packet.GetByteArray();
            foreach (TcpClientState client in _tcpClients.Values)
            {
                if (client.EndPoint != null)
                {
                    Console.WriteLine($"Sending to client {client.ClientId}");
                    _udpListener.BeginSend(data, data.Length, client.EndPoint, null, null);
                }
            }
        }
    }

    private void RequestFromTcpClient(int clientId, string requestType)
    {
        Packet packet = new Packet();
        packet.WriteInt((int)PacketId.REQUEST);
        packet.WriteString(requestType);
        var data = packet.GetByteArray();
        _tcpClients[clientId].Stream.Write(data, 0, data.Length);
    }

    private void SendMessageToTcpClient(int clientId, string message)
    {
        Packet packet = new Packet();
        packet.WriteInt((int)PacketId.MESSAGE);
        packet.WriteString(message);
        var data = packet.GetByteArray();
        _tcpClients[clientId].Stream.Write(data, 0, data.Length);
    }


    private void RemoveTcpClient(int clientId)
    {
        lock (_clientsLock)
        {
            if (_tcpClients.ContainsKey(clientId))
            {
                _tcpClients.Remove(clientId);
                Console.WriteLine($"Removed TCP client {clientId} from tracking");
            }
        }
    }
    
    public void Close()
    {
        _running = false;
    }

}
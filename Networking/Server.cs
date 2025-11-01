using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Wasm;

namespace halloween.Networking;

internal class Server
{
    private TcpListener _tcpListener = null;
    private UdpClient _udpListener = null;
    private bool _running = false;
    private Dictionary<int, TcpClientState> _tcpClients = [];
    private int _nextClientId = 1;
    private object _clientsLock = new();

    private class TcpClientState
    {
        public int ClientId { get; set; }
        public NetworkStream Stream { get; set; }
        public byte[] Buffer { get; set; }
        public TcpClient Client { get; set; }
        public IPEndPoint EndPoint { get; set; }
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


            while(_running)
            {
                Console.Write("\nServer is running. Press 'X' to stop, T or U to send a message to all clients with TCP or UDP.");
                var key = Console.ReadKey(true);
                switch(key.Key)
                {
                    case ConsoleKey.X:
                        _running = false;
                        break;

                    case ConsoleKey.T:
                        SendToAllWithTCP();
                        break;

                    case ConsoleKey.U:
                        SendToAllWithUDP();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server exception: " + ex.Message);
        }
        finally
        {
            Console.WriteLine("Shutting down server...");
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
            _tcpClients.Add(clientId, state);

            stream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnReceiveDataWithTCP, state);

            _tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        }
        catch (ObjectDisposedException)
        {
            // Listener was stopped, ignore.
        }
        catch (Exception ex)
        {

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
                    if(id == 1)
                    {
                        string message = packet.ReadString();
                        Console.WriteLine($"\n[TCP Received: {message} from client id: {state.ClientId}]");
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

                if (id == 1)
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
            packet.WriteInt(1); // Packet ID
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
            packet.WriteInt(1); // Packet ID
            packet.WriteString(message); // String message
            var data = packet.GetByteArray();
            foreach (var client in _tcpClients)
            {
                if (client.Value.EndPoint != null)
                {
                    _udpListener.BeginSend(data, data.Length, client.Value.EndPoint, null, null);
                }
            }
        }
    }
    


    private void RemoveTcpClient(int clientId)
    {
        lock(_clientsLock)
        {
            if(_tcpClients.ContainsKey(clientId))
            {
                _tcpClients.Remove(clientId);
                Console.WriteLine($"Removed TCP client {clientId} from tracking");
            }
        }
    }

}
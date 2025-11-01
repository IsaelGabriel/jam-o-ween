using System.Net;
using System.Net.Sockets;

namespace halloween.Networking;

internal class Client
{
    private UdpClient _udpClient = null;
    private TcpClient _tcpClient = null;
    private NetworkStream _tcpStream = null;
    private bool _running = false;
    private byte[] receivedBuffer = null;

    public void Start(string ip, ushort port)
    {
        try
        {
            // Setup TCP Client (Connection-oriented)
            _tcpClient = new TcpClient();
            _tcpClient.Connect(ip, port);
            _tcpStream = _tcpClient.GetStream();
            receivedBuffer = new byte[4096];
            _tcpStream.BeginRead(receivedBuffer, 0, receivedBuffer.Length, OnReceiveDataWithTCP, null);

            // Setup UDP Client (Connectionless)
            _udpClient = new UdpClient();
            _udpClient.Connect(ip, port);
            _udpClient.BeginReceive(OnReceiveDataWithUDP, null);

            _running = true;

            while (_running)
            {
                // Client behaviour here...
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Main exception: " + ex.Message);
        }
        finally
        {
            _udpClient?.Close();
            _tcpStream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("Client shut down.");
        }

    }

    private void OnReceiveDataWithTCP(IAsyncResult result)
    {
        try
        {
            int bytesRead = _tcpStream.EndRead(result);

            if (bytesRead > 0)
            {
                byte[] data = new byte[bytesRead];
                for (int i = 0; i < bytesRead; i++)
                {
                    data[i] = receivedBuffer[i];
                }
                
                using (Packet packet = new Packet(data))
                {
                    int id = packet.ReadInt();
                    if(id == 1)
                    {
                        string message = packet.ReadString();
                        Console.WriteLine($"\nTCP Received: {message}");
                    }
                }
            }
            else
            {
                // Server has closed the connection
                Console.WriteLine("TCP Server closed the connection.");
                _running = false;
                return;
            }

            receivedBuffer = new byte[4096];
            _tcpStream.BeginRead(receivedBuffer, 0, receivedBuffer.Length, OnReceiveDataWithTCP, null);
        }
        catch (IOException)
        {
            // This often happends if the server disconnects unexpectedly
            Console.WriteLine("TCP Connection was lost.");
            _running = false;
        }
        catch (ObjectDisposedException)
        {
            // This happens if the stream is closed during shutdown
            Console.Write("TCP Stream was closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in TCP receive callback: " + ex.Message);
            _running = false;
        }
    }

    private void OnReceiveDataWithUDP(IAsyncResult result)
    {
        try
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData = _udpClient.EndReceive(result, ref remoteEndPoint);

            using (Packet packet = new Packet(receivedData))
            {
                int id = packet.ReadInt();

                if (id == 1)
                {
                    string message = packet.ReadString();
                    Console.WriteLine($"\n[UDP Received: {message}]");
                }
            }

            _udpClient.BeginReceive(OnReceiveDataWithUDP, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in UDP receive callback: " + ex.Message);
        }
    }

    private void SendDataWithTcp()
    {
        try
        {
            string message = "Hello with TCP at" + DateTime.Now.ToString("HH:mm:ss.fff");
            using (Packet packet = new Packet())
            {
                packet.WriteInt(1); // Packet ID
                packet.WriteString(message); // String message
                var data = packet.GetByteArray();
                _tcpStream.Write(data, 0, data.Length);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error sending TCP data: " + ex.Message);
        }
    }
    
    private void SendDataWithUdp()
    {
        try
        {
            string message = "Hello with UDP at" + DateTime.Now.ToString("HH:mm:ss.fff");
            using (Packet packet = new Packet())
            {
                packet.WriteInt(1); // Packet ID
                packet.WriteString(message); // String message
                var data = packet.GetByteArray();
                _udpClient.Send(data, data.Length);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error sending UDP data: " + ex.Message);
        }
    }
}
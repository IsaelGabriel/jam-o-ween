using halloween.Networking;

string ip = "127.0.0.1";
ushort port = 7777;

Console.Clear();
Console.WriteLine("Start as (S)erver or (C)lient?");
var key = Console.ReadKey(true);

switch(key.Key)
{
    case ConsoleKey.S:
        Server server = new Server();
        server.Start(port);
        break;

    case ConsoleKey.C:
        Client client = new Client();
        client.Start(ip, port);
        break;
}
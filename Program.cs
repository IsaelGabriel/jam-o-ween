using halloween.Networking;

const ushort PORT = 7777;

Console.Clear();
Console.WriteLine("Start as (S)erver, (H)ost or (C)lient?");
var key = Console.ReadKey(true);

switch (key.Key)
{
    case ConsoleKey.S:
        Server server = new Server();
        server.Start(PORT);
        break;

    case ConsoleKey.H:
        Server hostServer = new Server();
        Client hostClient = new Client();
        new Task(() =>
        {
            hostServer.Start(PORT);

        }).Start();
        new Task(() =>
        {
            hostClient.Start("127.0.0.1", PORT);
        }).Start();
        break;

    case ConsoleKey.C:
        Client client = new Client();

        string ip = "";

        do
        {

            Console.Write("Insert IP: ");
            ip = Console.ReadLine()?? "";

        } while (!ValidateIPv4(ip));



        client.Start(ip, PORT);
        break;
}

bool ValidateIPv4(string ipString)
{
    if (string.IsNullOrWhiteSpace(ipString))
    {
        return false;
    }

    string[] split = ipString.Split('.');
    if(split.Length != 4)
    {
        return false;

    }

    byte tempForParsing;

    return split.All(r => byte.TryParse(r, out tempForParsing));

}
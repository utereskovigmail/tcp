using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;


string ipServer = "127.0.0.1";

int port = 2084;

TcpClient myClient = new TcpClient();
await myClient.ConnectAsync(IPAddress.Parse(ipServer), port);

Console.WriteLine($"Підключилися до сервера {myClient.Client.RemoteEndPoint}");
NetworkStream myStream = myClient.GetStream();

Console.WriteLine("Enter your name:");
string name = Console.ReadLine() ?? "NoName";
var bytes = Encoding.UTF8.GetBytes(name);
await myStream.WriteAsync(bytes, 0, bytes.Length);


_ = Task.Run(async () =>
{
    byte[] buffer = new byte[1024];
    try
    {
        while (true)
        {
            int bytesRead = await myStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Disconnected from server.");
                break;
            }
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"\n[From server]: {message}\nEnter inquiry:");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading from server: {ex.Message}");
    }
});

while (true)
{
    Console.WriteLine("Enter inquiry (or 'q' to quit):");
    string inquiry = Console.ReadLine();
    if (inquiry == "q")
        break;

    bytes = Encoding.UTF8.GetBytes(inquiry);
    await myStream.WriteAsync(bytes, 0, bytes.Length);
}

myStream.Close();
myClient.Close();
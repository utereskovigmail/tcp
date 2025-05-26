using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

int port = 2084;
TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine("Listening on port {0}", port);
ConcurrentDictionary<string, TcpClient> data = new();
while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClient(client, data));
}



static async Task HandleClient(TcpClient client, ConcurrentDictionary<string, TcpClient> data)
{
    NetworkStream stream = client.GetStream();
    List<string> images = new();

    byte[] buffer = new byte[1024];

    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    string name = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

    Console.WriteLine("New client joined - " + name + " from - " + client.Client.RemoteEndPoint.ToString());


    if (data.ContainsKey(name))
    {
    }
    else
    {
        data.TryAdd(name, client);
    }

    while (client.Connected)
    {
        buffer = new byte[1024];
        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        if (bytesRead == 0) break; 

        string choice = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        Console.WriteLine("Received inquiry - " + choice);
        if (choice.Trim().StartsWith("send"))
        {
            //send-username-message
            string[] parts = choice.Split("-");
            string message = parts[2];
            string tragetedUsername = parts[1];

            if (data.TryGetValue(tragetedUsername, out TcpClient targetedClient))
            {
                NetworkStream targetStream = targetedClient.GetStream();
                byte[] messageBytes = Encoding.UTF8.GetBytes($"[Message from [{name}]: {message}");
                await targetStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                Console.WriteLine($"{name}  -->  {tragetedUsername}: {message}");
            }
            else
            {
                string error = "No user found";
                byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
        }
        else if (choice.Trim().StartsWith("picture"))
        {
            //picture-path
            Console.WriteLine("Processing picture");
            int idx = choice.IndexOf('-');
            string[] parts = {choice.Substring(0, idx), choice.Substring(idx + 1)};
            string path = parts[1];
            // Console.WriteLine($"path - {path}");
            string url = "https://myp22.itstep.click/api/Galleries/upload";
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                HttpClient httpClient = new HttpClient();
                string Base64 = Convert.ToBase64String(bytes);
                var payload = new { photo = Base64 };
                var response = await httpClient.PostAsJsonAsync(url, payload);
            

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Image was sent successfully");
                    string result = await response.Content.ReadAsStringAsync();
                    // Console.WriteLine(result);
                    Cl classs = JsonSerializer.Deserialize<Cl>(result);
                    images.Add(classs.image);
                    byte[] messageBytes = Encoding.UTF8.GetBytes("Image was sent successfully");
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    Console.WriteLine("Image was not sent successfully");
                    byte[] messageBytes = Encoding.UTF8.GetBytes("Image was not sent successfully");
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                }
            }
            else
            {
                Console.WriteLine("Incorrect path");
            }
           
        }
        else if (choice.Trim().StartsWith("showAll"))
        {
            //showAll
            Console.WriteLine("---- All pictures  ----");
            foreach (string image in images)
            {
                Console.WriteLine(image);
            }
            string message = string.Join("\n", images);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }
        else if (choice.Trim().StartsWith("get"))
        {
            //get-name
            int idx = choice.IndexOf('-');
            string[] parts = {choice.Substring(0, idx), choice.Substring(idx + 1)};
            string n = parts[1];
            string url = $"https://myp22.itstep.click/images/{n}";
            Process.Start("open",url);
        }
        else
        {
            Console.WriteLine("Invalid inquiry");
        }
        





        // text = "thanks buddy - " + DateTime.Now.ToShortTimeString();
        // buffer = Encoding.UTF8.GetBytes(text);
        // stream.Write(buffer, 0, buffer.Length);


    }
    string m = "Thanks for working with us";
    byte[] b = Encoding.UTF8.GetBytes(m);
    await stream.WriteAsync(b, 0, b.Length);
    Console.WriteLine($"{name}  left us");
    stream.Close();
    client.Close();
}


public class Cl
{
    public string image { get; set; }
}



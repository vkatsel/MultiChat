using System.Net.Sockets;

namespace Server;

public class ClientNode
{
    public string Name { get; set; }
    public TcpClient Client { get; }
    public BinaryReader Reader { get; }
    public BinaryWriter Writer { get; }

    public ClientNode(TcpClient client, string name)
    {
        Name = name;
        Client = client;
        
        var stream = client.GetStream();
        Reader = new BinaryReader(stream);
        Writer = new BinaryWriter(stream);
    }

    public void CloseConnection()
    {
        Client.Close();
    }
}
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Server;

public class Server
{
    private TcpListener listener;
    private IPAddress ipAddress;
    private int port;
    private ConcurrentDictionary<int, Room> rooms = new ConcurrentDictionary<int, Room>();
    
    public Server(IPAddress ipAddress, int port)
    {
        this.ipAddress = ipAddress;
        this.port = port;
        listener = new TcpListener(ipAddress, port);
    }
    
    public void Start()
    {
        listener.Start();
        Logger.LogInfo("Server started");
        
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Logger.LogInfo($"Client connected");
            
            Task.Run(() => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        BinaryReader reader = new BinaryReader(stream);
        BinaryWriter writer = new BinaryWriter(stream);

        try
        {
            writer.Write("[SERVER] Connected. Please provide your name and a room number: <Name> <Room Number>");
            string name = reader.ReadString();
            int roomNumber = reader.ReadInt32();
            
            Room targetRoom = rooms.GetOrAdd(roomNumber, _ => new Room());
            
            writer.Write(true);
            writer.Write($"[SERVER] Joining the room {roomNumber}");
            
            targetRoom.AcceptClient(client, name);
            Logger.LogInfo($"Client {name} joined room {roomNumber}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Handshake failed: {e.Message}");
            client.Close();
        }
    }
}
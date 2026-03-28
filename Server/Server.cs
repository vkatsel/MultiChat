using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Server;

public class Server(IPAddress ipAddress, int port)
{
    private readonly TcpListener _listener = new(ipAddress, port);
    private readonly ConcurrentDictionary<int, Room> _rooms = new ConcurrentDictionary<int, Room>();

    public void Start()
    {
        _listener.Start();
        Logger.LogInfo("Server started");
        
        while (true)
        {
            TcpClient client = _listener.AcceptTcpClient();
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
            
            Room targetRoom = _rooms.GetOrAdd(roomNumber, _ => new Room(MoveClientToRoom));
            
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
    
    private void MoveClientToRoom(ClientNode clientNode, int newRoomId)
    {
        Logger.LogInfo($"Switching {clientNode.Name} to room {newRoomId}");
        clientNode.Writer.Write(true); clientNode.Writer.Write($"[SERVER] Switching to room {newRoomId}");
        
        Room newRoom = _rooms.GetOrAdd(newRoomId, _ => new Room(MoveClientToRoom));
        newRoom.AcceptExistingClient(clientNode); 
    }
}
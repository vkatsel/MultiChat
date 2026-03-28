using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server;

public class Room
{
    private ConcurrentDictionary<int, ClientNode> _clients = new();
    private Queue<string> _messages = new();
    private readonly object _lock = new();
    private int _nextId = 0;
    
    private readonly Action<ClientNode, int> _switchRoomCallback;
    
    
    public Room(Action<ClientNode, int> switchRoomCallback)
    {
        _switchRoomCallback = switchRoomCallback;
        Task.Run(BroadcastWorker);
    }
    
    public void AcceptClient(TcpClient client, string name)
    {
        try
        {
            int clientId = Interlocked.Increment(ref _nextId);
            ClientNode clientNode = new ClientNode(client, name);
            
            Logger.LogInfo($"Client {name} connected with ID {clientId}");
            
            _clients.TryAdd(clientId, clientNode);
            Task.Run(() => Recieve(clientNode, clientId));
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to add client {name}: {e.Message}");
            client.Close();
            throw;
        }
    }
    
    public void AcceptExistingClient(ClientNode node)
    {
        int clientId = Interlocked.Increment(ref _nextId);
    
        _clients.TryAdd(clientId, node);
        Logger.LogInfo($"Client {node.Name} switched to this room with ID {clientId}");
        
        Task.Run(() => Recieve(node, clientId));
        
        QueueMessage($"[SERVER] {node.Name} joined the room.");
    }

    public void QueueMessage(string message)
    {
        lock (_lock)
        {
            _messages.Enqueue(message);
            Monitor.PulseAll(_lock);
        }
    }
    
    private void Broadcast(string message)
    {
        Logger.LogInfo($"Broadcasting message: {message}");
        
        foreach (var client in _clients)
        {
            int clientId = client.Key;
            ClientNode clientNode = client.Value;

            try
            {
                clientNode.Writer.Write((byte)0x01);
                clientNode.Writer.Write(message);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to send message to client {clientNode.Name}: {e.Message}");
                clientNode.CloseConnection();
                _clients.TryRemove(clientId, out _);
            }
        }
    }

    private void Recieve(ClientNode сlient, int clientId)
    {
        // try
        // {
        //     while (true)
        //     {
        //         byte commandId = client.Reader.ReadByte();
        //
        //         switch (commandId)
        //         {
        //             case 1: 
        //                 string msg = client.Reader.ReadString();
        //                 QueueMessage(msg);
        //                 break;
        //         
        //             case 4:
        //                 int newRoomId = client.Reader.ReadInt32();
        //                 _clients.TryRemove(clientId, out _);
        //                 _switchRoomCallback(client, newRoomId);
        //                 return; 
        //         }
        //     }
        // }
        // catch (Exception)
        // {
        //     Logger.LogInfo($"Client {client.Name} abruptly disconnected.");
        //     _clients.TryRemove(clientId, out _);
        //     client.CloseConnection();
        // }
        throw new NotImplementedException();
    }
    
    
    private void BroadcastWorker()
    {
        while (true)
        {
            string message;

            lock (_lock)
            {
                while (_messages.Count == 0)
                {
                    Monitor.Wait(_lock);
                }
                
                message = _messages.Dequeue();
            }
            
            Broadcast(message);
        }
    }
}

public class ClientNode
{
    public string Name { get; }
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
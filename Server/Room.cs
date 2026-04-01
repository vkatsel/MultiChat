using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server;

public class Room
{
    private ConcurrentDictionary<int, ClientNode> _clients = new();
    private Queue<string> _messages = new();
    private readonly object _lock = new();
    
    private int _nextId = 0;
    private readonly string _roomPath;
    
    private readonly Action<ClientNode, int> _switchRoomCallback;
    
    
    public Room(Action<ClientNode, int> switchRoomCallback, int roomId)
    {
        _switchRoomCallback = switchRoomCallback;
        _roomPath = $"Rooms/{roomId}";
        Directory.CreateDirectory(_roomPath);
        Task.Run(BroadcastWorker);
    }

    public void AcceptClient(ClientNode node)
    {
        try
        {
            int clientId = Interlocked.Increment(ref _nextId);
    
            _clients.TryAdd(clientId, node);
            Logger.LogInfo($"Client {node.Name} switched to this room with ID {clientId}");
        
            Task.Run(() => Recieve(node, clientId));
        
            QueueMessage($"[SERVER] {node.Name} joined the room.");
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to add client {node.Name}: {e.Message}");
            node.Client.Close();
            throw;
        }
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

    private void Recieve(ClientNode client, int clientId)
    {
        try
        {
            while (true)
            {
                byte commandId = client.Reader.ReadByte();

                switch (commandId)
                {
                    case 0x01:
                        string msg = client.Reader.ReadString();
                        QueueMessage(msg);
                        break;
                    case 0x02:
                        string filename = client.Reader.ReadString();
                        long fileSize = client.Reader.ReadInt64();
                        UploadToRoom(Path.Combine(_roomPath, filename), fileSize, client);
                        break;
                    case 0x03:
                        filename = client.Reader.ReadString();
                        DownloadFromRoom(Path.Combine(_roomPath, filename), client);

                        Logger.LogInfo($"File {filename} sent to {client.Name}.");
                        break;
                    case 0x04:
                        int newRoomId = client.Reader.ReadInt32();
                        _clients.TryRemove(clientId, out _);
                        _switchRoomCallback(client, newRoomId);
                        return;
                    case 0x05:
                        Logger.LogInfo($"Client {client.Name} is closing the connection.");
    
                        client.Writer.Write((byte)0x05);
                        client.Writer.Write("That was a nice one. Have a nice day! :)");
                        
                        _clients.TryRemove(clientId, out _);
                        client.CloseConnection();
                        return;
                }
            }
        }
        catch (EndOfStreamException)
        {
            Logger.LogError($"Client {client.Name} disconnected.");
            _clients.TryRemove(clientId, out _);
            client.CloseConnection();
        }
        catch (Exception e)
        {
            Logger.LogError($"Error occurred while reading from client {client.Name}: {e.Message}");
            _clients.TryRemove(clientId, out _);
            client.CloseConnection();
        }
    }
    
    ////////////////////
    // Helper Methods //
    ////////////////////
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

    private void UploadToRoom(string path, long fileSize, ClientNode client)
    {
        try
        {
            using (FileStream fs = File.Create(path))
            {
                byte[] buffer = new byte[4096];
                long totalBytesRead = 0;

                while (totalBytesRead < fileSize)
                {
                    int bytesLeft = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                    int bytesRecieved = client.Reader.Read(buffer, 0, bytesLeft);
                    
                    if (bytesRecieved == 0 || bytesLeft == 0)
                        throw new Exception("Transmission failed. Connection Error");
                    
                    fs.Write(buffer, 0, bytesRecieved);
                    totalBytesRead += bytesRecieved;
                    Logger.LogInfo($"\rUploading file {Path.GetFileName(path)}...");
                }
                
                QueueMessage($"[Server] File {Path.GetFileName(path)} received from {client.Name}. Size: {fileSize} bytes.");
                Logger.LogSuccess($"[File {path} received from {client.Name}.");
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save file {path}: {e.Message}");
            throw;
        }
    }

    private void DownloadFromRoom(string path, ClientNode client)
    {
        string filename = Path.GetFileName(path);
        if (!File.Exists(path)) 
        {
            client.Writer.Write(false);
            client.Writer.Write($"File {filename} not found.");
            return;
        }
        
        client.Writer.Write(true);
        client.Writer.Write($"File {filename} found. Download? (Y/n): ");

        if (client.Reader.ReadBoolean())
        {
            client.Writer.Write((byte)0x03);
            
            long fileSize = new FileInfo(path).Length;
            client.Writer.Write(fileSize);

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] buffer = new byte[4096];
                int bytesRead = 0;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    client.Writer.Write(buffer, 0, bytesRead);
                    client.Writer.Flush();
                }
                
                Logger.LogSuccess($"File {filename} sent to {client.Name}.");
            }
        }
        else
        {
            Logger.LogWarning($"{client.Name} refused file {filename} download.");
        }
    }
}

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
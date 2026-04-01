using System.Net.Sockets;

namespace Client;

public class Client(string ipAddress = "127.0.0.1", int port = 12345)
{
    private const string Manual = "\nWelcome to MultiChat!\n" +
                                  "To send a message, type it and press enter.\n" +
                                  "Commands:\n" +
                                  "  /room <number>    - Switch room\n" +
                                  "  /upload <path>    - Upload a file\n" +
                                  "  /download <name>  - Download a file\n" +
                                  "  exit              - Terminate connection\n";

    private TcpClient _client;
    private NetworkStream _ns;
    private BinaryWriter _bw;
    private BinaryReader _br;
    private bool _isWaitingForDownloadConfirm;

    public void Start()
    {
        Console.WriteLine(Manual);

        while (true)
        {
            if (ConnectAndHandshake()) break;
            
            Logger.LogWarning("Do you wish to retry? (Y/n): ");
            if (Console.ReadLine()?.ToLower() == "n") return;
        }

        try
        {
            Task.Run(ListenToServer);
            SendToServer();
        }
        catch (Exception e)
        {
            Logger.LogError($"Error occurred: {e.Message}");
            _client.Close();
            throw;
        }
    }

    private bool ConnectAndHandshake()
    {
        try 
        {
            Logger.LogInfo($"Connecting to {ipAddress}:{port}...");
            
            _client = new TcpClient(ipAddress, port);
            _ns = _client.GetStream();
            _bw = new BinaryWriter(_ns);
            _br = new BinaryReader(_ns);

            var statusCode = _br.ReadBoolean();
            var message = _br.ReadString();

            if (statusCode)
            {
                Logger.LogInfo(message);
                var input = Console.ReadLine()?.Split();

                while (input == null || input.Length < 2 || 
                       string.IsNullOrWhiteSpace(input[0]) || !int.TryParse(input[1], out _))
                {
                    Logger.LogError("Invalid format. Please use: <Name> <Room Number>");
                    input = Console.ReadLine()?.Split();
                }

                _bw.Write(input[0]);
                _bw.Write(int.Parse(input[1]));

                if (_br.ReadBoolean())
                {
                    message = _br.ReadString();
                    Logger.LogSuccess(message);
                    return true;
                }

                message = _br.ReadString();
            }
            
            Logger.LogError(message);
            return false;
        } 
        catch (Exception e) 
        {
            Logger.LogError($"Connection failed: {e.Message}");
            return false;
        }
    }
    
    private void ListenToServer()
    {
        try
        {
            while (true)
            {
                byte commandId = _br.ReadByte();

                switch (commandId)
                {
                    case 0x01:
                        string message = _br.ReadString();
                        Logger.LogIncomingMessage(message); 
                        break;
                        
                    case 0x02:
                        bool isFound = _br.ReadBoolean();
                        string msg = _br.ReadString();
    
                        if (isFound)
                        {
                            Console.WriteLine(msg); 
                            _isWaitingForDownloadConfirm = true; 
                        }
                        else
                        {
                            Logger.LogError(msg);
                        }
                        break;
                        
                    case 0x03: 
                        string fileName = _br.ReadString();
                        long fileSize = _br.ReadInt64(); 
                        Logger.LogInfo($"Downloading {fileName} ({fileSize} bytes)...");
                        Download(fileName, fileSize);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Lost connection with server: {e.Message}");
            Environment.Exit(0); 
        }
    }
    
    private void SendToServer()
    {
        while (true)
        {
            string input = Console.ReadLine(); 
            if (string.IsNullOrWhiteSpace(input)) continue;
            
            if (_isWaitingForDownloadConfirm && !input.StartsWith("/"))
            {
                if (input.ToLower() == "y") _bw.Write(true);
                else
                {
                    _bw.Write(false);
                    Logger.LogWarning("Download cancelled.");
                }
                _isWaitingForDownloadConfirm = false;
                continue;
            }
            
            if (input.StartsWith("/"))
            {
                HandleCommand(input);
            }
            else if (input.ToLower() == "exit")
            {
                _bw.Write((byte)0x05);
                
                Logger.LogInfo("Disconnecting...");
                Environment.Exit(0);
            }
            else
            {
                _bw.Write((byte)0x01);
                _bw.Write(input);
            }
        }
    }

    private void HandleCommand(string input)
    {
        var parts = input.Split(' ', 2);
        string command = parts[0].ToLower();
        string argument = parts.Length > 1 ? parts[1] : string.Empty;

        switch (command)
        {
            case "/room":
                if (int.TryParse(argument, out int newRoomId))
                {
                    _bw.Write((byte)0x04);
                    _bw.Write(newRoomId);
                    Logger.LogInfo($"Request to switch to room {newRoomId} sent...");
                }
                else
                {
                    Logger.LogError("Bad input. Use formatting: /room <number>");
                }
                break;

            case "/upload":
                if (!string.IsNullOrWhiteSpace(argument)) Upload(argument);
                else Logger.LogError("Please specify a file path. Example: /upload photo.png");
                break;

            case "/download":
                if (!string.IsNullOrWhiteSpace(argument))
                {
                    Logger.LogInfo($"Requesting {argument} from server...");
                    _bw.Write((byte)0x03);
                    _bw.Write(argument);
                }
                else Logger.LogError("Please specify a file name. Example: /download photo.png");

                break;

            default:
                Logger.LogError($"Unknown command: {command}");
                break;
        }
    }
    
    private void Upload(string filepath)
    {
        if (!File.Exists(filepath)) 
        {
            Logger.LogError($"File {filepath} not found.");
            return;
        }
        
        _bw.Write((byte)0x02);
        _bw.Write(Path.GetFileName(filepath));

        long fileSize = new FileInfo(filepath).Length;
        _bw.Write(fileSize);
        long totalBytesSent = 0;
        
        using (FileStream fs = File.OpenRead(filepath))
        {
            byte[] buffer = new byte[4096];
            int bytesRead = 0;

            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                _bw.Write(buffer, 0, bytesRead);
                _bw.Flush();
                totalBytesSent += bytesRead;
                Console.Write($"\r {totalBytesSent}/{fileSize} bytes sent. {fileSize - bytesRead} bytes remaining.");
            }
            Console.WriteLine();
        }
        Logger.LogSuccess($"File {filepath} sent to server.");
    }
    
    private void Download(string filename, long fileSize)
    {
        using (FileStream fileStream = File.Create(filename))
        {
            byte[] buffer = new byte[4096];
            long totalBytesRead = 0;

            while (totalBytesRead < fileSize)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                int bytesReceived = _br.Read(buffer, 0, bytesToRead);

                if (bytesToRead == 0 || bytesReceived == 0)
                    break;

                fileStream.Write(buffer, 0, bytesReceived);
                totalBytesRead += bytesReceived;
                Console.Write($"\rProgress: {totalBytesRead}/{fileSize}");
            }
            Console.WriteLine();
            Logger.LogSuccess($"File {filename} received.");
        }
    }
}
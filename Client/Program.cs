using System.Net;
using System.Net.Sockets;

namespace Client;

class Program
{
    static void Main(string[] args)
    {
        TcpClient client = new TcpClient(IPAddress.Loopback.ToString(), 12345);
        NetworkStream ns = client.GetStream();
        BinaryWriter bw = new BinaryWriter(ns);
        BinaryReader br = new BinaryReader(ns);

        while (true)
        {
            if (Handshake(br, bw)) break;
            
            Logger.LogWarning("Do you wish to retry? (Y/n): ");
            if (Console.ReadLine()?.ToLower() == "n") break;
        }
        
        Task.Run(() => ListenToServer(br));
        SendToServer(bw);
        
    }

    private static bool Handshake(BinaryReader br, BinaryWriter bw)
    {
        try {
            var statusCode = br.ReadBoolean();
            var message = br.ReadString();

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

                bw.Write(input[0]);
                bw.Write(int.Parse(input[1]));

                if (br.ReadBoolean())
                {
                    message = br.ReadString();
                    Logger.LogSuccess(message);
                    return true;
                }

                message = br.ReadString();
            }
            
            Logger.LogError(message);
            throw new Exception(message);
        } 
        catch (Exception e) {
            Logger.LogError($"Handshake failed. Error message: {e.Message}");
            return false;
        }
    }
    
    private static void ListenToServer(BinaryReader br)
    {
        try
        {
            while (true)
            {
                byte commandId = br.ReadByte();

                switch (commandId)
                {
                    case 0x01:
                        string message = br.ReadString();
                        Logger.LogIncomingMessage(message);
                        break;
                        
                    case 0x03: 
                        string fileName = br.ReadString();
                        long fileSize = br.ReadInt64(); 
                        Logger.LogWarning($"\n[FILE OFFER] The file is uploaded to room: {fileName} ({fileSize} bytes).");
                        Logger.LogWarning("Enter '/download " + fileName + "' to download it.\n");
                        break;
                        
                    // TODO
                    // Complete other commands
                    // Think of better UX, change input formats,
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Lost connection with server: {e.Message}");
            Environment.Exit(0); 
        }
    }
    
    private static void SendToServer(BinaryWriter bw)
    {
        while (true)
        {
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            
            if (input.StartsWith("/room "))
            {
                if (int.TryParse(input.Substring(6), out int newRoomId))
                {
                    bw.Write((byte)0x04);
                    bw.Write(newRoomId);
                    Logger.LogInfo($"Request to switch to room {newRoomId} sent...");
                }
                else
                {
                    Logger.LogError("Bad input. Use formatting: /room <number>");
                }
            }
            else if (input.StartsWith("/upload "))
            {
                Logger.LogInfo("Still not done");
            }
            else
            {
                bw.Write((byte)0x01);
                bw.Write(input);
            }
        }
    }
}
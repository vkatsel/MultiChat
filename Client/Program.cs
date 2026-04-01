namespace Client;

class Program
{
    static void Main(string[] args)
    {
        Client client = new Client("127.0.0.1", 12345);
        
        try
        {
            client.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical failure: {ex.Message}");
            Console.ReadLine();
        }
    }
}
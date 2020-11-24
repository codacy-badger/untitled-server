using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UntitledSandbox_Server
{
    public class Client
    {
        public static string name;
        public static void ClientMain()
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            int port = 5086;
            Console.Clear();
            Console.WriteLine("> Enter your nickname:");
            name = Console.ReadLine();
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(localAddr, port);
            SendMessage(tcpClient.GetStream(), "Player,Join," + name);
            Console.WriteLine("> Player,Join,{0} message was send. Waiting for response...", name);
            Thread consoleThread = new Thread(ConsoleProcessor);
            consoleThread.Start(tcpClient);

            while (true)
            {
                try
                {
                    NetworkStream stream = tcpClient.GetStream();
                    ProcessMessage(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine("(X) Error occured: {0}", e.Message);
                    Console.Read();
                    Environment.Exit(0);
                }
            }
        }

        public static void ConsoleProcessor(object obj)
        {
            while (true)
            {
                TcpClient tcpClient = (TcpClient)obj;
                NetworkStream stream = tcpClient.GetStream();
                string message = Console.ReadLine();
                SendMessage(stream, message);
            }
        }

        public static void SendMessage(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
            Console.WriteLine("> Sended message {0}", message);
        }

        public static void ProcessMessage(NetworkStream stream)
        {
            byte[] data = new byte[256];
            int bytes = stream.Read(data, 0, data.Length);
            string message = Encoding.UTF8.GetString(data, 0, bytes);
            string[] args = message.Split(',');
            string content = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i == args.Length - 1) content += args[i];
                else content += args[i] + ",";
            }

            switch (args[0])
            {
                case "Disconnect":
                    Console.WriteLine("(X) Disconnected from server: " + args[1]);
                    Console.Read();
                    Environment.Exit(0);
                    break;
                case "Connect":
                    Console.WriteLine("> Connected to server successfully.");
                    break;
                case "Chat":
                    if (args.Length > 2)
                    {
                        switch (args[1])
                        {
                            case "Receive":
                                string ChatMessage = "";
                                for (int i = 3; i < args.Length; i++)
                                {
                                    if (i == args.Length - 1) ChatMessage += args[i];
                                    else ChatMessage += args[i] + ",";
                                }
                                if (ChatMessage == "") return;
                                Console.WriteLine("> {0}: {1}", args[2], ChatMessage);
                                break;
                            case "PlayerKicked":
                                Console.WriteLine("> {0} was kicked from the game.", args[2]);
                                break;
                            case "PlayerBanned":
                                Console.WriteLine("> {0} was banned from the game.", args[2]);
                                break;
                            case "PlayerJoined":
                                Console.WriteLine("> {0} has joined the game.", args[2]);
                                break;
                            case "PlayerLeaved":
                                Console.WriteLine("> {0} has leaved the game.", args[2]);
                                break;
                        }
                    }
                    else if (args.Length == 2)
                    {
                        switch (args[1])
                        {
                            case "Disabled":
                                Console.WriteLine("> Chat was disabled.");
                                break;
                            case "Login":
                                Console.WriteLine("> This server uses authification. Register or login now.");
                                break;
                        }
                    }
                    return;
                default:
                    Console.WriteLine("> Received message " + content);
                    break;
            }
        }
    }
}

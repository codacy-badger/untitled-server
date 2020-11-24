using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static UntitledSandbox_Server.FileManager;
using static UntitledSandbox_Server.ObjectsHelper;
using static UntitledSandbox_Server.SaveSystem;

namespace UntitledSandbox_Server
{
    [System.Serializable]
    public class Vector3
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class Player
    {
        public string IPAdress { get; set; }
        public string name { get; set; }
        public float hp { get; set; }
        public bool dead { get; set; }
        public Vector3 pos { get; set; }
        public Vector3 rot { get; set; }
        public bool loggedIn { get; set; }
    }

    public class Server
    {
        #region Variables
        public static List<Player> players = new List<Player>();
        public static List<TcpClient> clients = new List<TcpClient>();
        public static List<GameObject> objects = new List<GameObject>();
        public static string PlayerToKick = "";
        public static string PlayerToBan = "";
        #endregion

        #region Other
        public static Vector3 GetInitalPosition()
        {
            Vector3 pos = new Vector3();
            pos.x = -352f;
            pos.y = 0f;
            pos.z = 27f;
            return pos;
        }
        #endregion

        #region Main
        public static void ServerMain()
        {
            int port = 5086;
            TcpListener server = null;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
            Console.Clear();

            Thread consoleThread = new Thread(ConsoleProcessor);
            consoleThread.Start();

            while (true)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread clientThread = new Thread(ProcessConnection);
                    clientThread.Start(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine("(X) Error occured: {0}", e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine(e.Source);
                    Console.Read();
                    Environment.Exit(0);
                }
            }
        }
        #endregion

        #region Console Processor
        public static void ConsoleProcessor()
        {
            while (true)
            {
                string message = Console.ReadLine();
                string[] args = message.Split(',');
                if (!message.Contains(","))
                {
                    Console.WriteLine("> Provide one argument minimum.");
                }
                else
                {
                    switch (args[0])
                    {
                        case "Kick":
                            if (args[1] == "")
                            {
                                Console.WriteLine("> Provide player's name.");
                            }
                            else
                            {
                                for (int i = 0; i < players.Count; i++)
                                {
                                    if (players[i].name == args[1])
                                    {
                                        Console.WriteLine("> Player {0} will be kicked when a packet from him will be sent.", args[1]);
                                        PlayerToKick = args[1];
                                    }
                                }
                            }
                            break;
                        case "Ban":
                            if (args[1] == "")
                            {
                                Console.WriteLine("> Provide player's name.");
                            }
                            else
                            {
                                try
                                {
                                    if (!bool.Parse(ReadConfig(0)))
                                    {
                                        Console.WriteLine("> Banlist was disabled.");
                                    }
                                    else
                                    {
                                        for (int i = 0; i < players.Count; i++)
                                        {
                                            if (players[i].name == args[1])
                                            {
                                                Console.WriteLine("> Player {0} was banned.", args[1]);
                                                PlayerToBan = args[1];
                                                AppendBanlist(PlayerToBan);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("> Cannot load 'Banlist enabled' setting.");
                                }
                            }
                            break;
                    }
                }
            } 
        }
        #endregion

        #region Process Connection
        private static void ProcessConnection(object obj)
        {
            TcpClient client = (TcpClient)obj;
            clients.Add(client); 
            NetworkStream stream = client.GetStream();
            string name = ProcessPlayer(stream, client);
            if (name == null) return;
            Player plr = new Player();
            plr.IPAdress = client.Client.RemoteEndPoint.ToString();
            plr.name = name;
            plr.pos = GetInitalPosition();
            plr.rot = new Vector3();
            plr.rot.x = 0;
            plr.rot.y = 0;
            plr.rot.z = 0;
            plr.hp = 100;
            plr.dead = false;
            if (ReadConfig(0) == "true")
                plr.loggedIn = false;
            else
                plr.loggedIn = true;
            players.Add(plr);
            int index = players.Count - 1;
            SendMessageToEveryone("Chat,PlayerJoined," + plr.name);
            Task.Delay(60000).ContinueWith(t => {
                if (!players[index].loggedIn)
                {
                    SendMessage(stream, "Disconnect,LoginTimeout", plr.name);
                    Task.Delay(1000).ContinueWith(t => client.Close());
                }
            });

            while (client.Connected)
            {
                if (players[index].hp <= 0 && !players[index].dead)
                {
                    SendMessage(stream, "Player,Death", plr.name);
                    plr.dead = true;
                    players[index].dead = true;
                }

                if (PlayerToKick == plr.name)
                {
                    SendMessage(stream, "Disconnect,Kicked", plr.name);
                    Task.Delay(1000).ContinueWith(t => client.Close());
                    Console.WriteLine("> Player {0} was kicked.", plr.name);
                    players.Remove(plr);
                    clients.Remove(client);
                    SendMessageToEveryone("Chat,PlayerKicked," + plr.name);
                    PlayerToKick = "";
                }

                if (PlayerToBan == plr.name)
                {
                    SendMessage(stream, "Disconnect,Banned", plr.name);
                    Task.Delay(1000).ContinueWith(t => client.Close());
                    Console.WriteLine("> Player {0} was banned.", plr.name);
                    players.Remove(plr);
                    clients.Remove(client);
                    SendMessageToEveryone("Chat,PlayerBanned," + plr.name);
                    PlayerToBan = "";
                }

                try 
                { 
                    stream = client.GetStream();
                    if (!players[index].loggedIn)
                    {
                        ProcessLogin(stream, client, plr, index);
                    }
                    else
                    {
                        ProcessMessage(stream, client, plr, index);
                    }
                }
                catch { return; }
                
            }

            Console.WriteLine("> Player {0} was disconnected.", plr.name);
            players.Remove(plr);
            clients.Remove(client);
            SendMessageToEveryone("Chat,PlayerLeaved," + plr.name);
        }
        #endregion

        #region Process Message
        public static void ProcessMessage(NetworkStream stream, TcpClient client, Player plr, int index)
        {
            try
            {
                byte[] data = new byte[256];
                int bytes = 0;
                try { bytes = stream.Read(data, 0, data.Length); }
                catch { return; }
                string message = Encoding.UTF8.GetString(data, 0, bytes);
                string[] args = message.Split(',');
                string content = "";
                for (int i = 0; i < args.Length; i++)
                {
                    if (i == args.Length - 1) content += args[i];
                    else content += args[i] + ",";
                }

                if (content == "") return;

                Console.WriteLine("> Player {0} ({1}) sended: {2}", plr.name, plr.IPAdress, content);

                switch (args[0])
                {
                    #region Player
                    case "Player":
                        if (args.Length < 2) return;
                        switch (args[1])
                        {
                            #region GetData
                            case "GetData":
                                SendMessage(stream, "Player,ReturnData," + plr.pos.x + "," + plr.pos.y + "," + plr.pos.z + "," + plr.rot.x + "," + plr.rot.y + "," + plr.rot.z, plr.name);
                                break;
                            #endregion
                            #region SetPos
                            case "SetPos":
                                if (args.Length != 5) return;
                                if (plr.dead)
                                {
                                    SendMessage(stream, "Disconnect,IllegalPacket", plr.name);
                                    return;
                                }
                                Vector3 pos = new Vector3();
                                try
                                {
                                    pos.x = float.Parse(args[2]);
                                    pos.y = float.Parse(args[3]);
                                    pos.z = float.Parse(args[4]);

                                    players[index].pos = pos;
                                    return;
                                }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong SetPos packet.", plr.name);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                    clients.Remove(client);
                                }
                                break;
                            #endregion
                            #region SetRot
                            case "SetRot":
                                if (args.Length != 5) return;
                                if (plr.dead)
                                {
                                    SendMessage(stream, "Disconnect,IllegalPacket", plr.name);
                                    return;
                                }
                                Vector3 rot = new Vector3();
                                try
                                {
                                    rot.x = float.Parse(args[2]);
                                    rot.y = float.Parse(args[3]);
                                    rot.z = float.Parse(args[4]);

                                    players[index].rot = rot;
                                    return;
                                }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong SetRot packet.", plr.name);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                    clients.Remove(client);
                                }
                                break;
                            #endregion
                            #region GetHP
                            case "GetHP":
                                SendMessage(stream, "Player,ReturnHP," + plr.hp.ToString(), plr.name);
                                break;
                            #endregion
                            #region SetHP
                            case "SetHP":
                                if (args.Length != 3) return;
                                try { players[index].hp = float.Parse(args[2]); }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong SetHP packet.");
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                }
                                break;
                            #endregion
                            #region Respawn
                            case "Respawn":
                                if (!plr.dead)
                                {
                                    SendMessage(stream, "Disconnect,IllegalPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    return;
                                }
                                players[index].pos = GetInitalPosition();
                                players[index].rot.x = 0;
                                players[index].rot.y = 0;
                                players[index].rot.z = 0;
                                players[index].dead = false;
                                return;
                            #endregion
                            #region GetPlayers
                            case "GetPlayers":
                                string playersList = "";
                                for (int i = 0; i < players.Count; i++)
                                {
                                    if (i == players.Count - 1) playersList += players[i].name;
                                    else playersList += players[i].name + ",";
                                }
                                if (playersList == "") return;
                                SendMessage(stream, "Players,ReturnPlayers," + playersList, plr.name);
                                break;
                            #endregion
                            #region GetPlayerData
                            case "GetPlayerPos":
                                if (args.Length < 3) return;
                                for (int i = 0; i < players.Count; i++)
                                {
                                    if (players[i].name == args[2])
                                    {
                                        SendMessage(stream, "Players,ReturnPlayerData," + players[i].pos.x + "," + players[i].pos.y + "," + players[i].pos.z + "," + players[i].rot.x + "," + players[i].rot.y + "," + players[i].rot.z, plr.name);
                                    }
                                }
                                break;
                                #endregion
                        }
                        break;
                    #endregion
                    #region Chat
                    case "Chat":
                        if (args.Length < 2) return;
                        switch (args[1])
                        {
                            #region SendMessage
                            case "SendMessage":
                                bool isChatEnabled = true;
                                string ChatMessage = "";
                                for (int i = 2; i < args.Length; i++)
                                {
                                    if (i == args.Length - 1) ChatMessage += args[i];
                                    else ChatMessage += args[i] + ",";
                                }
                                if (ChatMessage == "") return;
                                try { isChatEnabled = bool.Parse(ReadConfig(1)); }
                                catch { }
                                for (int i = 0; i < clients.Count; i++)
                                {
                                    NetworkStream ns = clients[i].GetStream();
                                    if (isChatEnabled) SendMessage(ns, "Chat,Receive," + plr.name + "," + ChatMessage, plr.name);
                                    else SendMessage(ns, "Chat,Disabled", plr.name);
                                }
                                return;
                                #endregion
                        }
                        return;
                    #endregion
                    #region GameObjects
                    case "GameObjects":
                        switch (args[1])
                        {
                            #region Instantiate
                            case "Instantiate":
                                if (args.Length < 9) return; 
                                try
                                {
                                    GameObject obj = new GameObject();
                                    obj.prefabPath = args[2];
                                    obj.pos = new Vector3();
                                    obj.pos.x = float.Parse(args[3]);
                                    obj.pos.y = float.Parse(args[4]);
                                    obj.pos.z = float.Parse(args[5]);
                                    obj.rot = new Vector3();
                                    obj.rot.x = float.Parse(args[6]);
                                    obj.rot.y = float.Parse(args[7]);
                                    obj.rot.z = float.Parse(args[8]);
                                    objects.Add(obj);
                                    int ID = objects.Count - 1;
                                    objects[ID].ID = ID;

                                    SendMessageToEveryone("GameObjects,Instantiated," +
                                        ID + "," +
                                        obj.prefabPath + "," +
                                        obj.pos.x + "," +
                                        obj.pos.y + "," +
                                        obj.pos.z + "," +
                                        obj.rot.x + "," +
                                        obj.rot.y + "," +
                                        obj.rot.z);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("> Player {0} sended wrong Instantiate packet.", plr.name);
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                }                      
                                break;
                            #endregion
                            #region GetObjects
                            case "GetObjects":
                                string objectsList = "";
                                for (int i = 0; i < objects.Count; i++)
                                {
                                    if (i == objects.Count - 1) objectsList += objects[i].ID;
                                    else objectsList += objects[i].ID + ",";
                                }
                                if (objectsList == "") return;
                                SendMessage(stream, "Players,ReturnObjects," + objectsList, plr.name);
                                break;
                            #endregion
                            #region GetObjectInstance
                            case "GetObjectInstance":
                                if (args.Length < 3) return;
                                try
                                {
                                    string encodedObj = EncodeGameObject(objects[int.Parse(args[2])]);
                                    SendMessage(stream, "GameObjects,ReturnObjectInstance," + encodedObj, plr.name);
                                }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong GetObjectInstance packet.", plr.name);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                    clients.Remove(client);
                                }
                                break;
                            #endregion
                            #region UpdateObject
                            case "UpdateObject":
                                if (args.Length < 9) return;
                                try
                                {
                                    SendMessageToEveryone("GameObjects,UpdatedObject," +
                                        int.Parse(args[2]).ToString() + "," +
                                        float.Parse(args[3]).ToString() + "," +
                                        float.Parse(args[4]).ToString() + "," +
                                        float.Parse(args[5]).ToString() + "," +
                                        float.Parse(args[6]).ToString() + "," +
                                        float.Parse(args[7]).ToString() + "," +
                                        float.Parse(args[8]).ToString());
                                }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong UpdateObject packet.", plr.name);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                    clients.Remove(client);
                                }
                                break;
                            #endregion
                            #region RemoveObject
                            case "RemoveObject":
                                if (args.Length < 3) return;
                                try
                                {
                                    SendMessageToEveryone("GameObjects,RemovedObject," + int.Parse(args[2]).ToString());
                                }
                                catch
                                {
                                    Console.WriteLine("> Player {0} sended wrong RemoveObject packet.", plr.name);
                                    SendMessage(stream, "Disconnect,WrongPacket", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                    players.Remove(plr);
                                    clients.Remove(client);
                                }
                                break;
                                #endregion
                        }
                        break;
                    #endregion
                    #region Authification
                    case "Authification":
                        if (args.Length < 2) return;
                        switch (args[1])
                        {
                            #region ChangePassword
                            case "ChangePassword":
                                if (args.Length < 4) return;
                                if (ReadPassword(plr.name) == args[2])
                                {
                                    SetPassword(plr.name, args[3]);
                                    SendMessage(stream, "Chat,Receive,Server,Changed password successfully!", plr.name);
                                }
                                else if (ReadPassword(plr.name) == "BadFile")
                                {
                                    Console.WriteLine("> Players database is corrupted.", plr.name);
                                    SendMessage(stream, "Disconnect,CorruptedDatabase", plr.name);
                                    Task.Delay(1000).ContinueWith(t => client.Close());
                                }
                                else
                                {
                                    SendMessage(stream, "Chat,Receive,Server,Incorrect password entered!", plr.name);
                                }
                                break;
                            #endregion
                        }
                        break;
                    #endregion
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("> Error {0}, {1}.", e.Message, e.StackTrace);
            }
        }
        #endregion

        #region Process Login
        public static void ProcessLogin(NetworkStream stream, TcpClient client, Player plr, int index)
        {
            try
            {
                if (players[index].loggedIn) return;
                byte[] data = new byte[256];
                int bytes = 0;
                try { bytes = stream.Read(data, 0, data.Length); }
                catch { return; }
                string message = Encoding.UTF8.GetString(data, 0, bytes);
                string[] args = message.Split(',');
                string content = "";
                for (int i = 0; i < args.Length; i++)
                {
                    if (i == args.Length - 1) content += args[i];
                    else content += args[i] + ",";
                }

                if (content == "") return;

                Console.WriteLine("> Player {0} ({1}) sended: {2}", plr.name, plr.IPAdress, content);

                if (args[0] != "Authification") return;

                if (args.Length < 2) return;

                switch (args[1])
                {
                    #region Login
                    case "Login":
                        if (args.Length < 3) return;
                        if (ReadPassword(plr.name) == "PlayerNotRegistered")
                        {
                            SendMessage(stream, "Chat,Receive,Server,Register first!", plr.name);
                        }
                        else if (ReadPassword(plr.name) == args[2])
                        {
                            SendMessage(stream, "Chat,Receive,Server,Logged in!", plr.name);
                            players[index].loggedIn = true;
                            plr.loggedIn = true;
                        }
                        else if (ReadPassword(plr.name) == "BadFile")
                        {
                            Console.WriteLine("> Players database is corrupted.", plr.name);
                            SendMessage(stream, "Disconnect,CorruptedDatabase", plr.name);
                            Task.Delay(1000).ContinueWith(t => client.Close());
                        }
                        else
                        {
                            Console.WriteLine("> Player {0} sended wrong password to log in.", plr.name);
                            SendMessage(stream, "Disconnect,WrongPassword", plr.name);
                            Task.Delay(1000).ContinueWith(t => client.Close());
                        }
                        break;
                    #endregion
                    #region Register
                    case "Register":
                        if (args.Length < 3) return;
                        if (ReadPassword(plr.name) == "PlayerNotRegistered")
                        {
                            SetPassword(plr.name, args[2]);
                            SendMessage(stream, "Chat,Receive,Server,Registered and logged in!", plr.name);
                        }
                        else if (ReadPassword(plr.name) == "BadFile")
                        {
                            Console.WriteLine("> Players database is corrupted.", plr.name);
                            SendMessage(stream, "Disconnect,CorruptedDatabase", plr.name);
                            Task.Delay(1000).ContinueWith(t => client.Close());
                        }
                        else
                        {
                            SendMessage(stream, "Chat,Receive,Server,You're already registered!", plr.name);
                        }
                        break;
                        #endregion
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("> Error {0}, {1}.", e.Message, e.StackTrace);
            }
        }
        #endregion

        #region Process Player
        public static string ProcessPlayer(NetworkStream stream, TcpClient client)
        {
            string ClientPublicIP = client.Client.RemoteEndPoint.ToString();
            string ClientLocalIP = client.Client.LocalEndPoint.ToString();
            Console.WriteLine("> Player connected! ({0}/{1})", ClientLocalIP, ClientPublicIP);
            bool isOnline = false;
            byte[] data = new byte[256];
            int bytes = stream.Read(data, 0, data.Length);
            string message = Encoding.UTF8.GetString(data, 0, bytes);
            string[] args = message.Split(',');
            string name = "";
            client.Client.Blocking = true;

            if (args[0] == "Player" && args[1] == "Join")
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].name == args[2])
                    {
                        isOnline = true;
                    }
                }

                if (isOnline)
                {
                    SendMessage(stream, "Disconnect,ThisNameIsUsed", ClientPublicIP);
                    Console.WriteLine("> Player with IP {0} was kicked.", ClientPublicIP);
                    isOnline = false;
                    Task.Delay(1000).ContinueWith(t => client.Close());
                }
            }

            if (!isOnline)
            {
                name = args[2];
                Console.WriteLine("> {0}/{1} nickname is {2}.", ClientLocalIP, ClientPublicIP, name);
                if (name == "Server")
                {
                    SendMessage(stream, "Disconnect,RestrictedName", name);
                    Task.Delay(1000).ContinueWith(t => client.Close());
                    return null;
                }
                else
                {
                    string[] banned = ReadBanlist().ToArray();
                    bool isBanned = false;
                    for (int i = 0; i < banned.Length; i++)
                    {
                        if (banned[i] == name) isBanned = true;
                    }
                    if (isBanned)
                    {
                        SendMessage(stream, "Disconnect,Banned", name);
                        Task.Delay(1000).ContinueWith(t => client.Close());
                        return null;
                    }
                    else
                    {
                        SendMessage(stream, "Connect,Successful", name);
                        if (ReadConfig(0) == "true") SendMessage(stream, "Chat,Login", name);
                        return name;
                    }
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Messages
        public static void SendMessage(NetworkStream stream, string message, string name)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
            Console.WriteLine("> Sended message {0} to {1}.", message, name);
        }

        public static void SendMessageToEveryone(string message)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    NetworkStream ns = clients[i].GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    ns.Write(data, 0, data.Length);
                }
                catch
                {
                    Console.WriteLine("> Can't send message to one of the clients.");
                }
            }
            Console.WriteLine("> Sended message {0} to everyone.", message);
        }
        public static void SendMessageToEveryoneButNotToWhoItSended(string message, TcpClient client)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    if (clients[i] == client) continue;
                    NetworkStream ns = clients[i].GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    ns.Write(data, 0, data.Length);
                }
                catch
                {
                    Console.WriteLine("> Can't send message to one of the clients.");
                }
            }
            Console.WriteLine("> Sended message {0} to everyone.", message);
        }
        #endregion
    }
}

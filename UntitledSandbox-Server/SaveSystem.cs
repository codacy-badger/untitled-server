using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UntitledSandbox_Server
{
    [System.Serializable]
    public class PlayersDatabase
    {
        public PlayerData[] players { get; set; }
    }

    [System.Serializable]
    public class PlayerData
    {
        public string name { get; set; }
        public string password { get; set; }
    }

    public class SaveSystem
    {
        public static string path = Directory.GetCurrentDirectory() + "/data/";
        public static string database = path + "players.db";

        public static string ReadPassword(string user)
        {
            try
            {
                FileStream stream;
                if (!File.Exists(database)) File.Create(database);
                if (File.ReadAllText(database) == "") return "PlayerNotRegistered";
                stream = File.OpenRead(database);
                BinaryFormatter formatter = new BinaryFormatter();
                PlayersDatabase db = new PlayersDatabase();
                db = (PlayersDatabase)formatter.Deserialize(stream);

                stream.Close();

                for (int i = 0; i < db.players.Length; i++)
                {
                    if (db.players[i].name == user) return db.players[i].password;
                }

                return "PlayerNotRegistered";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return "BadFile";
            }
        }

        public static void SetPassword(string user, string password)
        {
            try
            {
                bool contains = false;
                int index = 0;
                FileStream stream;
                if (!File.Exists(database))
                {
                    stream = File.Create(database);
                    stream.Close();
                }
                BinaryFormatter formatter = new BinaryFormatter();
                PlayersDatabase db = new PlayersDatabase();
                if (File.ReadAllText(database) != "")
                {
                    stream = File.OpenRead(database);
                    db = (PlayersDatabase)formatter.Deserialize(stream);
                    for (int i = 0; i < db.players.Length; i++)
                    {
                        if (db.players[i].name == user)
                        {
                            contains = true;
                            index = i;
                        }
                    }
                    if (contains)
                    {
                        db.players[index].password = password;
                        stream.Close();
                        stream = File.OpenWrite(database);
                        formatter.Serialize(stream, db);
                        stream.Close();
                    }
                    else
                    {
                        List<PlayerData> players = new List<PlayerData>();
                        for (int i = 0; i < db.players.Length; i++)
                        {
                            players.Add(db.players[i]);
                        }
                        PlayerData player = new PlayerData();
                        player.name = user;
                        player.password = password;
                        players.Add(player);

                        db.players = players.ToArray();
                        stream.Close();
                        stream = File.OpenWrite(database);
                        formatter.Serialize(stream, db);
                        stream.Close();
                    }
                }
                else
                {
                    stream = File.OpenWrite(database);
                    List<PlayerData> players = new List<PlayerData>();
                    PlayerData plr = new PlayerData();
                    plr.name = user;
                    plr.password = password;
                    players.Add(plr);
                    PlayersDatabase newDB = new PlayersDatabase();
                    newDB.players = players.ToArray();
                    formatter.Serialize(stream, newDB);
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return;
            }
        }
    }
}

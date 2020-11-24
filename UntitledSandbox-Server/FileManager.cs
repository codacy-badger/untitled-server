using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UntitledSandbox_Server
{
    public class FileManager
    {
        public struct Data
        {
            public static string path = Directory.GetCurrentDirectory() + "/data/";
            public static string config = path + "config.txt";
            public static string players = path + "players.txt";
            public static string banlist = path + "banlist.txt";
        }

        public static void FixFolder()
        {
            if (!Directory.Exists(Data.path))
                Directory.CreateDirectory(Data.path);
        }

        public static string ReadConfig(int index)
        {
            FixFolder();
            string path = Data.config;
            if (!File.Exists(path)) File.Create(path);
            string content = File.ReadAllText(path);
            string[] data = content.Split(',');
            if (data.Length <= index) return "Unknown";
            return data[index];
        }

        public static void WriteConfig(int index, string data)
        {
            FixFolder();
            string path = Data.config;
            if (!File.Exists(path)) File.Create(path);
            string content = File.ReadAllText(path);
            string[] contents = content.Split(',');
            if (contents.Length <= index) return;
            contents[index] = data;
            content = "";
            for (int i = 0; i < contents.Length; i++)
            {
                if (i == contents.Length - 1) content += contents[i];
                else content += contents[i] + ",";
            };
            File.WriteAllText(path, content);
        }

        public static List<string> ReadBanlist()
        {
            FixFolder();
            string path = Data.banlist;
            if (!File.Exists(path)) File.Create(path);
            string content = File.ReadAllText(path);
            string[] data = content.Split(',');
            return data.ToList();
        }

        public static void AppendBanlist(string name)
        {
            FixFolder();
            string path = Data.banlist;
            if (!File.Exists(path)) File.Create(path);
            File.AppendAllText(path, "," + name);
        }

        public static void Defaults()
        {
            FixFolder();
            string path = Data.config;
            File.WriteAllText(path, "true,true,true,false");
        }
    }
}
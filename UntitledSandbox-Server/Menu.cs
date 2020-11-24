using System;
using System.Collections.Generic;
using System.Text;

namespace UntitledSandbox_Server
{
    public class Menu
    {
        public static string version = "Beta 1.0";
        public static string product = "Untitled Sandbox Server";
        public static void MenuMain()
        {
            Console.Clear();
            Console.WriteLine("Welcome to the " + product);
            Console.WriteLine("1 - Server");
            Console.WriteLine("2 - Client");
            Console.WriteLine("3 - Settings");
            Console.WriteLine("Enter number below:");

            string choise = Console.ReadLine();

            switch (choise)
            {
                case "1":
                    Server.ServerMain();
                    break;
                case "2":
                    Client.ClientMain();
                    break;
                case "3":
                    Settings.SettingsMain();
                    break; 
                default:
                    MenuMain();
                    break;
            }
        }
    }
}

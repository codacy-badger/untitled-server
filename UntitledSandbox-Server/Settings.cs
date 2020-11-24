using System;
using static UntitledSandbox_Server.FileManager;

namespace UntitledSandbox_Server
{
    public class Settings
    {
        public static void SettingsMain()
        {
            try
            {
                Console.Clear();
                Console.WriteLine("Settings Menu");
                Console.WriteLine("1 - Authification: {0}", ReadConfig(0));
                Console.WriteLine("2 - Use banlist: {0}", ReadConfig(1));
                Console.WriteLine("3 - Chat enabled: {0}", ReadConfig(2));
                Console.WriteLine("4 - Anti-cheat: {0}", ReadConfig(3));
                Console.WriteLine("5 - Back to menu");
                Console.WriteLine("Enter number below:");

                string choise = Console.ReadLine();

                switch (choise)
                {
                    case "1":
                        if (bool.Parse(ReadConfig(0)))
                            WriteConfig(0, "false");
                        else
                            WriteConfig(0, "true");
                        SettingsMain();
                        break;
                    case "2":
                        if (bool.Parse(ReadConfig(1)))
                            WriteConfig(1, "false");
                        else
                            WriteConfig(1, "true");
                        SettingsMain();
                        break;
                    case "3":
                        if (bool.Parse(ReadConfig(2)))
                            WriteConfig(2, "false");
                        else
                            WriteConfig(2, "true");
                        SettingsMain();
                        break;
                    case "4":
                        if (bool.Parse(ReadConfig(3)))
                            WriteConfig(3, "false");
                        else
                            WriteConfig(3, "true");
                        SettingsMain();
                        break;
                    case "5":
                        Menu.MenuMain();
                        break;
                    default:
                        SettingsMain();
                        break;
                }
            }
            catch
            {
                Console.WriteLine("> Error occured. Press enter to exit.");
                Console.Read();
                Environment.Exit(0);
            }
        }
    }
}

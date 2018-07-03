namespace PoE.Bot.Handlers
{
    using Console = Colorful.Console;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;

    public enum Source
    {
        Connected,
        Discord,
        Disconnected,
        Database,
        Event,
        Exception
    }

    public class LogHandler
    {
        private static readonly object Lock = new object();

        public static async Task CriticalFail(Source source, string text)
        {
            Write(source, text);
            await Task.Delay(5000);
            Environment.Exit(1);
        }

        public static void ForceGC()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

        public static void PrintApplicationInformation()
        {
            Console.WriteAscii("PoE Bot", Color.LightGray);
            Append("-> INFORMATION\n", Color.LightBlue);
            Append("    Author: Kyle Undefined | Discord: https://discord.me/poe_xbox\n", Color.LightBlue);
            Append($"\n=======================[ {DateTime.Now} ]=======================\n", Color.LightBlue);
            FileLog($"\n\n=================================[ {DateTime.Now} ]=================================\n\n");
        }

        public static void Write(Source source, string text)
        {
            Color sourceColor;
            Console.Write(Environment.NewLine);
            Append($"{(DateTime.Now.ToShortTimeString().Length <= 7 ? $"0{DateTime.Now.ToShortTimeString()}" : DateTime.Now.ToShortTimeString())} ", Color.DarkGray);
            switch (source)
            {
                case Source.Connected:
                    sourceColor = Color.SpringGreen;
                    break;

                case Source.Discord:
                    sourceColor = Color.CornflowerBlue;
                    break;

                case Source.Disconnected:
                    sourceColor = Color.Orange;
                    break;

                case Source.Database:
                    sourceColor = Color.Violet;
                    break;

                case Source.Exception:
                    sourceColor = Color.Crimson;
                    break;

                case Source.Event:
                    sourceColor = Color.LightSalmon;
                    break;
            }
            Append($"[{source}]", sourceColor);
            Append($" {text}", Color.WhiteSmoke);
            FileLog($"[{(DateTime.Now.ToShortTimeString().Length <= 7 ? $"0{DateTime.Now.ToShortTimeString()}" : DateTime.Now.ToShortTimeString())}] [{source}] {text}");
        }

        private static void Append(string text, Color color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }

        private static void FileLog(string message)
        {
            lock (Lock)
                using (StreamWriter writer = File.AppendText($"{Directory.GetCurrentDirectory()}/log.txt"))
                    writer.WriteLine(message);
        }
    }
}
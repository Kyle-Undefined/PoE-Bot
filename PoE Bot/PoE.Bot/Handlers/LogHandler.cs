namespace PoE.Bot.Handlers
{
    using System;
    using System.IO;
    using System.Drawing;
    using System.Threading.Tasks;
    using Console = Colorful.Console;

    public class LogHandler
    {
        readonly static object Lock = new object();

        static void FileLog(string Message)
        {
            lock (Lock)
                using (var Writer = File.AppendText($"{Directory.GetCurrentDirectory()}/log.txt"))
                    Writer.WriteLine(Message);
        }

        static void Append(string Text, Color Color)
        {
            Console.ForegroundColor = Color;
            Console.Write(Text);
        }

        public static async Task CriticalFail(Source Source, string Text)
        {
            Write(Source, Text);
            await Task.Delay(5000);
            Environment.Exit(1);
        }

        public static void Write(Source Source, string Text)
        {
            Color SourceColor;
            Console.Write(Environment.NewLine);
            Append($"{(DateTime.Now.ToShortTimeString().Length <= 7 ? $"0{DateTime.Now.ToShortTimeString()}" : DateTime.Now.ToShortTimeString())} ", Color.DarkGray);
            switch (Source)
            {
                case Source.CNN: SourceColor = Color.SpringGreen; break;
                case Source.DSD: SourceColor = Color.CornflowerBlue; break;
                case Source.DSN: SourceColor = Color.Orange; break;
                case Source.DTB: SourceColor = Color.Violet; break;
                case Source.EXC: SourceColor = Color.Crimson; break;
                case Source.EVT: SourceColor = Color.LightSalmon; break;
            }
            Append($"[{Source}]", SourceColor);
            Append($" {Text}", Color.WhiteSmoke);
            FileLog($"[{(DateTime.Now.ToShortTimeString().Length <= 7 ? $"0{DateTime.Now.ToShortTimeString()}" : DateTime.Now.ToShortTimeString())}] [{Source}] {Text}");
        }

        public static void PrintApplicationInformation()
        {
            Console.WriteAscii("PoE Bot", Color.LightGray);
            Append("-> INFORMATION\n", Color.LightBlue);
            Append("    Author: Kyle Undefined | Discord: https://discord.me/poe_xbox\n", Color.LightBlue);
            Append($"\n=======================[ {DateTime.Now} ]=======================\n", Color.LightBlue);
            FileLog($"\n\n=================================[ {DateTime.Now} ]=================================\n\n");
        }

        public static void ForceGC()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            FileLog($"[{(DateTime.Now.ToShortTimeString().Length <= 7 ? $"0{DateTime.Now.ToShortTimeString()}" : DateTime.Now.ToShortTimeString())}] GC Forced. {GC.MaxGeneration} Max Generations.");
        }
    }
    public enum Source
    {
        /// <summary>Connected</summary>
        CNN,
        /// <summary>Disconnected</summary>
        DSN,
        /// <summary>Discord</summary>
        DSD,
        /// <summary>Database</summary>
        DTB,
        /// <summary>Exception</summary>
        EXC,
        /// <summary>Event</summary>
        EVT
    }
}

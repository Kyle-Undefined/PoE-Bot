using System;
using System.Diagnostics;
using System.Text;
using PoE.Bot.Commands;
using PoE.Bot.Config;
using PoE.Bot.Core;
using PoE.Bot.Plugins;

namespace PoE.Bot
{
    public static class PoE_Bot
    {
        public static CommandManager CommandManager { get; internal set; }
        public static Client Client { get; internal set; }
        public static int PluginCount { get { return PluginManager.PluginCount; } }
        public static ConfigManager ConfigManager { get; private set; }
        internal static PluginManager PluginManager { get; set; }
        internal static UTF8Encoding UTF8 { get; set; }
        private static bool KeepRunning { get; set; }

        internal static void Main(string[] args)
        {
            // initialize self
            Log.R(Console.Out);
            Log.D(Debugger.IsAttached);
            UTF8 = new UTF8Encoding(false);

            // init discord
            Log.W("PoE_Bot", "Initializing PoE_Bot Discord module");
            Client = new Client();
            Client.Initialize();
            Log.W("PoE_Bot", "PoE_Bot Discord module initialized");

            // load plugins
            Log.W("PoE_Bot", "Loading PoE_Bot Plugins");
            PluginManager = new PluginManager();
            PluginManager.LoadAssemblies();
            Log.W("PoE_Bot", "PoE_Bot Plugins loaded");

            // init config
            Log.W("PoE_Bot", "Initializing PoE_Bot Config module");
            ConfigManager = new ConfigManager();
            ConfigManager.Initialize();
            Log.W("PoE_Bot", "PoE_Bot Config module initialized");

            // init plugins
            Log.W("PoE_Bot", "Initializing PoE_Bot Plugins");
            PluginManager.Initialize();
            Log.W("PoE_Bot", "PoE_Bot Plugins Initialized");

            // init commands
            Log.W("PoE_Bot", "Initializing PoE_Bot Command module");
            CommandManager = new CommandManager();
            CommandManager.Initialize();
            Log.W("PoE_Bot", "PoE_Bot Command module initialized");

            // run
            Log.W("PoE_Bot", "PoE_Bot is now running");
            KeepRunning = true;
            while (KeepRunning) { }

            // some shutdown signal and subsequent shutdown
            Log.W("PoE_Bot", "Caught exit signal");
            Client.Deinitialize();
            Log.W("PoE_Bot", "Disposing logger");
            Log.Q();
        }
    }
}

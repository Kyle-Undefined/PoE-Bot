using System;
using System.Reflection;

namespace PoE.Bot.Plugins
{
    internal class Plugin
    {
        public IPlugin _Plugin { get; set; }
        public string Name { get { return this._Plugin.Name; } }
        public Assembly DeclaringAssembly { get { return this._Plugin.GetType().GetTypeInfo().Assembly; } }
        public Type EntryType { get { return this._Plugin.GetType(); } }
    }
}

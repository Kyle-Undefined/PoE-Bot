using System.Reflection;
using System.Runtime.Loader;

namespace PoE.Bot.AssemblyResolver
{
    public delegate Assembly AssemblyResolveEventHandler(string name);

    public class Resolver
    {
        public Resolver()
        {
            AssemblyLoadContext.Default.Resolving += Default_Resolving;
        }

        public Assembly LoadFromFile(string filename)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(filename);
        }

        private Assembly Default_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            return this.FireResolving(arg2.Name);
        }

        private Assembly FireResolving(string name)
        {
            if (this.Resolving != null)
                return this.Resolving(name);
            return null;
        }

        public event AssemblyResolveEventHandler Resolving;
    }
}

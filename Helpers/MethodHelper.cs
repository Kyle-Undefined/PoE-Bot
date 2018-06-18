namespace PoE.Bot.Helpers
{
    using System;
    using System.Linq;
    using PoE.Bot.Addons;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client.Extensions;
    using System.Collections.Generic;

    public class MethodHelper
    {
        public static CancellationToken Cancellation(TimeSpan Time)
            => new CancellationTokenSource(Time).Token;

        public static T RunSync<T>(Task<T> AsyncTask)
            => Task.Run(async ()
                => await AsyncTask.WithCancellation(Cancellation(TimeSpan.FromSeconds(10)))).GetAwaiter().GetResult();

        public static void RunSync(Task AsyncTask) => Task.Run(async () =>
        {
            try
            {
                await AsyncTask.WithCancellation(Cancellation(TimeSpan.FromSeconds(10)));
            }
            catch { }
        });

        public static IEnumerable<Assembly> Assemblies
        {
            get
            {
                var Entries = Assembly.GetEntryAssembly().GetReferencedAssemblies();
                foreach (var Ass in Entries)
                    yield return Assembly.Load(Ass);
                yield return Assembly.GetEntryAssembly();
                yield return typeof(ILookup<string, string>).GetTypeInfo().Assembly;
            }
        }

        public static (bool, string) CollectionCheck<T>(IList<T> Collection, object Value, string ObjectName, string CollectionName)
        {
            var check = Collection.Contains((T)Value);
            if (Collection.Contains((T)Value))
                return (false, $"{Extras.Cross} I don't know how to use this yet. `{ObjectName}` already exists in {CollectionName}.");
            else if (Collection.Count == (Collection as List<T>).Capacity)
                return (false, $"{Extras.Cross} I don't know how to use this yet. Reached max number of entries.");
            else if (typeof(T) == typeof(string) && $"{Value}".Length >= 300)
                return (false, $"{Extras.Cross} I don't know how to use this yet. Message way too large.");
            return (true, $"`{ObjectName}` has been added to {CollectionName}");
        }
    }
}

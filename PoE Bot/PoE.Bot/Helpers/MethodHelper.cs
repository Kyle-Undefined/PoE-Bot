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
        public static CancellationToken Cancellation(TimeSpan time) => new CancellationTokenSource(time).Token;

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

        public static DateTime UnixDateTime(double Unix) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Unix).ToLocalTime();

        public static IEnumerable<Assembly> Assemblies
        {
            get
            {
                var Entries = Assembly.GetEntryAssembly().GetReferencedAssemblies();
                foreach (var Ass in Entries) yield return Assembly.Load(Ass);
                yield return Assembly.GetEntryAssembly();
                yield return typeof(ILookup<string, string>).GetTypeInfo().Assembly;
            }
        }

        public static (bool, string) CollectionCheck<T>(IList<T> Collection, object Value, string ObjectName, string CollectionName)
        {
            var check = Collection.Contains((T)Value);
            if (Collection.Contains((T)Value)) return (false, $"`{ObjectName}` already exists in {CollectionName}.");
            else if (Collection.Count == (Collection as List<T>).Capacity) return (false, $"Reached max number of entries {Extras.Cross}");
            else if (typeof(T) == typeof(string) && $"{Value}".Length >= 300) return (false, $"Message way too large {Extras.Cross}");
            return (true, $"`{ObjectName}` has been added to {CollectionName}");
        }
    }
}

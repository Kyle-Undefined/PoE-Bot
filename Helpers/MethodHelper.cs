namespace PoE.Bot.Helpers
{
    using Addons;
    using Discord;
    using Raven.Client.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public static class MethodHelper
    {
        public static IEnumerable<Assembly> Assemblies
        {
            get
            {
                AssemblyName[] assemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
                foreach (AssemblyName assembly in assemblies)
                    yield return Assembly.Load(assembly);
                yield return Assembly.GetEntryAssembly();
                yield return typeof(ILookup<string, string>).GetTypeInfo().Assembly;
            }
        }

        public static (bool, string) CalculateResponse(IMessage message)
            => message is null || string.IsNullOrWhiteSpace(message.Content)
            ? (false, $"{Extras.Cross} There is a fine line between consideration and hesitation. The former is wisdom, the latter is fear. *Request Timed Out*")
            : message.Content.ToLower().Equals("c")
                ? (false, $"Understood, Exile {Extras.OkHand}")
                : (true, message.Content);

        public static CancellationToken Cancellation(TimeSpan time)
            => new CancellationTokenSource(time).Token;

        public static (bool, string) CollectionCheck<T>(IList<T> collection, object value, string objectName, string collectionName)
        {
            if (collection.Contains((T)value))
                return (false, $"{Extras.Cross} I don't know how to use this yet. `{objectName}` already exists in {collectionName}.");
            else if (collection.Count == (collection as List<T>).Capacity)
                return (false, $"{Extras.Cross} I don't know how to use this yet. Reached max number of entries.");
            return typeof(T) == typeof(string) && $"{value}".Length >= 300
                ? (false, $"{Extras.Cross} I don't know how to use this yet. Message way too large.")
                : (true, $"`{objectName}` has been added to {collectionName}");
        }

        public static IEnumerable<string> Pages<T>(IEnumerable<T> collection)
        {
            var collectionList = collection.ToList();
            var buildPages = new List<string>(collectionList.Count());
            for (int i = 0; i <= collectionList.Count(); i += 10)
                buildPages.Add(string.Join("\n", collectionList.Skip(i).Take(10)));
            return buildPages;
        }

        public static T RunSync<T>(Task<T> asyncTask)
            => Task.Run(async () => await asyncTask.WithCancellation(Cancellation(TimeSpan.FromSeconds(10))).ConfigureAwait(false)).GetAwaiter().GetResult();

        public static void RunSync(Task asyncTask)
            => Task.Run(async () => { try { await asyncTask.WithCancellation(Cancellation(TimeSpan.FromSeconds(10))).ConfigureAwait(false); } catch { } });

        public static IList<T> Sort<T>(this IList<T> list)
        {
            if (list is List<T>)
                ((List<T>)list).Sort();
            else
            {
                List<T> copy = new List<T>(list);
                copy.Sort();
                Copy(copy, 0, list, 0, list.Count);
            }
            return list;
        }

        private static void Copy<T>(IList<T> sourceList, int sourceIndex, IList<T> destinationList, int destinationIndex, int count)
        {
            for (int i = 0; i < count; i++)
                destinationList[destinationIndex + i] = sourceList[sourceIndex + i];
        }
    }
}
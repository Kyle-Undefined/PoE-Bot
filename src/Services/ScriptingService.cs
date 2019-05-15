namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Scripting;
	using Microsoft.CodeAnalysis.Scripting;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;

	[Service]
	public class ScriptingService
	{
		public readonly IReadOnlyList<string> Imports = new ReadOnlyCollection<string>(new List<string>
		{
			"Discord",
			"Discord.WebSocket",
			"Microsoft.Extensions.DependencyInjection",
			"Microsoft.Extensions.Options",
			"PoE.Bot",
			"PoE.Bot.Contexts",
			"PoE.Bot.Extensions",
			"PoE.Bot.Helpers",
			"PoE.Bot.Models",
			"PoE.Bot.Modules",
			"PoE.Bot.Services",
			"Qmmands",
			"System",
			"System.Collections.Generic",
			"System.Diagnostics",
			"System.Globalization",
			"System.Linq",
			"System.Math",
			"System.Reflection",
			"System.Text"
		});

		public async Task<ScriptingResult> EvaluateScriptAsync<T>(string code, T properties)
		{
			if (string.IsNullOrWhiteSpace(code))
				return ScriptingResult.FromError(new ArgumentException("code parameter cannot be empty, null or whitespace", nameof(code)), ScriptStage.Preprocessing);

			var options = ScriptOptions.Default
				.WithReferences(typeof(IDiscordClient).Assembly)
				.WithReferences(typeof(DiscordSocketClient).Assembly)
				.WithReferences(GetAssemblies())
				.WithImports(Imports)
				.WithImports(GetNamespaces());

			var script = CSharpScript.Create(code, options, typeof(T));

			var compilationTimer = Stopwatch.StartNew();
			var compilationDiagnostics = script.Compile();

			if (compilationDiagnostics.Length > 0 && compilationDiagnostics.Any(a => a.Severity == DiagnosticSeverity.Error))
				return ScriptingResult.FromError(compilationDiagnostics, ScriptStage.Compilation, compilationTime: compilationTimer.ElapsedMilliseconds);

			compilationTimer.Stop();

			var executionTimer = new Stopwatch();

			try
			{
				executionTimer.Start();
				var executionResult = await script.RunAsync(properties);
				executionTimer.Stop();
				var returnValue = executionResult.ReturnValue;

				return ScriptingResult.FromSuccess(returnValue, compilationTimer.ElapsedMilliseconds, executionTimer.ElapsedMilliseconds);
			}
			catch (Exception exception)
			{
				return ScriptingResult.FromError(exception, ScriptStage.Execution, compilationTimer.ElapsedMilliseconds, executionTimer.ElapsedMilliseconds);
			}
		}

		private IEnumerable<Assembly> GetAssemblies()
		{
			var entries = Assembly.GetEntryAssembly();
			foreach (var assembly in entries.GetReferencedAssemblies())
				yield return Assembly.Load(assembly);
			yield return entries;
		}

		private IEnumerable<string> GetNamespaces() => Assembly.GetExecutingAssembly().GetTypes().Where(x => !string.IsNullOrEmpty(x.Namespace)).Select(x => x.Namespace).Distinct();
	}

	public class EvaluationHelper
	{
		public EvaluationHelper(GuildContext context)
		{
			Context = context;
		}

		public GuildContext Context { get; }
	}
}

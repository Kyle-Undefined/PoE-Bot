namespace PoE.Bot.Modules
{
	using Discord;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Addons.Interactive;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.ModuleBases;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Help Module")]
	[Description("Help Commands")]
	public class HelpModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }
		public CommandService Commands { get; set; }
		public IServiceProvider Services { get; set; }

		[Command("Help", "Commands")]
		[Name("Help")]
		[Description("Gets all commands and displays them")]
		[Usage("help")]
		[RunMode(RunMode.Parallel)]
		public async Task HelpAsync()
		{
			var config = await Database.BotConfigs.AsNoTracking().FirstAsync();
			var modules = Commands.GetAllModules().OrderBy(x => x.Name);
			var pages = new List<string>();

			foreach (var module in modules)
			{
				var passed = true;
				foreach (var precondition in module.Checks)
				{
					if (!(await precondition.CheckAsync(Context, Services)).IsSuccessful)
					{
						passed = false;
						break;
					}
				}

				if (passed)
				{
					var commands = new List<string>();
					foreach (var command in module.Commands)
					{
						if (command.Name != "Tag")
							commands.Add("**" + command.Name + "**\n" + command.Description);
					}

					if (!module.Name.Contains("Base"))
						pages.Add("\u200b\n**" + module.Description + "**\n\u200b\n" + string.Join("\n\n", commands));
				}
			}
			await PagedReplyAsync(new PaginatedMessage
			{
				Pages = pages,
				Color = new Color(0, 255, 255),
				Title = "For more information on a command do " + config.Prefix + "help command",
				Author = new EmbedAuthorBuilder
				{
					Name = "Command List",
					IconUrl = Context.Client.CurrentUser.GetAvatar()
				}
			});
		}

		[Command("Help", "Command")]
		[Name("Help")]
		[Description("Gets the command and shows all the information about it")]
		[Usage("help enhance")]
		public async Task HelpAsync(
			[Name("Command Name")]
			[Description("Command to get more info on")]
			[Remainder] string commandName)
		{
			var search = Commands.GetAllCommands().Where(x => string.Equals(x.Name, commandName, StringComparison.CurrentCultureIgnoreCase));

			if (search.Count() is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " No command found for `" + commandName + "`");
				return;
			}

			var command = search.First();
			var commandParams = string.Empty;
			foreach (var param in command.Parameters)
				commandParams += "*" + param.Name + "* - " + param.Description + "\n";

			var commandInfo = new EmbedFieldBuilder
			{
				Name = command.Name,
				Value = "**Aliases**: " + string.Join(", ", command.Aliases) + "\n"
					+ "**Description**: " + command.Description + "\n"
					+ "**Usage**: " + (await Database.BotConfigs.AsNoTracking().FirstAsync()).Prefix + (command.Attributes.FirstOrDefault(x => x is UsageAttribute) as UsageAttribute)?.ExampleUsage
					+ "\n" + (commandParams.Length > 0 ? "**Parameter(s)**: \n" + commandParams : "")
			};

			await ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.User.GetDisplayName(), Context.User.GetAvatar())
				.AddField(commandInfo)
				.WithCurrentTimestamp()
				.Build());
		}

		[Command("ModuleHelp", "MHelp")]
		[Name("ModuleHelp")]
		[Description("Gets all commands from a module")]
		[Usage("modulehelp fun")]
		public async Task ModuleHelpAsync(
			[Name("Module Name")]
			[Description("Module to get commands in")]
			[Remainder] string moduleName)
		{
			var module = Commands.GetAllModules().FirstOrDefault(x => string.Equals(x.Name, moduleName + " Module", StringComparison.CurrentCultureIgnoreCase));
			if (!(module is null))
			{
				var config = await Database.BotConfigs.AsNoTracking().FirstAsync();
				var embed = EmbedHelper.Embed(EmbedHelper.Info)
					.WithAuthor("Command List", Context.Client.CurrentUser.GetAvatar())
					.WithTitle("For more information on a command do " + config.Prefix + "help command")
					.WithFooter("Current Prefix: " + config.Prefix)
					.WithCurrentTimestamp();

				var passed = true;
				foreach (var precondition in module.Checks)
				{
					if (!(await precondition.CheckAsync(Context, Services)).IsSuccessful)
					{
						passed = false;
						break;
					}
				}

				if (passed)
				{
					var commands = new List<string>();
					foreach (var command in module.Commands)
					{
						if (command.Name != "Tag")
							commands.Add("**" + command.Name + "**\n" + command.Description);
					}

					if (!module.Name.Contains("Base"))
						embed.WithDescription("\u200b\n**" + module.Description + "**\n\u200b\n" + string.Join("\n\n", commands));
				}

				await ReplyAsync(embed: embed.Build());
			}
			else
			{
				await ReplyAsync(EmoteHelper.Cross + " No Module found for `" + moduleName + "`");
				return;
			}
		}
	}
}
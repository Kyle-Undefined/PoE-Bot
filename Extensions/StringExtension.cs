namespace PoE.Bot.Extensions
{
	using PoE.Bot.Models;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	public static class StringExtension
	{
		public static bool DoesStringHaveProfanity(this string data, ICollection<Profanity> badWords)
		{
			foreach (var word in badWords)
			{
				var regex = new Regex(word.Word.ExpandRegexForMispellings(), RegexOptions.IgnoreCase | RegexOptions.Multiline);
				var match = regex.Match(data);
				if (match.Success)
					return match.Success;
			}
			return false;
		}

		public static string ExpandRegexForMispellings(this string word)
		{
			char[] chars = word.ToCharArray();
			var op = @"(^|\s)[" + string.Join("][", chars) + @"](\s|$)";

			return op
				.Replace("[a]", "[aA@]+")
				.Replace("[b]", "(?:(I3)|(l3)|(i3)|(13)|[bB])+")
				.Replace("[c]", "(?:[cC\\(]|[kK])+")
				.Replace("[d]", "[dD]+")
				.Replace("[e]", "[eE3]+")
				.Replace("[f]", "(?:[fF]|[pPhH])+")
				.Replace("[g]", "[gG6]+")
				.Replace("[h]", "[hH]+")
				.Replace("[i]", "[iIl!1]+")
				.Replace("[j]", "[jJ]+")
				.Replace("[k]", "(?:[cC\\(]|[kK])+")
				.Replace("[l]", "[lL1!i]+")
				.Replace("[m]", "[mM]+")
				.Replace("[n]", "[nN]+")
				.Replace("[o]", "[oO0]+")
				.Replace("[p]", "[pP]+")
				.Replace("[q]", "[qQ9]+")
				.Replace("[r]", "[rR]+")
				.Replace("[s]", "[sS$5]+")
				.Replace("[t]", "[tT7]+")
				.Replace("[u]", "[uUvV]+")
				.Replace("[v]", "[vVuU]+")
				.Replace("[w]", "[wWvvVV]+")
				.Replace("[x]", "[xX]+")
				.Replace("[y]", "[yY]+")
				.Replace("[z]", "[zZ2]+");
		}

		public static string FormatTimeSpan(this TimeSpan timeSpan)
		{
			string FormatTS(int ts, string name) => ts > 0 ? $"{ts} {name}{(ts > 1 ? "s" : "")}" : null;
			return string.Join(", ", new[]
			{
				FormatTS(timeSpan.Days, "day"),
				FormatTS(timeSpan.Hours, "hour"),
				FormatTS(timeSpan.Minutes, "minute"),
				FormatTS(timeSpan.Seconds, "second")
			}.Where(x => !(x is null)));
		}

		public static bool ProfanityMatch(this string message, ICollection<Profanity> profanityList) => message.DoesStringHaveProfanity(profanityList);

		public static IEnumerable<string> SplitInParts(this string text, int partLength)
		{
			for (var i = 0; i < text.Length; i += partLength)
				yield return text.Substring(i, Math.Min(partLength, text.Length - i));
		}
	}
}
namespace PoE.Bot.Extensions
{
	using System;
	using System.Collections.Generic;

	public static class IntExtension
	{
		public static int[] GetUnicodeCodePoints(this string emojiString)
		{
			var codePoints = new List<int>(emojiString.Length);
			for (int i = 0; i < emojiString.Length; i++)
			{
				int codePoint = char.ConvertToUtf32(emojiString, i);
				if (codePoint != 0xfe0f)
					codePoints.Add(codePoint);
				if (char.IsHighSurrogate(emojiString[i]))
					i++;
			}

			return codePoints.ToArray();
		}

		public static int GetValidColorNumber(this int? number, int random) => number ?? random;

		public static int LevenshteinDistance(this string a, string b)
		{
			if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
				return 0;

			int lengthA = a.Length;
			int lengthB = b.Length;
			int[,] distances = new int[lengthA + 1, lengthB + 1];
			for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
			for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

			for (int i = 1; i <= lengthA; i++)
			{
				for (int j = 1; j <= lengthB; j++)
				{
					int cost = b[j - 1] == a[i - 1] ? 0 : 1;
					distances[i, j] = Math.Min(Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1), distances[i - 1, j - 1] + cost);
				}
			}

			return distances[lengthA, lengthB];
		}
	}
}
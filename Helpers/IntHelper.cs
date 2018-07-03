namespace PoE.Bot.Helpers
{
    using System.Collections.Generic;

    public class IntHelper
    {
        public static int[] GetUnicodeCodePoints(string emojiString)
        {
            List<int> codePoints = new List<int>(emojiString.Length);
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
    }
}
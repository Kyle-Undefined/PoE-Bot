using System;

namespace PoE.Bot.Extensions
{
    public static class Extensions
    {
        public static string ToSizeString(this long l)
        {
            var d = (double)l;
            var i = 0;
            var u = new string[] { "", "k", "M", "G", "T" };
            while (d >= 900)
            {
                d /= 1024D;
                i++;
            }
            return string.Format("{0:#,##0.00} {1}B", d, u[i]);
        }

        public static string ToSizeString(this int l)
        {
            var d = (double)l;
            var i = 0;
            var u = new string[] { "", "k", "M", "G", "T" };
            while (d >= 900)
            {
                d /= 1024D;
                i++;
            }
            return string.Format("{0:#,##0.00} {1}B", d, u[i]);
        }

        public static string ToPointerString(this IntPtr ptr)
        {
            var i32 = ptr.ToInt32();
            var i64 = ptr.ToInt64();
            var pst = null as string;
            //if (Environment.Is64BitOperatingSystem)
            if (IntPtr.Size == 8)
                pst = string.Concat("0x", i64.ToString("X16"));
            else
                pst = string.Concat("0x", i32.ToString("X8"));
            return pst;
        }
    }
}

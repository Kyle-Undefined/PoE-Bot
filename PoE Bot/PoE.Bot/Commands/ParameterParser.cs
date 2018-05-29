using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord;

namespace PoE.Bot.Commands
{
    public delegate bool TryParseDelegate<T>(string value, out T result);
    public delegate bool ContextParseDelegate<T>(CommandContext ctx, string value, out T result);

    public class ParameterParser
    {
        private Dictionary<Type, Delegate> Parsers { get; set; }

        public ParameterParser()
        {
            this.Parsers = new Dictionary<Type, Delegate>();
            this.Parsers[typeof(bool)] = (TryParseDelegate<bool>)bool.TryParse;
            this.Parsers[typeof(sbyte)] = (TryParseDelegate<sbyte>)sbyte.TryParse;
            this.Parsers[typeof(byte)] = (TryParseDelegate<byte>)byte.TryParse;
            this.Parsers[typeof(short)] = (TryParseDelegate<short>)short.TryParse;
            this.Parsers[typeof(ushort)] = (TryParseDelegate<ushort>)ushort.TryParse;
            this.Parsers[typeof(int)] = (TryParseDelegate<int>)int.TryParse;
            this.Parsers[typeof(uint)] = (TryParseDelegate<uint>)uint.TryParse;
            this.Parsers[typeof(long)] = (TryParseDelegate<long>)long.TryParse;
            this.Parsers[typeof(ulong)] = (TryParseDelegate<ulong>)ulong.TryParse;
            this.Parsers[typeof(float)] = (TryParseDelegate<float>)float.TryParse;
            this.Parsers[typeof(double)] = (TryParseDelegate<double>)double.TryParse;
            this.Parsers[typeof(decimal)] = (TryParseDelegate<decimal>)decimal.TryParse;
            this.Parsers[typeof(DateTime)] = (TryParseDelegate<DateTime>)DateTime.TryParse;
            this.Parsers[typeof(DateTimeOffset)] = (TryParseDelegate<DateTimeOffset>)DateTimeOffset.TryParse;
            this.Parsers[typeof(TimeSpan)] = (TryParseDelegate<TimeSpan>)TryParseTimeSpan;
            this.Parsers[typeof(char)] = (TryParseDelegate<char>)char.TryParse;
            this.Parsers[typeof(string)] = (TryParseDelegate<string>)TryParseString;
            this.Parsers[typeof(IUser)] = (ContextParseDelegate<IUser>)TryParseUser;
            this.Parsers[typeof(IRole)] = (ContextParseDelegate<IRole>)TryParseRole;
            this.Parsers[typeof(ITextChannel)] = (ContextParseDelegate<ITextChannel>)TryParseChannel;
        }

        public object Parse(CommandContext ctx, string value, Type type)
        {
            var dlg = this.GetDelegate(type);
            var dft = type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
            if (dlg == null)
                throw new ArgumentException("Invalid value specified.");

            var mtd = dlg.GetMethodInfo();
            var arg = (object[])null;
            if (dlg.GetType().GetGenericTypeDefinition() == typeof(TryParseDelegate<>))
                arg = new object[] { value, dft };
            else if (dlg.GetType().GetGenericTypeDefinition() == typeof(ContextParseDelegate<>))
                arg = new object[] { ctx, value, dft };
            if (!(bool)mtd.Invoke(null, arg))
                throw new ArgumentException("Invalid value specified.");

            return arg[arg.Length - 1];
        }

        public T Parse<T>(CommandContext ctx, string value)
        {
            var dlg = this.GetDelegate<T>();
            if (dlg == null)
                throw new ArgumentException("Invalid value specified.");

            var rtv = default(T);
            if (!dlg(value, out rtv))
                throw new ArgumentException("Invalid value specified.");

            return rtv;
        }

        private TryParseDelegate<T> GetDelegate<T>()
        {
            if (this.Parsers.ContainsKey(typeof(T)))
                return (TryParseDelegate<T>)this.Parsers[typeof(T)];
            return null;
        }

        private Delegate GetDelegate(Type t)
        {
            if (this.Parsers.ContainsKey(t))
                return this.Parsers[t];
            return null;
        }

        private static bool TryParseString(string value, out string result)
        {
            result = value;
            return true;
        }

        private static bool TryParseTimeSpan(string value, out TimeSpan result)
        {
            if (value == "0")
            {
                result = TimeSpan.Zero;
                return true;
            }

            if (TimeSpan.TryParse(value, out result))
                return true;

            var reg = new Regex(@"^(?<days>\d+d)?(?<hours>\d{1,2}h)?(?<minutes>\d{1,2}m)?(?<seconds>\d{1,2}s)?$", RegexOptions.Compiled);
            var gps = new string[] { "days", "hours", "minutes", "seconds" };
            var mtc = reg.Match(value);
            if (!mtc.Success)
            {
                result = TimeSpan.Zero;
                return false;
            }

            var d = 0;
            var h = 0;
            var m = 0;
            var s = 0;
            foreach (var gp in gps)
            {
                var val = 0;
                var gpc = mtc.Groups[gp].Value;
                if (string.IsNullOrWhiteSpace(gpc))
                    continue;

                var gpt = gpc.Last();
                int.TryParse(gpc.Substring(0, gpc.Length - 1), out val);
                switch (gpt)
                {
                    case 'd':
                        d = val;
                        break;

                    case 'h':
                        h = val;
                        break;

                    case 'm':
                        m = val;
                        break;

                    case 's':
                        s = val;
                        break;
                }
            }
            result = new TimeSpan(d, h, m, s);
            return true;
        }

        private static bool TryParseUser(CommandContext ctx, string value, out IUser result)
        {
            result = null;
            if (!value.StartsWith("<@") || !value.EndsWith(">"))
                return false;

            var shift = value.StartsWith("<@!") ? 1 : 0;
            var usrid = value.Substring(2 + shift, value.Length - (3 + shift));
            if (!usrid.All(xc => Char.IsNumber(xc)))
                return false;

            var id = ulong.Parse(usrid);
            result = ctx.Guild.GetUserAsync(id).GetAwaiter().GetResult();
            return true;
        }

        private static bool TryParseRole(CommandContext ctx, string value, out IRole result)
        {
            result = null;
            if (value.StartsWith("<@&") && value.EndsWith(">"))
            {
                var rlid = value.Substring(3, value.Length - 4);
                if (!rlid.All(xc => Char.IsNumber(xc)))
                    return false;

                var id = ulong.Parse(rlid);
                result = ctx.Guild.GetRole(id);
                return true;
            }
            else
            {
                var rl = ctx.Guild.Roles.FirstOrDefault(xr => xr.Name == value);
                result = rl;
                if (rl == null)
                    return false;
                return true;
            }
        }

        private static bool TryParseChannel(CommandContext ctx, string value, out ITextChannel result)
        {
            result = null;
            if (!value.StartsWith("<#") || !value.EndsWith(">"))
                return false;

            var chnid = value.Substring(2, value.Length - 3);
            if (!chnid.All(xc => Char.IsNumber(xc)))
                return false;

            var id = ulong.Parse(chnid);
            var chn = ctx.Guild.GetChannelAsync(id).GetAwaiter().GetResult() as ITextChannel;
            result = chn;
            if (chn == null)
                return false;
            return true;
        }
    }
}

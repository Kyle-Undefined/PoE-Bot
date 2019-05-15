namespace PoE.Bot.Parsers
{
	using System;

	internal class GenericParseResult<T>
	{
		public float Score { get; set; }
		public T Value { get; set; }
	}
}

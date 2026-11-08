using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCL.Core.Utils;

public class ArgumentsBuilder
{
	private enum ArgumentStyle
	{
		Flag,
		Equals,
		Space
	}

	private readonly struct Argument(string key, string? value, ArgumentStyle style)
	{
		public readonly string Key = key ?? throw new ArgumentNullException("key");

		public readonly string? Value = value;

		public readonly ArgumentStyle Style = style;
	}

	public enum PrefixStyle
	{
		Auto,
		SingleLine,
		DoubleLine
	}

	private readonly List<Argument> _args = new List<Argument>();

	private static readonly char[] _CharNeedToQute = new char[4] { ' ', '=', '|', '"' };

	public ArgumentsBuilder Add(string key, string value)
	{
		if (key == null)
		{
			throw new NullReferenceException("key");
		}
		if (value == null)
		{
			throw new NullReferenceException("value");
		}
		_args.Add(new Argument(key, _HandleValue(value), ArgumentStyle.Equals));
		return this;
	}

	public ArgumentsBuilder AddWithSpace(string key, string value)
	{
		if (key == null)
		{
			throw new NullReferenceException("key");
		}
		if (value == null)
		{
			throw new NullReferenceException("value");
		}
		_args.Add(new Argument(key, _HandleValue(value), ArgumentStyle.Space));
		return this;
	}

	public ArgumentsBuilder AddFlag(string flag)
	{
		if (flag == null)
		{
			throw new NullReferenceException("flag");
		}
		_args.Add(new Argument(flag, null, ArgumentStyle.Flag));
		return this;
	}

	public ArgumentsBuilder AddIf(bool condition, string key, string value)
	{
		if (condition)
		{
			Add(key, value);
		}
		return this;
	}

	public ArgumentsBuilder AddWithSpaceIf(bool condition, string key, string value)
	{
		if (condition)
		{
			AddWithSpace(key, value);
		}
		return this;
	}

	public ArgumentsBuilder AddFlagIf(bool condition, string flag)
	{
		if (condition)
		{
			AddFlag(flag);
		}
		return this;
	}

	public string GetResult(PrefixStyle prefixStyle = PrefixStyle.Auto)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Argument arg in _args)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			switch (prefixStyle)
			{
			case PrefixStyle.SingleLine:
				stringBuilder.Append('-').Append(arg.Key);
				break;
			case PrefixStyle.DoubleLine:
				stringBuilder.Append("--").Append(arg.Key);
				break;
			default:
				stringBuilder.Append((arg.Key.Length == 1) ? "-" : "--").Append(arg.Key);
				break;
			}
			if (arg.Value != null)
			{
				switch (arg.Style)
				{
				case ArgumentStyle.Equals:
					stringBuilder.Append('=').Append(arg.Value);
					break;
				case ArgumentStyle.Space:
					stringBuilder.Append(' ').Append(arg.Value);
					break;
				}
			}
		}
		return stringBuilder.ToString();
	}

	public override string ToString()
	{
		return GetResult();
	}

	public void Clear()
	{
		_args.Clear();
	}

	private static string _HandleValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "\"" + value + "\"";
		}
		if (!value.All((char x) => !_CharNeedToQute.Contains(x)))
		{
			return "\"" + value.Replace("\"", "\\\"") + "\"";
		}
		return value;
	}
}

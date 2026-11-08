using System;
using System.Collections.Generic;

namespace PCL.Core.App.Cli;

internal static class _003CCommandLine_003EF0A5EDD4F96A9FE30F880579595FB8B309044B5413A2DBB528135CC7ECD6CF522__CommandLineParser
{
	private static (CommandArgument, bool) _ParseArgument(string key, string possibleValueText)
	{
		if (key.StartsWith("--"))
		{
			key = key.Substring(2);
		}
		if (possibleValueText.Length == 0 || possibleValueText.StartsWith("--"))
		{
			return (new BoolArgument
			{
				Key = key,
				ValueText = string.Empty
			}, false);
		}
		string text = possibleValueText.ToLowerInvariant();
		if ((text == "true" || text == "false") ? true : false)
		{
			return (new BoolArgument
			{
				Key = key,
				ValueText = possibleValueText
			}, true);
		}
		if (decimal.TryParse(possibleValueText, out var result))
		{
			return (new DecimalArgument
			{
				Key = key,
				ValueText = possibleValueText,
				Value = result
			}, true);
		}
		return (new TextArgument
		{
			Key = key,
			ValueText = possibleValueText
		}, true);
	}

	public static CommandLine Parse(ReadOnlySpan<string> args, SubcommandDefinition subcommands)
	{
		if (args.IsEmpty)
		{
			throw new ArgumentException("The argument span must contain at least 1 element", "args");
		}
		int i = 1;
		string commandText = args[0];
		Dictionary<string, CommandArgument> dictionary = new Dictionary<string, CommandArgument>();
		CommandLine subcommand = null;
		bool flag;
		for (; i < args.Length; i += ((!flag) ? 1 : 2))
		{
			string text = args[i];
			if (subcommands.Contains(text))
			{
				subcommand = Parse(args.Slice(i), subcommands.SubcommandMap[text]);
				break;
			}
			CommandArgument commandArgument;
			(commandArgument, flag) = _ParseArgument(text, (i == args.Length - 1 || subcommands.Contains(args[i + 1])) ? "" : args[i + 1]);
			dictionary[commandArgument.Key] = commandArgument;
		}
		return new CommandLine
		{
			CommandText = commandText,
			Arguments = dictionary.AsReadOnly(),
			Subcommand = subcommand
		};
	}
}

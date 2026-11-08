using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace PCL.Core.App.Cli;

[JsonConverter(typeof(CommandLineJsonConverter))]
public class CommandLine
{
	public required string CommandText { get; init; }

	public CommandLine? Subcommand { get; init; }

	public string? SubcommandText => Subcommand?.CommandText;

	public required IReadOnlyDictionary<string, CommandArgument> Arguments { get; init; }

	public (bool exists, bool isTypeMatch) TryGetArgumentValue<TValue>(string key, out TValue? value)
	{
		bool flag = Arguments.TryGetValue(key, out CommandArgument value2);
		bool item = false;
		if (flag && (item = value2.TryCastValue<TValue>(out TValue value3)))
		{
			value = value3;
			return (exists: true, isTypeMatch: true);
		}
		value = default;
		return (exists: flag, isTypeMatch: item);
	}

	public static CommandLine Parse(ReadOnlySpan<string> args, IEnumerable<SubcommandDefinition>? subcommands = null)
	{
		if (subcommands == null)
		{
			subcommands = Array.Empty<SubcommandDefinition>();
		}
		SubcommandDefinition subcommands2 = (commandText: args[0], subcommands: subcommands);
		return _003CCommandLine_003EF0A5EDD4F96A9FE30F880579595FB8B309044B5413A2DBB528135CC7ECD6CF522__CommandLineParser.Parse(args, subcommands2);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(CommandText).Append(" [");
		if (Arguments.Count > 0)
		{
			stringBuilder.AppendLine();
		}
		foreach (CommandArgument value in Arguments.Values)
		{
			stringBuilder.Append("  --").Append(value.Key);
			string text = value.ValueKind switch
			{
				ArgumentValueKind.Bool => value.CastValue<bool>() ? "true" : "false", 
				ArgumentValueKind.Decimal => value.CastValue<decimal>().ToString(CultureInfo.InvariantCulture), 
				ArgumentValueKind.Text => value.ValueText, 
				_ => null, 
			};
			if (text != null)
			{
				stringBuilder.Append(": ").Append(text);
			}
			stringBuilder.AppendLine();
		}
		stringBuilder.Append(']');
		if (Subcommand != null)
		{
			stringBuilder.AppendLine().Append("-> ").Append(Subcommand.ToString().Replace("\n", "\n   "));
		}
		return stringBuilder.ToString();
	}
}

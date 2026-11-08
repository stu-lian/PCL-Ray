using System;
using System.Collections.Generic;

namespace PCL.Core.App.Cli;

public class SubcommandDefinition
{
	public string CommandText { get; set; } = string.Empty;

	public IEnumerable<SubcommandDefinition> Subcommands { private get; set; } = Array.Empty<SubcommandDefinition>();

	public IReadOnlyDictionary<string, SubcommandDefinition> SubcommandMap
	{
		get
		{
			if (field != null)
			{
				return field;
			}
			Dictionary<string, SubcommandDefinition> dictionary = new Dictionary<string, SubcommandDefinition>();
			foreach (SubcommandDefinition subcommand in Subcommands)
			{
				dictionary[subcommand.CommandText] = subcommand;
			}
			return field = dictionary.AsReadOnly();
		}
	}

	public bool Contains(string subcommandText)
	{
		return SubcommandMap.ContainsKey(subcommandText);
	}

	public static implicit operator SubcommandDefinition((string commandText, IEnumerable<SubcommandDefinition> subcommands) tuple)
	{
		SubcommandDefinition subcommandDefinition = new SubcommandDefinition();
		(subcommandDefinition.CommandText, subcommandDefinition.Subcommands) = tuple;
		return subcommandDefinition;
	}

	public static implicit operator SubcommandDefinition(string commandText)
	{
		return new SubcommandDefinition
		{
			CommandText = commandText,
			Subcommands = Array.Empty<SubcommandDefinition>()
		};
	}
}

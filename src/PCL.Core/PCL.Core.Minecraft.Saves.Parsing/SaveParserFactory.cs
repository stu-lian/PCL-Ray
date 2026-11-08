using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.Minecraft.Saves.Parsing.Internal;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing;

public sealed class SaveParserFactory
{
	private readonly IReadOnlyList<ISaveParser> _parsers;

	public SaveParserFactory()
	{
		_parsers = new _003C_003Ez__ReadOnlyArray<ISaveParser>(new ISaveParser[6]
		{
			new Version261PlusSaveParser(),
			new Version116To1211SaveParser(),
			new Version113To1152SaveParser(),
			new Version19To1122SaveParser(),
			new Version131To189SaveParser(),
			new Pre113SaveParser()
		});
	}

	public SaveParserFactory(IEnumerable<ISaveParser> customParsers)
	{
		_parsers = customParsers?.ToArray() ?? Array.Empty<ISaveParser>();
	}

	public ISaveParser? Resolve(NbtCompound data, int? dataVersion)
	{
		foreach (ISaveParser parser in _parsers)
		{
			if (parser.CanHandle(data, dataVersion))
			{
				return parser;
			}
		}
		return null;
	}
}

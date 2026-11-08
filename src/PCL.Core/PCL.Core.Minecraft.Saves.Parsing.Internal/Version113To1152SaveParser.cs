using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Version113To1152SaveParser : ISaveParser
{
	private readonly ISaveParser _baseParser;

	public SaveFormatVersion FormatVersion => SaveFormatVersion.Version113To1152;

	public Version113To1152SaveParser()
		: this(new Version19To1122SaveParser())
	{
	}

	public Version113To1152SaveParser(ISaveParser baseParser)
	{
		_baseParser = baseParser;
	}

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (dataVersion.HasValue && dataVersion.Value >= 1443)
		{
			return dataVersion.Value < 2536;
		}
		return false;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		return _baseParser.Parse(folderPath, data, createdAt, modifiedAt);
	}
}

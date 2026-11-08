using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Version19To1122SaveParser : ISaveParser
{
	private readonly ISaveParser _baseParser;

	public SaveFormatVersion FormatVersion => SaveFormatVersion.Version19To1122;

	public Version19To1122SaveParser()
		: this(new Version131To189SaveParser())
	{
	}

	public Version19To1122SaveParser(ISaveParser baseParser)
	{
		_baseParser = baseParser;
	}

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (dataVersion.HasValue && dataVersion.Value >= 100)
		{
			return dataVersion.Value < 1443;
		}
		return false;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		SaveInfo saveInfo = _baseParser.Parse(folderPath, data, createdAt, modifiedAt);
		var (versionName, versionId) = NbtReadHelper.ReadVersion(data);
		return saveInfo with
		{
			VersionName = versionName,
			VersionId = versionId
		};
	}
}

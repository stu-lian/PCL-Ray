using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Version116To1211SaveParser : ISaveParser
{
	private readonly ISaveParser _baseParser;

	public SaveFormatVersion FormatVersion => SaveFormatVersion.Version116To1211;

	public Version116To1211SaveParser()
		: this(new Version19To1122SaveParser())
	{
	}

	public Version116To1211SaveParser(ISaveParser baseParser)
	{
		_baseParser = baseParser;
	}

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (dataVersion.HasValue && dataVersion.Value >= 2536)
		{
			return dataVersion.Value < 4774;
		}
		return false;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		return _baseParser.Parse(folderPath, data, createdAt, modifiedAt) with
		{
			Seed = ReadWorldGenSeed(data),
			Spawn = (NbtReadHelper.TryReadSpawnFromPos(data) ?? NbtReadHelper.TryReadSpawnFromFields(data))
		};
	}

	internal static long? ReadWorldGenSeed(NbtCompound data)
	{
		NbtCompound val = default;
		NbtLong val2 = default;
		if (data.TryGet<NbtCompound>("WorldGenSettings", out val) && val.TryGet<NbtLong>("seed", out val2))
		{
			return val2.Value;
		}
		return NbtReadHelper.TryGetLong(data, "RandomSeed");
	}
}

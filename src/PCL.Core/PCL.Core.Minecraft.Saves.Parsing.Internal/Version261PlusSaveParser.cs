using System;
using System.IO;
using System.Numerics;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Version261PlusSaveParser : ISaveParser
{
	private readonly ISaveParser _baseParser;

	public SaveFormatVersion FormatVersion => SaveFormatVersion.Version261Plus;

	public Version261PlusSaveParser()
		: this(new Version19To1122SaveParser())
	{
	}

	public Version261PlusSaveParser(ISaveParser baseParser)
	{
		_baseParser = baseParser;
	}

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (!(dataVersion >= 4774))
		{
			return data.Contains("difficulty_settings");
		}
		return true;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		SaveInfo saveInfo = _baseParser.Parse(folderPath, data, createdAt, modifiedAt);
		long? seed = Version116To1211SaveParser.ReadWorldGenSeed(data) ?? ReadSeedFromExternalFile(folderPath);
		Vector3? spawn = NbtReadHelper.TryReadSpawnFromPos(data) ?? NbtReadHelper.TryReadSpawnFromFields(data);
		Difficulty? difficulty = ReadDifficultySettings(data);
		bool flag = ReadHardcore(data);
		bool isDifficultyLocked = ReadLocked(data);
		return saveInfo with
		{
			Seed = seed,
			Spawn = spawn,
			Difficulty = difficulty,
			IsHardcore = flag,
			IsDifficultyLocked = isDifficultyLocked,
			GameMode = (flag ? GameMode.Hardcore : saveInfo.GameMode)
		};
	}

	internal static Difficulty? ReadDifficultySettings(NbtCompound data)
	{
		NbtCompound val = default;
		NbtString val2 = default;
		if (data.TryGet<NbtCompound>("difficulty_settings", out val) && val.TryGet<NbtString>("difficulty", out val2))
		{
			return val2.Value switch
			{
				"peaceful" => (Difficulty?)Difficulty.Peaceful, 
				"easy" => Difficulty.Easy, 
				"normal" => Difficulty.Normal, 
				"hard" => Difficulty.Hard, 
				_ => null, 
			};
		}
		return NbtReadHelper.ReadDifficultyByte(data);
	}

	internal static bool ReadHardcore(NbtCompound data)
	{
		NbtCompound val = default;
		NbtByte val2 = default;
		if (data.TryGet<NbtCompound>("difficulty_settings", out val) && val.TryGet<NbtByte>("hardcore", out val2))
		{
			return val2.Value == 1;
		}
		NbtByte val3 = default;
		if (data.TryGet<NbtByte>("hardcore", out val3))
		{
			return val3.Value == 1;
		}
		return false;
	}

	internal static bool ReadLocked(NbtCompound data)
	{
		NbtCompound val = default;
		NbtByte val2 = default;
		if (data.TryGet<NbtCompound>("difficulty_settings", out val) && val.TryGet<NbtByte>("locked", out val2))
		{
			return val2.Value == 1;
		}
		NbtByte val3 = default;
		if (data.TryGet<NbtByte>("DifficultyLocked", out val3))
		{
			return val3.Value == 1;
		}
		return false;
	}

	internal static long? ReadSeedFromExternalFile(string folderPath)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		string text = Path.Combine(folderPath, "data", "minecraft", "world_gen_settings.dat");
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			NbtCompound val = new NbtFile(text).RootTag.Get<NbtCompound>("data");
			NbtLong val2 = default;
			return (val != null && val.TryGet<NbtLong>("seed", out val2)) ? new long?(val2.Value) : ((long?)null);
		}
		catch
		{
			return null;
		}
	}
}

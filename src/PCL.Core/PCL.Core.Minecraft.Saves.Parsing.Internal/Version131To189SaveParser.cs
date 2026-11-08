using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Version131To189SaveParser : ISaveParser
{
	public SaveFormatVersion FormatVersion => SaveFormatVersion.Version131To189;

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (!dataVersion.HasValue)
		{
			return data.Contains("allowCommands");
		}
		return false;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		NbtString val = default;
		bool isHardcore;
		NbtByte val2 = default;
		NbtByte val3 = default;
		NbtByte val4 = default;
		return new SaveInfo
		{
			LevelName = (data.TryGet<NbtString>("LevelName", out val) ? val.Value : "unknown"),
			VersionName = null,
			VersionId = null,
			Seed = NbtReadHelper.TryGetLong(data, "RandomSeed"),
			LastPlayedUtc = NbtReadHelper.ReadLastPlayed(data),
			Spawn = NbtReadHelper.TryReadSpawnFromFields(data),
			GameMode = NbtReadHelper.ReadGameMode(data, out isHardcore),
			Difficulty = NbtReadHelper.ReadDifficultyByte(data),
			IsDifficultyLocked = (data.TryGet<NbtByte>("DifficultyLocked", out val2) && val2.Value == 1),
			IsHardcore = (data.TryGet<NbtByte>("hardcore", out val3) && val3.Value == 1),
			AllowCommands = (data.TryGet<NbtByte>("allowCommands", out val4) && val4.Value == 1),
			PlayTime = NbtReadHelper.ReadPlayTime(data),
			FolderPath = folderPath,
			CreatedAt = createdAt,
			ModifiedAt = modifiedAt
		};
	}
}

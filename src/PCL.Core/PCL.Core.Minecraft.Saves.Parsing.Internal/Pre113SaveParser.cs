using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal sealed class Pre113SaveParser : ISaveParser
{
	public SaveFormatVersion FormatVersion => SaveFormatVersion.Pre113;

	public bool CanHandle(NbtCompound data, int? dataVersion)
	{
		if (!dataVersion.HasValue)
		{
			return !data.Contains("allowCommands");
		}
		return false;
	}

	public SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt)
	{
		NbtString val = default;
		bool isHardcore;
		return new SaveInfo
		{
			LevelName = (data.TryGet<NbtString>("LevelName", out val) ? val.Value : "unknown"),
			VersionName = null,
			VersionId = null,
			Seed = NbtReadHelper.TryGetLong(data, "RandomSeed"),
			LastPlayedUtc = NbtReadHelper.ReadLastPlayed(data),
			Spawn = NbtReadHelper.TryReadSpawnFromFields(data),
			GameMode = NbtReadHelper.ReadGameMode(data, out isHardcore),
			Difficulty = null,
			IsDifficultyLocked = false,
			IsHardcore = isHardcore,
			AllowCommands = false,
			PlayTime = NbtReadHelper.ReadPlayTime(data),
			FolderPath = folderPath,
			CreatedAt = createdAt,
			ModifiedAt = modifiedAt
		};
	}
}

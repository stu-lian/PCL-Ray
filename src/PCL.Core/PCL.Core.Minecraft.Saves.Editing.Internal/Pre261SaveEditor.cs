using fNbt;

namespace PCL.Core.Minecraft.Saves.Editing.Internal;

internal sealed class Pre261SaveEditor : ISaveEditor
{
	public bool CanHandle(int? dataVersion)
	{
		if (dataVersion.HasValue)
		{
			return dataVersion < 4774;
		}
		return true;
	}

	public bool ApplyChanges(NbtCompound data, SaveChanges changes)
	{
		if (changes.IsEmpty)
		{
			return false;
		}
		return (byte)(0u | (WriteAllowCommands(data, changes) ? 1u : 0u) | (WriteDifficulty(data, changes) ? 1u : 0u) | (WriteDifficultyLocked(data, changes) ? 1u : 0u)) != 0;
	}

	internal static bool WriteAllowCommands(NbtCompound data, SaveChanges changes)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		if (!changes.AllowCommands.HasValue || !data.Contains("allowCommands"))
		{
			return false;
		}
		((NbtTag)data)["allowCommands"] = (NbtTag)new NbtByte("allowCommands", changes.AllowCommands.Value ? ((byte)1) : ((byte)0));
		return true;
	}

	internal static bool WriteDifficulty(NbtCompound data, SaveChanges changes)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected Obj, but got Unknown
		if (!changes.Difficulty.HasValue || !data.Contains("Difficulty"))
		{
			return false;
		}
		((NbtTag)data)["Difficulty"] = (NbtTag)new NbtByte("Difficulty", (byte)changes.Difficulty.Value);
		return true;
	}

	internal static bool WriteDifficultyLocked(NbtCompound data, SaveChanges changes)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		if (!changes.LockDifficulty.HasValue || !data.Contains("DifficultyLocked"))
		{
			return false;
		}
		((NbtTag)data)["DifficultyLocked"] = (NbtTag)new NbtByte("DifficultyLocked", changes.LockDifficulty.Value ? ((byte)1) : ((byte)0));
		return true;
	}
}

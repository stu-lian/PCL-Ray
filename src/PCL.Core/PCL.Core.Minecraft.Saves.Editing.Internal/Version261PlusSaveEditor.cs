using fNbt;

namespace PCL.Core.Minecraft.Saves.Editing.Internal;

internal sealed class Version261PlusSaveEditor : ISaveEditor
{
	public bool CanHandle(int? dataVersion)
	{
		return dataVersion >= 4774;
	}

	public bool ApplyChanges(NbtCompound data, SaveChanges changes)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected Obj, but got Unknown
		if (changes.IsEmpty)
		{
			return false;
		}
		NbtCompound val = default;
		if (!data.TryGet<NbtCompound>("difficulty_settings", out val) || val == null)
		{
			val = new NbtCompound("difficulty_settings");
			data.Add((NbtTag)(object)val);
		}
		return (byte)(0u | (Pre261SaveEditor.WriteAllowCommands(data, changes) ? 1u : 0u) | (WriteDifficulty(val, changes) ? 1u : 0u) | (WriteLocked(val, changes) ? 1u : 0u)) != 0;
	}

	internal static bool WriteDifficulty(NbtCompound difficultySettings, SaveChanges changes)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected Obj, but got Unknown
		if (!changes.Difficulty.HasValue)
		{
			return false;
		}
		((NbtTag)difficultySettings)["difficulty"] = (NbtTag)new NbtString("difficulty", changes.Difficulty.Value switch
		{
			Difficulty.Peaceful => "peaceful", 
			Difficulty.Easy => "easy", 
			Difficulty.Normal => "normal", 
			Difficulty.Hard => "hard", 
			_ => "normal", 
		});
		return true;
	}

	internal static bool WriteLocked(NbtCompound difficultySettings, SaveChanges changes)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected Obj, but got Unknown
		if (!changes.LockDifficulty.HasValue)
		{
			return false;
		}
		((NbtTag)difficultySettings)["locked"] = (NbtTag)new NbtByte("locked", changes.LockDifficulty.Value ? ((byte)1) : ((byte)0));
		return true;
	}
}

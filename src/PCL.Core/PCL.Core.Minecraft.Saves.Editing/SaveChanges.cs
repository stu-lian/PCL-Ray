namespace PCL.Core.Minecraft.Saves.Editing;

public record struct SaveChanges
{
	public Editable<bool> AllowCommands { get; set; }

	public Editable<Difficulty> Difficulty { get; set; }

	public Editable<bool> LockDifficulty { get; set; }

	public bool IsEmpty
	{
		get
		{
			if (!AllowCommands.HasValue && !Difficulty.HasValue)
			{
				return !LockDifficulty.HasValue;
			}
			return false;
		}
	}
}

using fNbt;

namespace PCL.Core.Minecraft.Saves.Editing;

public interface ISaveEditor
{
	bool CanHandle(int? dataVersion);

	bool ApplyChanges(NbtCompound data, SaveChanges changes);
}

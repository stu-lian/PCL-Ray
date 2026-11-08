using System;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing;

public interface ISaveParser
{
	SaveFormatVersion FormatVersion { get; }

	bool CanHandle(NbtCompound data, int? dataVersion);

	SaveInfo Parse(string folderPath, NbtCompound data, DateTime createdAt, DateTime modifiedAt);
}

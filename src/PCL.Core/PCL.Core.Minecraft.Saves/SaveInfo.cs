using System;
using System.Numerics;

namespace PCL.Core.Minecraft.Saves;

public sealed record SaveInfo
{
	public required string LevelName { get; init; }

	public string? VersionName { get; init; }

	public int? VersionId { get; init; }

	public long? Seed { get; init; }

	public DateTime LastPlayedUtc { get; init; }

	public Vector3? Spawn { get; init; }

	public GameMode GameMode { get; init; }

	public Difficulty? Difficulty { get; init; }

	public bool IsDifficultyLocked { get; init; }

	public bool IsHardcore { get; init; }

	public bool AllowCommands { get; init; }

	public TimeSpan PlayTime { get; init; }

	public required string FolderPath { get; init; }

	public DateTime CreatedAt { get; init; }

	public DateTime ModifiedAt { get; init; }
}

using System;
using System.Numerics;
using fNbt;

namespace PCL.Core.Minecraft.Saves.Parsing.Internal;

internal static class NbtReadHelper
{
	public static long? TryGetLong(NbtCompound data, string key)
	{
		NbtLong val = default;
		if (!data.TryGet<NbtLong>(key, out val))
		{
			return null;
		}
		return val.Value;
	}

	public static DateTime ReadLastPlayed(NbtCompound data)
	{
		return EpochMsToUtc(TryGetLong(data, "LastPlayed").GetValueOrDefault());
	}

	public static DateTime EpochMsToUtc(long ms)
	{
		return DateTime.UnixEpoch.AddMilliseconds(ms);
	}

	public static TimeSpan ReadPlayTime(NbtCompound data)
	{
		return TimeSpan.FromSeconds((double)TryGetLong(data, "Time").GetValueOrDefault() / 20.0);
	}

	public static GameMode ReadGameMode(NbtCompound data, out bool isHardcore)
	{
		NbtByte val = default;
		isHardcore = data.TryGet<NbtByte>("hardcore", out val) && val.Value == 1;
		if (isHardcore)
		{
			return GameMode.Hardcore;
		}
		NbtInt val2 = default;
		return (data.TryGet<NbtInt>("GameType", out val2) ? val2.Value : 0) switch
		{
			1 => GameMode.Creative, 
			2 => GameMode.Adventure, 
			3 => GameMode.Spectator, 
			_ => GameMode.Survival, 
		};
	}

	public static Vector3? TryReadSpawnFromFields(NbtCompound data)
	{
		NbtInt val = default;
		NbtInt val2 = default;
		NbtInt val3 = default;
		if (data.TryGet<NbtInt>("SpawnX", out val) && data.TryGet<NbtInt>("SpawnY", out val2) && data.TryGet<NbtInt>("SpawnZ", out val3))
		{
			return new Vector3(val.Value, val2.Value, val3.Value);
		}
		return null;
	}

	public static Vector3? TryReadSpawnFromPos(NbtCompound data)
	{
		NbtCompound val = default;
		NbtIntArray val2 = default;
		if (data.TryGet<NbtCompound>("spawn", out val) && val.TryGet<NbtIntArray>("pos", out val2) && val2.Value.Length == 3)
		{
			return new Vector3(val2[0], val2[1], val2[2]);
		}
		return null;
	}

	public static Difficulty? ReadDifficultyByte(NbtCompound data)
	{
		NbtByte val = default;
		if (data.TryGet<NbtByte>("Difficulty", out val))
		{
			return (Difficulty)val.Value;
		}
		return null;
	}

	public static (string? name, int? id) ReadVersion(NbtCompound data)
	{
		NbtCompound val = default;
		if (data.TryGet<NbtCompound>("Version", out val))
		{
			NbtString val2 = default;
			string item = (val.TryGet<NbtString>("Name", out val2) ? val2.Value : null);
			NbtInt val3 = default;
			int? item2 = (val.TryGet<NbtInt>("Id", out val3) ? new int?(val3.Value) : ((int?)null));
			return (name: item, id: item2);
		}
		return (name: null, id: null);
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Profile;

public sealed class ProfileManagement<TProfileModel> where TProfileModel : SafeProfile
{
	private readonly object _syncLock = new object();

	private ProfileJson<TProfileModel> _profiles = new ProfileJson<TProfileModel>();

	private TProfileModel? _current;

	public bool IsLoaded { get; private set; }

	public int LastUsed
	{
		get
		{
			lock (_syncLock)
			{
				return _profiles.LastUsed;
			}
		}
	}

	public TProfileModel? Current
	{
		get
		{
			lock (_syncLock)
			{
				return _current;
			}
		}
	}

	public IReadOnlyList<TProfileModel> GetAll()
	{
		lock (_syncLock)
		{
			return _profiles.Profiles.ToArray();
		}
	}

	public void Add(TProfileModel profile, bool select = false)
	{
		ArgumentNullException.ThrowIfNull(profile, "profile");
		lock (_syncLock)
		{
			_EnsureLoaded();
			if (!string.IsNullOrWhiteSpace(profile.ProfileId) && _profiles.Profiles.Any((TProfileModel p) => p.ProfileId == profile.ProfileId))
			{
				throw new InvalidOperationException("A profile with id '" + profile.ProfileId + "' already exists.");
			}
			_profiles.Profiles.Add(profile);
			if (select)
			{
				_SelectUnsafe(profile);
			}
		}
	}

	public void Delete(TProfileModel profile)
	{
		ArgumentNullException.ThrowIfNull(profile, "profile");
		lock (_syncLock)
		{
			_EnsureLoaded();
			int num = _profiles.Profiles.IndexOf(profile);
			if (num < 0 && !string.IsNullOrWhiteSpace(profile.ProfileId))
			{
				num = _profiles.Profiles.FindIndex((TProfileModel p) => p.ProfileId == profile.ProfileId);
			}
			if (num >= 0)
			{
				TProfileModel val = _profiles.Profiles[num];
				bool num2 = _current == val || (!string.IsNullOrWhiteSpace(val.ProfileId) && _current?.ProfileId == val.ProfileId);
				_profiles.Profiles.RemoveAt(num);
				if (num2)
				{
					_current = null;
					_profiles.LastUsed = -1;
				}
				else if (num < _profiles.LastUsed)
				{
					_profiles.LastUsed--;
				}
				_NormalizeLastUsedUnsafe();
			}
		}
	}

	public void Update(TProfileModel origin, TProfileModel current)
	{
		ArgumentNullException.ThrowIfNull(origin, "origin");
		ArgumentNullException.ThrowIfNull(current, "current");
		lock (_syncLock)
		{
			_EnsureLoaded();
			int index = _profiles.Profiles.IndexOf(origin);
			if (index < 0 && !string.IsNullOrWhiteSpace(origin.ProfileId))
			{
				index = _profiles.Profiles.FindIndex((TProfileModel p) => p.ProfileId == origin.ProfileId);
			}
			if (index < 0)
			{
				throw new KeyNotFoundException("The source profile does not exist.");
			}
			if (string.IsNullOrWhiteSpace(current.ProfileId))
			{
				current.ProfileId = origin.ProfileId;
			}
			if (!string.IsNullOrWhiteSpace(current.ProfileId) && _profiles.Profiles.Where((TProfileModel _, int profileIndex) => profileIndex != index).Any((TProfileModel profile) => profile.ProfileId == current.ProfileId))
			{
				throw new InvalidOperationException("A profile with id '" + current.ProfileId + "' already exists.");
			}
			_profiles.Profiles[index] = current;
			if (_current == origin || _current?.ProfileId == origin.ProfileId)
			{
				_current = current;
			}
		}
	}

	public void Select(TProfileModel? profile)
	{
		lock (_syncLock)
		{
			_EnsureLoaded();
			if (profile == null)
			{
				_current = null;
				_profiles.LastUsed = -1;
				return;
			}
			int num = _profiles.Profiles.IndexOf(profile);
			if (num < 0 && !string.IsNullOrWhiteSpace(profile.ProfileId))
			{
				num = _profiles.Profiles.FindIndex((TProfileModel p) => p.ProfileId == profile.ProfileId);
			}
			if (num < 0)
			{
				throw new KeyNotFoundException("The selected profile does not exist.");
			}
			_SelectUnsafe(_profiles.Profiles[num]);
		}
	}

	public void SelectAt(int index)
	{
		lock (_syncLock)
		{
			_EnsureLoaded();
			if (index < 0 || index >= _profiles.Profiles.Count)
			{
				_current = null;
				_profiles.LastUsed = -1;
			}
			else
			{
				_SelectUnsafe(_profiles.Profiles[index]);
			}
		}
	}

	public void LoadFromString(string profiles)
	{
		lock (_syncLock)
		{
			ProfileJson<TProfileModel> profileJson = (string.IsNullOrWhiteSpace(profiles) ? new ProfileJson<TProfileModel>() : (JsonSerializer.Deserialize<ProfileJson<TProfileModel>>(profiles, JsonCompat.SerializerOptions) ?? new ProfileJson<TProfileModel>()));
			ProfileJson<TProfileModel> profileJson2 = profileJson;
			if (profileJson2.Profiles == null)
			{
				List<TProfileModel> list = (profileJson2.Profiles = new List<TProfileModel>());
			}
			IGrouping<string, TProfileModel> grouping = profileJson.Profiles.Where((TProfileModel profile) => !string.IsNullOrWhiteSpace(profile.ProfileId)).GroupBy((TProfileModel profile) => profile.ProfileId, StringComparer.Ordinal).FirstOrDefault((IGrouping<string, TProfileModel> group) => group.Count() > 1);
			if (grouping != null)
			{
				throw new InvalidDataException("Duplicate profile id: " + grouping.Key);
			}
			_profiles = profileJson;
			IsLoaded = true;
			_NormalizeLastUsedUnsafe();
			_current = ((_profiles.LastUsed >= 0) ? _profiles.Profiles[_profiles.LastUsed] : null);
		}
	}

	public string Serialize()
	{
		lock (_syncLock)
		{
			_EnsureLoaded();
			return JsonSerializer.Serialize(_profiles, JsonCompat.SerializerOptions);
		}
	}

	public void Clear()
	{
		lock (_syncLock)
		{
			_EnsureLoaded();
			_profiles = new ProfileJson<TProfileModel>();
			_current = null;
		}
	}

	private void _SelectUnsafe(TProfileModel profile)
	{
		_current = profile;
		_profiles.LastUsed = _profiles.Profiles.IndexOf(profile);
	}

	private void _NormalizeLastUsedUnsafe()
	{
		if (_profiles.Profiles.Count == 0)
		{
			_profiles.LastUsed = -1;
		}
		else if (_profiles.LastUsed < 0 || _profiles.LastUsed >= _profiles.Profiles.Count)
		{
			_profiles.LastUsed = -1;
		}
	}

	private void _EnsureLoaded()
	{
		if (!IsLoaded)
		{
			throw new InvalidOperationException("The profile list has not been loaded.");
		}
	}
}

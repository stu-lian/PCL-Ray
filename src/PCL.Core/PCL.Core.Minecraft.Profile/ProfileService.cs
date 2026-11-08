using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.IoC;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Minecraft.Profile;

[LifecycleScope("profile", "档案服务", true)]
[LifecycleService(LifecycleState.Loaded)]
public class ProfileService : ILifecycleService
{
	private static readonly ProfileManagement<McProfile> _profiles = new ProfileManagement<McProfile>();

	private static readonly object _saveLock = new object();

	private static bool _canPersist = true;

	public static IReadOnlyList<McProfile> Profiles => _profiles.GetAll();

	public static McProfile? Current => _profiles.Current;

	public static int LastUsedProfile => _profiles.LastUsed;

	public static bool IsLoaded => _profiles.IsLoaded;

	public static bool IsCreatingProfile { get; set; }

	public static bool HasMicrosoftProfile => Profiles.Any((McProfile p) => p.ProfileType == ProfileType.Microsoft);

	public string Identifier => "profile";

	public string Name => "档案服务";

	public bool SupportAsync => true;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	} = null;

	private static ILifecycleService Service => Context.ServiceInstance;

	[LifecycleStart]
	private static Task _Start()
	{
		Load();
		return Task.CompletedTask;
	}

	[LifecycleStop]
	private static void _Stop()
	{
		if (IsLoaded)
		{
			Save();
		}
	}

	public static void Load()
	{
		if (_profiles.IsLoaded)
		{
			return;
		}
		bool value = ConfigService.GetProvider(ConfigSource.SharedEncrypt).GetValue<string>("Profile", out string value2);
		string migrated;
		if (!value && ConfigService.GetProvider(ConfigSource.Shared).GetValue<string>("Profile", out string value3))
		{
			if (_TryLoad(value3))
			{
				_EnsureProfileIds();
				Save();
			}
			else
			{
				Context.Error("档案加密配置无法读取；为避免覆盖原始凭据，本次不会保存档案配置。");
				_canPersist = false;
				_profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
			}
		}
		else if (value)
		{
			if (_TryLoad(value2))
			{
				_EnsureProfileIds();
				return;
			}
			Context.Error("档案配置格式无效；为避免覆盖原始凭据，本次不会保存档案配置。");
			_canPersist = false;
			_profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
		}
		else if (!_TryMigrateLegacy(out migrated))
		{
			Context.Error("迁移旧档案失败；为避免覆盖原始凭据，本次不会保存档案配置。");
			_canPersist = false;
			_profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
		}
		else
		{
			_profiles.LoadFromString(migrated ?? "{\"lastUsed\":-1,\"profiles\":[]}");
			Save();
		}
	}

	public static void Save()
	{
		if (!IsLoaded || !_canPersist)
		{
			return;
		}
		lock (_saveLock)
		{
			Config.System.ProfilesConfig.SetValue(_profiles.Serialize(), null, forceNewValue: true);
		}
	}

	public static void Add(McProfile profile, bool select = true)
	{
		_EnsureLoaded();
		if (string.IsNullOrWhiteSpace(profile.ProfileId))
		{
			profile.ProfileId = Guid.NewGuid().ToString("N");
		}
		_profiles.Add(profile, select);
		Save();
	}

	public static void Update(McProfile origin, McProfile current)
	{
		_EnsureLoaded();
		_profiles.Update(origin, current);
		Save();
	}

	public static void Remove(McProfile profile)
	{
		_EnsureLoaded();
		_profiles.Delete(profile);
		Save();
	}

	public static void Select(McProfile? profile)
	{
		_EnsureLoaded();
		_profiles.Select(profile);
		Save();
	}

	public static void SelectAt(int index)
	{
		_EnsureLoaded();
		_profiles.SelectAt(index);
		Save();
	}

	public static void Clear()
	{
		_EnsureLoaded();
		_profiles.Clear();
		Save();
	}

	public static async Task<McProfile> AuthenticateAsync(ProfileType profileType, AuthenticationRequest request, McProfile? existing, bool select, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		_EnsureLoaded();
		bool canUseExisting = existing != null && existing.ProfileType == profileType;
		bool flag = canUseExisting && !request.ForceRefresh;
		if (flag)
		{
			bool flag2 = ((profileType == ProfileType.Microsoft || profileType == ProfileType.YggdrasilConnect) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			DateTimeOffset? expiresAt = existing.ExpiresAt;
			if (expiresAt.HasValue)
			{
				DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
				if (valueOrDefault > DateTimeOffset.UtcNow.AddMinutes(1.0) && !string.IsNullOrWhiteSpace(existing.AccessToken))
				{
					if (select)
					{
						Select(existing);
					}
					return existing;
				}
			}
		}
		IAuthenticateProvider provider = await _CreateProviderAsync(profileType, request, existing, token).ConfigureAwait(continueOnCapturedContext: false);
		AuthenticationResult result = null;
		if (canUseExisting && profileType == ProfileType.Authlib && !request.ForceRefresh && !request.ForceReselectProfile)
		{
			try
			{
				if (await provider.ValidateAsync(existing, token).ConfigureAwait(continueOnCapturedContext: false))
				{
					if (select)
					{
						Select(existing);
					}
					return existing;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Context.Warn("验证第三方档案失败，将尝试刷新", ex2);
			}
		}
		if (canUseExisting && (!request.ForceReselectProfile || profileType != ProfileType.Authlib))
		{
			try
			{
				result = await provider.RefreshAsync(existing, token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (IdentityModelAuthenticationException ex4)
			{
				Context.Warn("刷新档案失败，将尝试重新认证", ex4);
				if (ex4.Error?.StartsWith("xsts_", StringComparison.OrdinalIgnoreCase) ?? false)
				{
					throw;
				}
				flag = !string.Equals(ex4.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase) && request.RefreshFailureHandler != null;
				if (flag)
				{
					flag = await request.RefreshFailureHandler(ex4, token).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag)
				{
					if (select)
					{
						Select(existing);
					}
					return existing;
				}
			}
			catch (Exception ex5)
			{
				flag = request.RefreshFailureHandler != null;
				if (flag)
				{
					flag = await request.RefreshFailureHandler(ex5, token).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag)
				{
					if (select)
					{
						Select(existing);
					}
					return existing;
				}
				if (profileType != ProfileType.Authlib || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
				{
					throw;
				}
				Context.Warn("刷新第三方档案失败，将尝试使用凭据重新认证", ex5);
			}
		}
		if (result == null)
		{
			AuthenticationRequest request2 = request with
			{
				RefreshToken = null,
				IdToken = (existing?.IdToken ?? request.IdToken)
			};
			result = await provider.AuthenticateAsync(request2, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		McProfile existing2 = (canUseExisting ? existing : _FindMatchingProfile(result));
		return ApplyAuthenticationResult(result, existing2, select);
	}

	public static McProfile ApplyAuthenticationResult(AuthenticationResult result, McProfile? existing = null, bool select = true)
	{
		ArgumentNullException.ThrowIfNull(result, "result");
		McProfile mcProfile = existing?.Clone() ?? new McProfile
		{
			ProfileId = Guid.NewGuid().ToString("N")
		};
		mcProfile.ProfileType = result.ProfileType;
		mcProfile.UserName = result.UserName;
		mcProfile.Uuid = result.Uuid;
		mcProfile.AccessToken = result.AccessToken;
		mcProfile.RefreshToken = result.RefreshToken;
		mcProfile.ClientToken = result.ClientToken;
		mcProfile.TokenType = result.TokenType;
		mcProfile.ExpiresAt = result.ExpiresAt;
		mcProfile.RawJson = result.RawJson;
		mcProfile.Server = result.Server ?? mcProfile.Server;
		mcProfile.ServerName = result.ServerName ?? mcProfile.ServerName;
		mcProfile.LoginName = result.LoginName ?? mcProfile.LoginName;
		mcProfile.Password = result.Password ?? mcProfile.Password;
		mcProfile.Provider = result.Provider ?? mcProfile.Provider;
		mcProfile.DiscoveryAddress = result.DiscoveryAddress ?? mcProfile.DiscoveryAddress;
		mcProfile.IdToken = result.IdToken ?? mcProfile.IdToken;
		if (existing == null)
		{
			Add(mcProfile, select);
		}
		else
		{
			Update(existing, mcProfile);
			if (select)
			{
				Select(mcProfile);
			}
		}
		return mcProfile;
	}

	private static async Task<IAuthenticateProvider> _CreateProviderAsync(ProfileType profileType, AuthenticationRequest request, McProfile? existing, CancellationToken token)
	{
		switch (profileType)
		{
		case ProfileType.Microsoft:
			return new MicrosoftProvider();
		case ProfileType.Authlib:
			return new AuthlibProvider(request.Server ?? existing?.Server ?? throw new InvalidOperationException("Authlib profile has no server."));
		case ProfileType.YggdrasilConnect:
		{
			YggdrasilConnectProvider provider = new YggdrasilConnectProvider(request.DiscoveryAddress ?? existing?.DiscoveryAddress ?? throw new InvalidOperationException("Yggdrasil Connect profile has no discovery address."), request.ClientId, request.Server ?? existing?.Server);
			await provider.InitializeAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			return provider;
		}
		default:
			throw new InvalidOperationException($"Profile type '{profileType}' does not require remote authentication.");
		}
	}

	private static McProfile? _FindMatchingProfile(AuthenticationResult result)
	{
		return Profiles.FirstOrDefault((McProfile profile) => profile.ProfileType == result.ProfileType && string.Equals(profile.Uuid, result.Uuid, StringComparison.Ordinal) && (result.ProfileType == ProfileType.Microsoft || string.Equals(profile.Server, result.Server, StringComparison.OrdinalIgnoreCase)));
	}

	private static void _EnsureLoaded()
	{
		if (!IsLoaded)
		{
			Load();
		}
	}

	private static bool _TryLoad(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}
		try
		{
			if (!(JsonCompat.ParseNode(json) is JsonObject jsonObject) || !(jsonObject["profiles"] is JsonArray))
			{
				return false;
			}
			_profiles.LoadFromString(json);
			return true;
		}
		catch (Exception ex)
		{
			Context.Warn("读取新档案配置失败，将尝试迁移旧档案", ex);
			return false;
		}
	}

	private static void _EnsureProfileIds()
	{
		bool flag = false;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (McProfile profile in Profiles)
		{
			if (string.IsNullOrWhiteSpace(profile.ProfileId) || !hashSet.Add(profile.ProfileId))
			{
				do
				{
					profile.ProfileId = Guid.NewGuid().ToString("N");
				}
				while (!hashSet.Add(profile.ProfileId));
				flag = true;
			}
		}
		if (flag)
		{
			Save();
		}
	}

	private static bool _TryMigrateLegacy(out string? migrated)
	{
		migrated = null;
		string text = new string[2]
		{
			Path.Combine(Paths.OldSharedData, "profiles.json"),
			Path.Combine(Paths.SharedData, "profiles.json")
		}.FirstOrDefault(File.Exists);
		if (text == null)
		{
			return true;
		}
		try
		{
			JsonObject jsonObject = JsonCompat.ParseNode(File.ReadAllText(text)) as JsonObject;
			if (!(jsonObject?["profiles"] is JsonArray source))
			{
				return false;
			}
			ProfileJson<McProfile> profileJson = new ProfileJson<McProfile>
			{
				LastUsed = (jsonObject["lastUsed"]?.GetValue<int>() ?? (-1))
			};
			foreach (JsonObject item in source.OfType<JsonObject>())
			{
				string text2 = item["type"]?.ToString()?.ToLowerInvariant();
				McProfile mcProfile = new McProfile();
				mcProfile.ProfileId = Guid.NewGuid().ToString("N");
				mcProfile.UserName = item["username"]?.ToString() ?? string.Empty;
				mcProfile.Uuid = item["uuid"]?.ToString() ?? string.Empty;
				mcProfile.Description = item["desc"]?.ToString() ?? string.Empty;
				mcProfile.SkinHeadId = item["skinHeadId"]?.ToString() ?? string.Empty;
				mcProfile.Server = item["server"]?.ToString();
				mcProfile.ServerName = item["serverName"]?.ToString();
				mcProfile.LoginName = _Decrypt(item["name"]?.ToString());
				mcProfile.Password = _Decrypt(item["password"]?.ToString());
				mcProfile.ClientToken = _Decrypt(item["clientToken"]?.ToString());
				mcProfile.AccessToken = _Decrypt(item["accessToken"]?.ToString());
				mcProfile.RefreshToken = _Decrypt(item["refreshToken"]?.ToString());
				mcProfile.RawJson = _Decrypt(item["rawJson"]?.ToString());
				mcProfile.TokenType = "Bearer";
				McProfile mcProfile2 = mcProfile;
				ProfileType profileType = ((!(text2 == "microsoft")) ? ((!(text2 == "authlib")) ? ProfileType.Offline : ProfileType.Authlib) : ProfileType.Microsoft);
				mcProfile2.ProfileType = profileType;
				McProfile mcProfile3 = mcProfile;
				long? num = item["expires"]?.GetValue<long>();
				if (num.HasValue)
				{
					long valueOrDefault = num.GetValueOrDefault();
					if (valueOrDefault > 0)
					{
						try
						{
							mcProfile3.ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(valueOrDefault);
						}
						catch (ArgumentOutOfRangeException)
						{
							mcProfile3.ExpiresAt = null;
						}
					}
				}
				profileJson.Profiles.Add(mcProfile3);
			}
			migrated = JsonSerializer.Serialize(profileJson, JsonCompat.SerializerOptions);
			return true;
		}
		catch (Exception ex2)
		{
			Context.Error("迁移旧档案失败", ex2);
			return false;
		}
	}

	private static string _Decrypt(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		try
		{
			return EncryptHelper.SecretDecrypt(value);
		}
		catch
		{
			return value;
		}
	}

	public ProfileService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_Stop();
	}

	public async Task StartAsync()
	{
		await _Start();
	}
}

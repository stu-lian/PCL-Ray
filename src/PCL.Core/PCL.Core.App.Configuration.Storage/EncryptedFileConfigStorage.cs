using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.Secret;

namespace PCL.Core.App.Configuration.Storage;

public class EncryptedFileConfigStorage(ConfigStorage source) : ConfigStorage
{
	private static readonly JsonSerializerOptions _SerializerOptions = new JsonSerializerOptions(JsonCompat.SerializerOptions)
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		AllowOutOfOrderMetadataProperties = true
	};

	public ConfigStorage Source { get; } = source;

	protected override bool OnAccess<TKey, TValue>(StorageAction action, ref TKey key, [NotNullWhen(true)] ref TValue value, object? argument)
	{
		try
		{
			TValue val;
			object obj;
			string data;
			switch (action)
			{
			case StorageAction.Set:
				if (typeof(TValue) == typeof(string))
				{
					scoped ref TValue reference2 = ref value;
					val = default;
					if (val == null)
					{
						val = reference2;
						reference2 = ref val;
						if (val == null)
						{
							obj = null;
							goto IL_0058;
						}
					}
					obj = reference2.ToString();
					goto IL_0058;
				}
				data = JsonSerializer.Serialize(value, _SerializerOptions);
				goto IL_0075;
			case StorageAction.Get:
			{
				string value2 = null;
				bool flag = Source.Access(StorageAction.Get, ref key, ref value2, argument);
				if (!flag)
				{
					return false;
				}
				string text = EncryptHelper.SecretDecrypt(value2);
				Type typeFromHandle = typeof(TValue);
				if (typeFromHandle == typeof(bool))
				{
					ref bool reference = ref Unsafe.As<TValue, bool>(ref value);
					string text2 = text.ToLowerInvariant();
					bool flag2 = ((text2 == "true" || text2 == "1") ? true : false);
					reference = flag2;
				}
				else if (typeFromHandle == typeof(string))
				{
					Unsafe.As<TValue, string>(ref value) = text;
				}
				else
				{
					val = JsonSerializer.Deserialize<TValue>(text, _SerializerOptions);
					if (val == null)
					{
						throw new NullReferenceException("Decryption produced a null reference");
					}
					value = val;
				}
				return flag;
			}
			default:
				{
					return Source.Access(action, ref key, ref value, argument);
				}
				IL_0058:
				if (obj == null)
				{
					obj = string.Empty;
				}
				data = (string)obj;
				goto IL_0075;
				IL_0075:
				data = EncryptHelper.SecretEncrypt(data);
				return Source.Access(StorageAction.Set, ref key, ref data, argument);
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Config", "无法处理加解密");
			return false;
		}
	}
}

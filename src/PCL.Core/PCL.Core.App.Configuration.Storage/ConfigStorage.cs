using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using PCL.Core.App.IoC;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.Diagnostics;

namespace PCL.Core.App.Configuration.Storage;

public abstract class ConfigStorage : IConfigProvider
{
	private static readonly JsonSerializerOptions _SerializerOptions = new JsonSerializerOptions(JsonCompat.SerializerOptions)
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	protected abstract bool OnAccess<TKey, TValue>(StorageAction action, ref TKey key, [NotNullWhen(true)] ref TValue value, object? argument);

	protected virtual void OnStop()
	{
	}

	public void Stop()
	{
		OnStop();
	}

	public bool Access<TKey, TValue>(StorageAction action, ref TKey key, [NotNullWhen(true)] ref TValue value, object? argument)
	{
		bool flag = false;
		try
		{
			flag = OnAccess(action, ref key, ref value, argument);
		}
		catch (Exception value2)
		{
			string msg = $"Config Storage Error Report\nA exception was thrown while processing an access.\n\n[Diagnostics Info]\n{_GenerateDiagnosticsInfo(action, key, value, flag, argument, appendCallStack: true)}\n\n[Exception Details]\n{value2}";
			LogWrapper.Fatal("Config", msg);
			Lifecycle.ForceShutdown(-2);
		}
		return flag;
	}

	private string _GenerateDiagnosticsInfo<TKey, TValue>(StorageAction accessAction, TKey? accessKey, TValue? accessValue, bool accessHasValue, object? accessContext, bool appendCallStack = false)
	{
		string value = JsonSerializer.Serialize(accessContext, _SerializerOptions);
		string value2 = JsonSerializer.Serialize(accessKey, _SerializerOptions);
		string text = JsonSerializer.Serialize(accessValue, _SerializerOptions);
		string value3 = (appendCallStack ? ("Stack:\n|=> " + string.Join("\n|=> ", StackHelper.GetStack(10, includeNamespace: true, includeParameters: true).Skip(1))) : ("Caller: " + StackHelper.GetDirectCallerName(includeNamespace: true, includeParameters: true, 2)));
		return $"Storage Access: {accessAction} {ToString()}\n|- Context: {((accessContext == null) ? "" : ("(" + accessContext.GetType().Name + ") "))}{value}\n|- Key: ({typeof(TKey).Name}) {value2}\n|- Value: ({typeof(TValue).Name}) {(accessHasValue ? text : "undefined")}\n|- {value3}";
	}

	public bool GetValue<T>(string key, [NotNullWhen(true)] out T? value, object? argument = null)
	{
		string key2 = key;
		T value2 = default;
		bool result = Access(StorageAction.Get, ref key2, ref value2, argument);
		value = value2;
		return result;
	}

	public void SetValue<T>(string key, T value, object? argument = null)
	{
		string key2 = key;
		T value2 = value;
		Access(StorageAction.Set, ref key2, ref value2, argument);
	}

	public void Delete(string key, object? argument = null)
	{
		string key2 = key;
		object value = null;
		Access(StorageAction.Delete, ref key2, ref value, argument);
	}

	public bool Exists(string key, object? argument = null)
	{
		string key2 = key;
		bool value = false;
		return Access(StorageAction.Exists, ref key2, ref value, argument) & value;
	}

	public override string ToString()
	{
		return $"{GetType().Name}@{GetHashCode()}";
	}
}

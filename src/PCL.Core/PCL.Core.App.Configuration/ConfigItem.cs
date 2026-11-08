using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App.Configuration;

public class ConfigItem<TValue>(string key, Func<TValue> defaultValue, ConfigSource source) : IConfigScope, ConfigItem
{
	private Func<TValue>? _defaultValueConstructor = defaultValue;

	private TValue? _defaultValue;

	private bool _defaultValueHasSet;

	private ConfigValueCache<TValue> _valueCache = new ConfigValueCache<TValue>();

	private readonly HashSet<ConfigObserver> _observers = new HashSet<ConfigObserver>();

	private readonly HashSet<ConfigObserver> _previewObservers = new HashSet<ConfigObserver>();

	public string Key { get; } = key;

	public ConfigSource Source { get; } = source;

	public Type Type => typeof(TValue);

	public TValue DefaultValue => _GetDefaultValue();

	public object DefaultValueNoType
	{
		get
		{
			TValue defaultValue = DefaultValue;
			return (defaultValue != null) ? defaultValue : default(TValue);
		}
	}

	private IConfigProvider _Provider => field ?? (field = ConfigService.GetProvider(Source));

	public bool EnableCache
	{
		get;
		set
		{
			if (!field)
			{
				_valueCache.InvalidateAll();
			}
			field = value;
		}
	} = true;

	private TValue _GetDefaultValue()
	{
		if (_defaultValueHasSet)
		{
			return _defaultValue;
		}
		_defaultValue = _defaultValueConstructor();
		_defaultValueHasSet = true;
		_defaultValueConstructor = null;
		return _defaultValue;
	}

	public ConfigItem(string key, TValue defaultValue, ConfigSource source)
		: this(key, (Func<TValue>)(() => defaultValue), source)
	{
	}

	public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
	{
		if (!keys.Contains(Key))
		{
			return Array.Empty<string>();
		}
		return new _003C_003Ez__ReadOnlySingleElementList<string>(Key);
	}

	private bool _ProcessNewCache(TValue newCache, object? argument, bool force = false)
	{
		if (!EnableCache)
		{
			return true;
		}
		if (!force && _valueCache.TryRead(out TValue value, argument) && EqualityComparer<TValue>.Default.Equals(value, newCache))
		{
			return false;
		}
		if (newCache is INotifyPropertyChanged notifyPropertyChanged)
		{
			notifyPropertyChanged.PropertyChanged += (object? _, PropertyChangedEventArgs _) =>
			{
				OnContentChanged();
			};
		}
		else if (newCache is INotifyCollectionChanged notifyCollectionChanged)
		{
			notifyCollectionChanged.CollectionChanged += (object? _, NotifyCollectionChangedEventArgs _) =>
			{
				OnContentChanged();
			};
		}
		_valueCache.Write(newCache, argument);
		return true;
		void OnContentChanged()
		{
			SetValue(newCache, argument, forceNewValue: false, bypassCache: true);
		}
	}

	public TValue GetValue(object? argument = null)
	{
		TValue value = default;
		bool flag = EnableCache && _valueCache.TryRead(out value, argument);
		bool flag2 = false;
		if (!flag)
		{
			flag2 = true;
			flag = _Provider.GetValue<TValue>(Key, out value, argument);
		}
		ConfigEventArgs e = _TriggerEvent(ConfigEvent.Get, argument, value, bypassOldValue: true);
		if ((object)e != null)
		{
			if (e.Cancelled)
			{
				return DefaultValue;
			}
			if (e.NewValueReplacement != null)
			{
				return (TValue)e.NewValueReplacement;
			}
		}
		if (!flag)
		{
			value = DefaultValue;
		}
		if (flag2)
		{
			_ProcessNewCache(value, argument);
		}
		return value;
	}

	public object GetValueNoType(object? argument = null)
	{
		TValue value = GetValue(argument);
		return (value != null) ? value : default(TValue);
	}

	public bool SetValue(TValue value, object? argument = null, bool forceNewValue = false, bool bypassCache = false)
	{
		ConfigEventArgs e = _TriggerEvent(ConfigEvent.Set, argument, value, bypassOldValue: false, fillNewValue: false, null, true);
		if ((object)e != null)
		{
			if (e.Cancelled)
			{
				return false;
			}
			if (e.NewValueReplacement != null)
			{
				value = (TValue)e.NewValueReplacement;
			}
		}
		if (bypassCache || _ProcessNewCache(value, argument, forceNewValue))
		{
			_Provider.SetValue(Key, value, argument);
		}
		_TriggerEvent(ConfigEvent.Set, argument, value, bypassOldValue: false, fillNewValue: false, e, false);
		return true;
	}

	public bool SetValueNoType(object value, object? argument = null)
	{
		try
		{
			return SetValue((TValue)value, argument);
		}
		catch (InvalidCastException)
		{
			if (value is string value2)
			{
				return SetValue(value2.Convert<TValue>(), argument);
			}
			throw new InvalidCastException($"Value convert failed (required: {Type.FullName}, provided: {value.GetType().FullName})");
		}
	}

	public bool SetDefaultValue(object? argument = null, bool? forceNewValue = null)
	{
		return SetValue(DefaultValue, argument, forceNewValue ?? IsDefault(argument));
	}

	public bool Reset(object? argument = null)
	{
		ConfigEventArgs e = _TriggerEvent(ConfigEvent.Reset, argument, null, bypassOldValue: false, fillNewValue: false, null, true);
		if ((object)e != null && e.Cancelled)
		{
			return false;
		}
		_Provider.Delete(Key, argument);
		if (EnableCache)
		{
			_valueCache.Invalidate(argument);
		}
		_TriggerEvent(ConfigEvent.Reset, argument, DefaultValueNoType, bypassOldValue: false, fillNewValue: false, null, false);
		return true;
	}

	public bool IsDefault(object? argument = null)
	{
		bool flag = !_Provider.Exists(Key, argument);
		ConfigEventArgs e = _TriggerEvent(ConfigEvent.CheckDefault, argument, flag);
		if ((object)e != null && e.NewValueReplacement != null)
		{
			flag = (bool)e.NewValueReplacement;
		}
		return flag;
	}

	public void Observe(ConfigObserver observer)
	{
		if (observer.IsPreview)
		{
			_previewObservers.Add(observer);
		}
		else
		{
			_observers.Add(observer);
		}
	}

	public bool Unobserve(ConfigObserver observer)
	{
		if (!observer.IsPreview)
		{
			return _observers.Remove(observer);
		}
		return _previewObservers.Remove(observer);
	}

	private object? _GetValueOrNull(object? argument)
	{
		if (!_Provider.GetValue<TValue>(Key, out TValue value, argument))
		{
			return null;
		}
		return value;
	}

	public ConfigEventArgs? TriggerEvent(ConfigEvent trigger, object? argument, bool bypassOldValue = false, bool fillNewValue = false)
	{
		return _TriggerEvent(trigger, argument, null, bypassOldValue, fillNewValue);
	}

	private ConfigEventArgs? _TriggerEvent(ConfigEvent trigger, object? argument, object? newValue, bool bypassOldValue = false, bool fillNewValue = false, ConfigEventArgs? e = null, bool? isPreview = null)
	{
		bool flag = false;
		IEnumerable<ConfigObserver> source;
		if (isPreview.HasValue)
		{
			IEnumerable<ConfigObserver> enumerable = ((isPreview == true) ? _previewObservers : _observers);
			source = enumerable;
		}
		else
		{
			source = _previewObservers.Concat(_observers);
		}
		foreach (ConfigObserver item in from observer in source
			let logic = (int)(observer.Event & trigger)
			where logic > 0
			select observer)
		{
			if ((object)e == null)
			{
				if (isPreview == false && !bypassOldValue)
				{
					bypassOldValue = true;
				}
				object obj = ((fillNewValue || !bypassOldValue) ? _GetValueOrNull(argument) : null);
				if ((newValue == null) & fillNewValue)
				{
					newValue = obj ?? ((object)DefaultValue);
				}
				e = new ConfigEventArgs(this, trigger, argument, bypassOldValue ? null : obj, newValue);
			}
			item.Handler(e);
			if (item.IsPreview)
			{
				if (e.NewValueReplacement != null)
				{
					flag = true;
				}
				if (e.Cancelled)
				{
					return e;
				}
			}
			else if (!flag && e.NewValueReplacement != null)
			{
				e.NewValueReplacement = null;
			}
		}
		if ((object)e != null && e.Cancelled)
		{
			e.Cancelled = false;
		}
		return e;
	}
}
public interface ConfigItem
{
	string Key { get; }

	ConfigSource Source { get; }

	Type Type { get; }

	object DefaultValueNoType { get; }

	bool EnableCache { get; set; }

	event ConfigEventHandler Changed
	{
		add
		{
			Observe(ConfigEvent.Changed, value);
		}
		remove
		{
			throw new NotSupportedException("Please use Observe() and Unobserve() to access advanced event management");
		}
	}

	void Observe(ConfigObserver observer);

	bool Unobserve(ConfigObserver observer);

	ConfigEventArgs? TriggerEvent(ConfigEvent trigger, object? argument, bool bypassOldValue = false, bool fillNewValue = false);

	ConfigObserver Observe(ConfigEvent trigger, ConfigEventHandler handler, bool isPreview = false)
	{
		ConfigObserver configObserver = new ConfigObserver(trigger, handler, isPreview);
		Observe(configObserver);
		return configObserver;
	}

	bool Reset(object? argument = null);

	bool IsDefault(object? argument = null);

	bool SetDefaultValue(object? argument = null, bool? forceNewValue = null);

	object GetValueNoType(object? argument = null);

	bool SetValueNoType(object value, object? argument = null);
}

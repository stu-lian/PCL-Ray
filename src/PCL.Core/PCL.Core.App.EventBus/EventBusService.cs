using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.IoC;

namespace PCL.Core.App.EventBus;

[LifecycleService(LifecycleState.BeforeLoading)]
[LifecycleScope("eventbus", "EventBus", true)]
public sealed class EventBusService : ILifecycleService
{
	private sealed class Subscription : IDisposable
	{
		private Action? _dispose;

		public Subscription(Action dispose)
		{
			_dispose = dispose ?? throw new ArgumentNullException("dispose");
		}

		public void Dispose()
		{
			Interlocked.Exchange(ref _dispose, null)?.Invoke();
		}
	}

	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)>>> _Channels = new ConcurrentDictionary<string, ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>>>();

	private static int _isStopping;

	public string Identifier => "eventbus";

	public string Name => "EventBus";

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

	[LifecycleStop]
	private static Task _StopAsync()
	{
		Interlocked.Exchange(ref _isStopping, 1);
		try
		{
			int count = _Channels.Count;
			int value = _Channels.Values.Sum((ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object> OwnerRef)>> c) => c.Values.Sum((ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object> OwnerRef)> h) => h.Count));
			_Channels.Clear();
			Context.Info($"EventBus stopping: cleared {count} channels and {value} handlers.");
			return Task.CompletedTask;
		}
		catch (Exception ex)
		{
			Context.Error($"Exception while stopping EventBus: {ex}");
			return Task.FromException(ex);
		}
	}

	public static Task PublishAsync<TEventData>(string channelName, TEventData data) where TEventData : EventDataBase
	{
		if (Volatile.Read(in _isStopping) != 0)
		{
			throw new InvalidOperationException("EventBus is stopping");
		}
		return _CallChannelAsync(channelName, data);
	}

	public static IDisposable Subscribe<TEventData>(string channel, IEventHandler<TEventData> handler, bool disposeOwnerOnUnsubscribe = false) where TEventData : EventDataBase
	{
		if (Volatile.Read(in _isStopping) != 0)
		{
			throw new InvalidOperationException("EventBus is stopping");
		}
		if (string.IsNullOrWhiteSpace(channel))
		{
			throw new ArgumentNullException("channel");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		if (!_Channels.TryGetValue(channel, out ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)>> dataHandler))
		{
			Context.Trace("Channel " + channel + " not found.");
			if (!AddChannel(channel))
			{
				throw new InvalidOperationException("Failed to create channel.");
			}
		}
		if (dataHandler == null)
		{
			dataHandler = _Channels[channel];
		}
		Type dataType = typeof(TEventData);
		ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)> handlers = dataHandler.GetOrAdd(dataType, (Type _) => new ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>());
		WeakReference<object> ownerRef = new WeakReference<object>(handler);
		Guid id = Guid.NewGuid();
		handlers.TryAdd(id, (Wrapper, ownerRef));
		return new Subscription(() =>
		{
			handlers.TryRemove(id, out (Func<EventDataBase, Task>, WeakReference<object>) _);
			if (handlers.IsEmpty)
			{
				dataHandler.TryRemove(dataType, out ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)> _);
			}
			if (disposeOwnerOnUnsubscribe && ownerRef.TryGetTarget(out object tgt) && tgt is IDisposable disposable && !dataHandler.Values.Any((ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object> OwnerRef)> dict) => dict.Values.Any(((Func<EventDataBase, Task> Handler, WeakReference<object> OwnerRef) e) => e.OwnerRef != null && e.OwnerRef.TryGetTarget(out var target) && target == tgt)))
			{
				try
				{
					disposable.Dispose();
				}
				catch (Exception value3)
				{
					Context.Error($"Exception disposing subscription owner: {value3}");
				}
			}
		});
		Task Wrapper(EventDataBase ev)
		{
			if (ownerRef.TryGetTarget(out object target) && target is IEventHandler<TEventData> eventHandler)
			{
				return eventHandler.HandleEventAsync((TEventData)ev);
			}
			return Task.CompletedTask;
		}
	}

	public static IDisposable Subscribe<TEventData>(string channel, Func<TEventData, Task> handler) where TEventData : EventDataBase
	{
		if (Volatile.Read(in _isStopping) != 0)
		{
			throw new InvalidOperationException("EventBus is stopping");
		}
		if (string.IsNullOrWhiteSpace(channel))
		{
			throw new ArgumentNullException("channel");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		if (!_Channels.TryGetValue(channel, out ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)>> dataHandler))
		{
			Context.Trace("Channel " + channel + " not found.");
			if (!AddChannel(channel))
			{
				throw new InvalidOperationException("Failed to create channel.");
			}
		}
		if (dataHandler == null)
		{
			dataHandler = _Channels[channel];
		}
		Type dataType = typeof(TEventData);
		ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)> handlers = dataHandler.GetOrAdd(dataType, (Type _) => new ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>());
		Guid id = Guid.NewGuid();
		handlers.TryAdd(id, (Wrapper, null));
		return new Subscription(() =>
		{
			handlers.TryRemove(id, out (Func<EventDataBase, Task>, WeakReference<object>) _);
			if (handlers.IsEmpty)
			{
				dataHandler.TryRemove(dataType, out ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)> _);
			}
		});
		Task Wrapper(EventDataBase ev)
		{
			return handler((TEventData)ev);
		}
	}

	public static bool AddChannel(string name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			return _Channels.TryAdd(name, new ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>>());
		}
		return false;
	}

	public static bool RemoveChannel(string name)
	{
		ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>> value;
		return _Channels.TryRemove(name, out value);
	}

	private static Task _CallChannelAsync<TEventData>(string channel, TEventData data) where TEventData : EventDataBase
	{
		if (!_Channels.TryGetValue(channel, out ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task>, WeakReference<object>)>> value))
		{
			Context.Error("Channel " + channel + " not found.");
			throw new InvalidOperationException("No channel found for the given channel identification.");
		}
		return _CallEventHandlerAsync(data, value);
	}

	private static Task _CallEventHandlerAsync<TEventData>(TEventData data, ConcurrentDictionary<Type, ConcurrentDictionary<Guid, (Func<EventDataBase, Task> Handler, WeakReference<object>? OwnerRef)>> dataHandlers) where TEventData : EventDataBase
	{
		Type type = data.GetType();
		List<Func<EventDataBase, Task>> list = new List<Func<EventDataBase, Task>>();
		foreach (var (type3, concurrentDictionary2) in dataHandlers)
		{
			if (!type3.IsAssignableFrom(type))
			{
				continue;
			}
			foreach (KeyValuePair<Guid, (Func<EventDataBase, Task>, WeakReference<object>)> item in concurrentDictionary2.ToImmutableArray())
			{
				Guid key = item.Key;
				(Func<EventDataBase, Task>, WeakReference<object>) value = item.Value;
				if (value.Item2 != null && !value.Item2.TryGetTarget(out var _))
				{
					concurrentDictionary2.TryRemove(key, out var _);
				}
				else
				{
					list.Add(value.Item1);
				}
			}
		}
		if (list.Count == 0)
		{
			Context.Trace("No handler found for event data type " + type.Name);
			return Task.CompletedTask;
		}
		return Task.WhenAll(((IEnumerable<Func<EventDataBase, Task>>)list).Select((Func<Func<EventDataBase, Task>, Task>)(async (Func<EventDataBase, Task> h) =>
		{
			try
			{
				await h(data).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception value3)
			{
				Context.Error($"Event handler threw an exception: {value3}");
			}
		})).ToImmutableArray());
	}

	public EventBusService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		await _StopAsync();
	}

	public async Task StartAsync()
	{
	}
}

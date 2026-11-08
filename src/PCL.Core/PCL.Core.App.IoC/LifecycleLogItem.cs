using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.App.IoC;

public readonly record struct LifecycleLogItem
{
	public ILifecycleService? Source { get; init; }

	public string Message { get; init; }

	public Exception? Exception { get; init; }

	public LogLevel Level { get; init; }

	public ActionLevel ActionLevel { get; init; }

	public DateTime Time { get; }

	public string ContextName { get; }

	public LifecycleLogItem(ILifecycleService? Source, string Message, Exception? Exception, LogLevel Level, ActionLevel ActionLevel)
	{
		this.Source = Source;
		this.Message = Message;
		this.Exception = Exception;
		this.Level = Level;
		this.ActionLevel = ActionLevel;
		Time = DateTime.Now;
		int? currentId = Task.CurrentId;
		object obj;
		if (currentId.HasValue)
		{
			int valueOrDefault = currentId.GetValueOrDefault();
			obj = $"TSK#{valueOrDefault}";
		}
		else
		{
			obj = null;
		}
		if (obj == null)
		{
			obj = Thread.CurrentThread.Name ?? $"#{Environment.CurrentManagedThreadId}";
		}
		ContextName = (string)obj;
	}

	public override string ToString()
	{
		string value = ((Source == null) ? "" : $" [{Source.Name}|{Source.Identifier}]");
		string text = $"[{Time:HH:mm:ss.fff}]{value}";
		if (Exception != null)
		{
			return $"{text} ({Message}) {Exception.GetType().FullName}: {Exception.Message}";
		}
		return text + " " + Message;
	}

	public string ComposeMessage()
	{
		string value = ((Source == null) ? "" : $" [{Source.Name}|{Source.Identifier}]");
		string text = $"[{Time:HH:mm:ss.fff}] [{Level.RealLevel().PrintName()}] [{ContextName}]{value} {Message}";
		if (Exception != null)
		{
			text += $"\n{Exception}";
		}
		return text;
	}

	[CompilerGenerated]
	public void Deconstruct(out ILifecycleService? Source, out string Message, out Exception? Exception, out LogLevel Level, out ActionLevel ActionLevel)
	{
		Source = this.Source;
		Message = this.Message;
		Exception = this.Exception;
		Level = this.Level;
		ActionLevel = this.ActionLevel;
	}
}

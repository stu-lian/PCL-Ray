using System;

namespace PCL.Core.App.IoC;

public record LifecycleServiceInfo
{
	private readonly ILifecycleService _service;

	public string Identifier => _service.Identifier;

	public string Name => _service.Name;

	public bool CanStartAsync => _service.SupportAsync;

	public LifecycleState StartState { get; }

	public DateTime StartTime { get; init; } = DateTime.Now;

	public string FullIdentifier => $"{StartState}/{Identifier}";

	public bool IsStopped { get; private set; }

	public void MarkAsStopped()
	{
		IsStopped = true;
	}

	public LifecycleServiceInfo(ILifecycleService service, LifecycleState startState)
	{
		_service = service;
		StartState = startState;
		StartTime = DateTime.Now;
	}
}

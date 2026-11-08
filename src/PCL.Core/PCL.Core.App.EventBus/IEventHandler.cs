using System;
using System.Threading.Tasks;

namespace PCL.Core.App.EventBus;

public interface IEventHandler<in TEventData> : IDisposable where TEventData : EventDataBase
{
	Task HandleEventAsync(TEventData eventData);
}

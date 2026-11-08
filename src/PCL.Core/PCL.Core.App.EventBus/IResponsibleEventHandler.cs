using System.Threading.Tasks;

namespace PCL.Core.App.EventBus;

public interface IResponsibleEventHandler<TResponse>
{
	Task<TResponse> HandleEventAsync(EventDataBase eventData);
}

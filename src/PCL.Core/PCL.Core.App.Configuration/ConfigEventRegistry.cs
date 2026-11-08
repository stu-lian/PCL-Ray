using System.Collections.Generic;

namespace PCL.Core.App.Configuration;

public class ConfigEventRegistry(IEnumerable<IConfigScope> scope, ConfigEventHandler handler, ConfigEvent trigger = ConfigEvent.Changed, bool isPreview = false)
{
	public IEnumerable<IConfigScope> Scopes => scope;

	public ConfigEvent Trigger => trigger;

	public ConfigEventHandler Handler => handler;

	public bool IsPreview => isPreview;

	public ConfigEventRegistry(IConfigScope scope, ConfigEventHandler handler, ConfigEvent trigger = ConfigEvent.Changed, bool isPreview = false)
		: this(new _003C_003Ez__ReadOnlySingleElementList<IConfigScope>(scope), handler, trigger, isPreview)
	{
	}

	public ConfigObserver ToObserver()
	{
		return new ConfigObserver(trigger, handler, isPreview);
	}
}

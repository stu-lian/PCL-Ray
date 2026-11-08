using System.Collections.Generic;

namespace PCL.Core.App.Configuration;

public interface IConfigScope
{
	IEnumerable<string> CheckScope(IReadOnlySet<string> keys);

	bool Reset(object? argument = null);

	bool IsDefault(object? argument = null);
}

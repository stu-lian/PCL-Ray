using System.Collections.Generic;

namespace PCL.Core.App.Configuration.Storage;

public interface IEnumerableKeyProvider
{
	IEnumerable<string> Keys { get; }
}

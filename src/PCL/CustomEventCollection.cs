using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;

namespace PCL;

[ContentProperty("Events")]
public class CustomEventCollection : IEnumerable<CustomEvent>, IEnumerable
{
	private readonly List<CustomEvent> _events = new List<CustomEvent>();

	public List<CustomEvent> Events => _events;

	public IEnumerator<CustomEvent> GetEnumerator()
	{
		return Events.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}

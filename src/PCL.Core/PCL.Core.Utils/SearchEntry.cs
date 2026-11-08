using System.Collections.Generic;

namespace PCL.Core.Utils;

public class SearchEntry<T>(T item, List<KeyValuePair<string, double>> searchSource)
{
	public T Item { get; set; } = item;

	public List<KeyValuePair<string, double>> SearchSource { get; set; } = searchSource;

	public double Similarity { get; set; }

	public bool AbsoluteRight { get; set; }
}

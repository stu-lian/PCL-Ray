using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace PCL.Core.App.Configuration.Storage;

public static class YamlNodeConverter
{
	public class EventStreamParserAdapter(IEnumerable<ParsingEvent> events) : IParser
	{
		private readonly IEnumerator<ParsingEvent> _enumerator = events.GetEnumerator();

		public ParsingEvent Current => _enumerator.Current;

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}
	}

	public static IParser GetParser(this IEnumerable<ParsingEvent> eventStream)
	{
		return (IParser)(object)new EventStreamParserAdapter(eventStream);
	}

	public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlStream stream)
	{
		yield return (ParsingEvent)new StreamStart();
		foreach (YamlDocument document in stream.Documents)
		{
			foreach (ParsingEvent item in document.ConvertToEventStream())
			{
				yield return item;
			}
		}
		yield return (ParsingEvent)new StreamEnd();
	}

	public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlDocument document)
	{
		yield return (ParsingEvent)new DocumentStart();
		foreach (ParsingEvent item in document.RootNode.ConvertToEventStream())
		{
			yield return item;
		}
		yield return (ParsingEvent)new DocumentEnd(false);
	}

	public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlNode node)
	{
		YamlScalarNode val = (YamlScalarNode)(object)((node is YamlScalarNode) ? node : null);
		if (val != null)
		{
			return _ConvertToEventStream(val);
		}
		YamlSequenceNode val2 = (YamlSequenceNode)(object)((node is YamlSequenceNode) ? node : null);
		if (val2 != null)
		{
			return _ConvertToEventStream(val2);
		}
		YamlMappingNode val3 = (YamlMappingNode)(object)((node is YamlMappingNode) ? node : null);
		if (val3 != null)
		{
			return _ConvertToEventStream(val3);
		}
		throw new NotSupportedException("Unsupported node type: " + ((object)node).GetType().Name);
	}

	private static IEnumerable<ParsingEvent> _ConvertToEventStream(YamlScalarNode scalar)
	{
		yield return (ParsingEvent)new Scalar(((YamlNode)scalar).Anchor, ((YamlNode)scalar).Tag, scalar.Value, scalar.Style, false, false);
	}

	private static IEnumerable<ParsingEvent> _ConvertToEventStream(YamlSequenceNode sequence)
	{
		yield return (ParsingEvent)new SequenceStart(((YamlNode)sequence).Anchor, ((YamlNode)sequence).Tag, false, sequence.Style);
		foreach (YamlNode child in sequence.Children)
		{
			foreach (ParsingEvent item in child.ConvertToEventStream())
			{
				yield return item;
			}
		}
		yield return (ParsingEvent)new SequenceEnd();
	}

	private static IEnumerable<ParsingEvent> _ConvertToEventStream(YamlMappingNode mapping)
	{
		yield return (ParsingEvent)new MappingStart(((YamlNode)mapping).Anchor, ((YamlNode)mapping).Tag, false, mapping.Style);
		foreach (KeyValuePair<YamlNode, YamlNode> pair in (IEnumerable<KeyValuePair<YamlNode, YamlNode>>)mapping.Children)
		{
			foreach (ParsingEvent item in pair.Key.ConvertToEventStream())
			{
				yield return item;
			}
			foreach (ParsingEvent item2 in pair.Value.ConvertToEventStream())
			{
				yield return item2;
			}
		}
		yield return (ParsingEvent)new MappingEnd();
	}
}

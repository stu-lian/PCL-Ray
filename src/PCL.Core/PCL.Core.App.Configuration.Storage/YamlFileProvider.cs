using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PCL.Core.Logging;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace PCL.Core.App.Configuration.Storage;

public class YamlFileProvider : CommonFileProvider, IEnumerableKeyProvider
{
	private readonly YamlMappingNode _rootNode;

	private static readonly IDeserializer _Deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithEnforceRequiredMembers().Build();

	private static readonly ISerializer _Serializer = new SerializerBuilder().DisableAliases().Build();

	public IEnumerable<string> Keys => ((IEnumerable<KeyValuePair<YamlNode, YamlNode>>)_rootNode).Select((KeyValuePair<YamlNode, YamlNode> pair) => ((object)pair.Key).ToString());

	private static YamlMappingNode? _LoadFile(string path)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected Obj, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected Obj, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (!File.Exists(path))
		{
			return null;
		}
		using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		try
		{
			YamlStream val = new YamlStream();
			val.Load((TextReader)streamReader);
			if (val.Documents.Count == 0)
			{
				return new YamlMappingNode();
			}
			YamlNode rootNode = val.Documents[0].RootNode;
			return (YamlMappingNode?)(object)(((rootNode is YamlMappingNode) ? rootNode : null) ?? throw new ConfigFileInitException(path, $"Invalid root node type: {rootNode.NodeType}"));
		}
		catch (Exception ex)
		{
			if (ex is ConfigFileInitException)
			{
				throw;
			}
			throw new ConfigFileInitException(path, "Failed to load YAML content", ex);
		}
	}

	public YamlFileProvider(string path)
		: base(path)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected Obj, but got Unknown
		YamlMappingNode val = _LoadFile(path);
		if (val != null)
		{
			_rootNode = val;
			return;
		}
		try
		{
			string path2 = Path.Combine(Basics.GetParentPath(path), Path.GetFileNameWithoutExtension(path) + ".json");
			if (File.Exists(path2))
			{
				using (FileStream jsonInput = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using FileStream yamlOutput = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
					JsonToYamlConverter.Convert(jsonInput, yamlOutput);
					_rootNode = _LoadFile(path);
					return;
				}
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "转换失败，已忽略");
		}
		_rootNode = new YamlMappingNode();
	}

	public override T Get<T>(string key)
	{
		YamlNode val = ((IDictionary<YamlNode, YamlNode>)_rootNode.Children)[(YamlNode)key];
		IParser parser = val.ConvertToEventStream().GetParser();
		try
		{
			return _Deserializer.Deserialize<T>(parser);
		}
		catch (Exception)
		{
			Type? typeFromHandle = typeof(T);
			string text = ((object)val).ToString();
			T val2;
			if (typeFromHandle == typeof(bool))
			{
				string text2 = text.ToLowerInvariant();
				bool flag = ((text2 == "true" || text2 == "1") ? true : false);
				val2 = (T)(object)flag;
			}
			else
			{
				val2 = (T)(object)text;
			}
			T val3 = val2;
			Set(key, val3);
			return val3;
		}
	}

	public override void Set<T>(string key, T value)
	{
		YamlNodeEmitter yamlNodeEmitter = new YamlNodeEmitter();
		_Serializer.Serialize((IEmitter)(object)yamlNodeEmitter, (object)value);
		((IDictionary<YamlNode, YamlNode>)_rootNode.Children)[(YamlNode)key] = yamlNodeEmitter.SingleRootNode;
	}

	public override bool Exists(string key)
	{
		return ((IDictionary<YamlNode, YamlNode>)_rootNode.Children).ContainsKey((YamlNode)key);
	}

	public override void Remove(string key)
	{
		((IDictionary<YamlNode, YamlNode>)_rootNode.Children).Remove((YamlNode)key);
	}

	protected override void WriteToStream(Stream stream)
	{
		StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
		_Serializer.Serialize((TextWriter)streamWriter, (object)_rootNode);
		streamWriter.Flush();
	}

	static YamlFileProvider()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
	}
}

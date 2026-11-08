using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using PCL.Core.Utils;

namespace PCL.Core.App.Configuration.Storage;

public class JsonFileProvider : CommonFileProvider, IEnumerableKeyProvider
{
	private readonly JsonObject _rootElement;

	private static readonly JsonDocumentOptions _DocumentOptions = JsonCompat.DocumentOptions;

	private static readonly JsonSerializerOptions _SerializerOptions = new JsonSerializerOptions(JsonCompat.SerializerOptions)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		AllowOutOfOrderMetadataProperties = true
	};

	private static readonly JsonWriterOptions _WriterOptions = new JsonWriterOptions
	{
		Indented = true
	};

	public IEnumerable<string> Keys => _rootElement.Select((KeyValuePair<string, JsonNode> pair) => pair.Key);

	public JsonFileProvider(string path)
		: base(path)
	{
		try
		{
			if (File.Exists(path))
			{
				using (FileStream utf8Json = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					JsonNode jsonNode = JsonNode.Parse(utf8Json, JsonCompat.NodeOptions, _DocumentOptions);
					if (!(jsonNode is JsonObject rootElement))
					{
						throw new ConfigFileInitException(path, "Invalid root element type: " + (jsonNode?.GetValueKind().ToString() ?? "Empty"));
					}
					_rootElement = rootElement;
					return;
				}
			}
			using FileStream utf8Json2 = new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
			_rootElement = new JsonObject();
			JsonSerializer.Serialize(utf8Json2, _rootElement, _SerializerOptions);
		}
		catch (Exception ex)
		{
			if (ex is ConfigFileInitException)
			{
				throw;
			}
			throw new ConfigFileInitException(path, "Failed to read JSON file", ex);
		}
	}

	public override T Get<T>(string key)
	{
		JsonNode jsonNode = _rootElement[key];
		if (jsonNode == null)
		{
			throw new KeyNotFoundException("Not found: '" + key + "'");
		}
		try
		{
			T val = jsonNode.Deserialize<T>(_SerializerOptions);
			if (val == null)
			{
				throw GetNullException();
			}
			return val;
		}
		catch (JsonException)
		{
			Type typeFromHandle = typeof(T);
			T val2;
			if (typeFromHandle == typeof(string))
			{
				val2 = (T)(object)jsonNode.ToString();
			}
			else
			{
				string text = jsonNode.Deserialize<string>(_SerializerOptions);
				if (typeFromHandle == typeof(bool))
				{
					string text2 = text.ToLowerInvariant();
					bool flag = ((text2 == "true" || text2 == "1") ? true : false);
					val2 = (T)(object)flag;
				}
				else
				{
					T val3 = JsonSerializer.Deserialize<T>(text, _SerializerOptions);
					if (val3 == null)
					{
						throw GetNullException();
					}
					val2 = val3;
				}
			}
			Set(key, val2);
			return val2;
		}
		Exception GetNullException()
		{
			return new InvalidDataException("Deserialized value is null: '" + key + "'");
		}
	}

	public override void Set<T>(string key, T value)
	{
		_rootElement[key] = JsonSerializer.SerializeToNode(value, _SerializerOptions);
	}

	public override bool Exists(string key)
	{
		return _rootElement.ContainsKey(key);
	}

	public override void Remove(string key)
	{
		_rootElement.Remove(key);
	}

	protected override void WriteToStream(Stream stream)
	{
		Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(stream, _WriterOptions);
		_rootElement.WriteTo(utf8JsonWriter, _SerializerOptions);
		utf8JsonWriter.Flush();
	}
}

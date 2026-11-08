using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Utils;

public sealed class ExpandoObjectConverter : JsonConverter<ExpandoObject>
{
	public static readonly ExpandoObjectConverter Default = new ExpandoObjectConverter();

	public override ExpandoObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
		return Read(jsonDocument.RootElement, options);
	}

	public static ExpandoObject Read(JsonElement element, JsonSerializerOptions options)
	{
		ExpandoObject expandoObject = new ExpandoObject();
		IDictionary<string, object> dictionary = expandoObject;
		foreach (JsonProperty item in element.EnumerateObject())
		{
			dictionary.Add(item.Name, _ConvertElement(item.Value, options));
		}
		return expandoObject;
	}

	private static object _ConvertElement(JsonElement element, JsonSerializerOptions options)
	{
		switch (element.ValueKind)
		{
		case JsonValueKind.Object:
			return Read(element, options);
		case JsonValueKind.Array:
			return (from item in element.EnumerateArray()
				select _ConvertElement(item, options)).ToList();
		case JsonValueKind.String:
			return element.GetString();
		case JsonValueKind.Number:
		{
			if (element.TryGetInt64(out var value))
			{
				return value;
			}
			if (element.TryGetDouble(out var value2))
			{
				return value2;
			}
			break;
		}
		case JsonValueKind.True:
			return true;
		case JsonValueKind.False:
			return false;
		case JsonValueKind.Null:
			return null;
		case JsonValueKind.Undefined:
			return null;
		}
		return element.GetRawText();
	}

	public override void Write(Utf8JsonWriter writer, ExpandoObject value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, object> item in (IEnumerable<KeyValuePair<string, object>>)value)
		{
			writer.WritePropertyName(item.Key);
			JsonSerializer.Serialize(writer, item.Value, options);
		}
		writer.WriteEndObject();
	}
}

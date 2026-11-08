using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PCL.Core.Utils;

public static class JsonCompat
{
	private sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
	{
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				return NormalizeDateTime(reader.GetDateTime());
			}
			if (!TryParseDateTime(reader.GetString(), out var dateTime))
			{
				return NormalizeDateTime(reader.GetDateTime());
			}
			return dateTime;
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value);
		}
	}

	private sealed class FlexibleBoolConverter : JsonConverter<bool>
	{
		public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			switch (reader.TokenType)
			{
			case JsonTokenType.True:
				return true;
			case JsonTokenType.False:
				return false;
			case JsonTokenType.String:
			{
				if (bool.TryParse(reader.GetString(), out var result))
				{
					return result;
				}
				if (int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
				{
					return result2 != 0;
				}
				break;
			}
			case JsonTokenType.Number:
			{
				if (reader.TryGetInt64(out var value))
				{
					return value != 0;
				}
				break;
			}
			}
			throw new JsonException($"Can not convert JSON token {reader.TokenType} to Boolean.");
		}

		public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
		{
			writer.WriteBooleanValue(value);
		}
	}

	private sealed class FlexibleStringConverter : JsonConverter<string>
	{
		public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return reader.TokenType switch
			{
				JsonTokenType.Null => null, 
				JsonTokenType.String => reader.GetString(), 
				JsonTokenType.Number => _ReadRawJsonValue(ref reader), 
				JsonTokenType.True => bool.TrueString, 
				JsonTokenType.False => bool.FalseString, 
				_ => _ReadRawJsonValue(ref reader), 
			};
		}

		private static string _ReadRawJsonValue(ref Utf8JsonReader reader)
		{
			using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
			return jsonDocument.RootElement.GetRawText();
		}

		public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value);
		}
	}

	public static readonly JsonNodeOptions NodeOptions = new JsonNodeOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	public static JsonSerializerOptions SerializerOptions { get; } = _CreateSerializerOptions();

	private static JsonSerializerOptions _CreateSerializerOptions()
	{
		JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
		jsonSerializerOptions.PropertyNameCaseInsensitive = true;
		jsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
		jsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
		jsonSerializerOptions.AllowTrailingCommas = true;
		jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
		jsonSerializerOptions.Converters.Add(new FlexibleDateTimeConverter());
		jsonSerializerOptions.Converters.Add(new FlexibleBoolConverter());
		jsonSerializerOptions.Converters.Add(new FlexibleStringConverter());
		jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		jsonSerializerOptions.MakeReadOnly(populateMissingResolver: true);
		return jsonSerializerOptions;
	}

	public static JsonNode ParseNode(string text)
	{
		return JsonNode.Parse(text, NodeOptions, DocumentOptions);
	}

	public static T? ToObject<T>(this JsonNode? node)
	{
		if (node != null)
		{
			return node.Deserialize<T>(SerializerOptions);
		}
		return default;
	}

	public static JsonArray FromObject<T>(IEnumerable<T> items)
	{
		JsonArray jsonArray = new JsonArray();
		foreach (T item in items)
		{
			jsonArray.Add(JsonSerializer.SerializeToNode(item, SerializerOptions));
		}
		return jsonArray;
	}

	public static bool TryGetDateTime(JsonNode? node, out DateTime dateTime)
	{
		dateTime = default;
		if (node != null)
		{
			if (node is JsonValue jsonValue)
			{
				if (jsonValue.TryGetValue<DateTime>(out var value))
				{
					dateTime = NormalizeDateTime(value);
					return true;
				}
				if (jsonValue.TryGetValue<string>(out string value2) && TryParseDateTime(value2, out dateTime))
				{
					return true;
				}
			}
			try
			{
				dateTime = NormalizeDateTime(node.Deserialize<DateTime>(SerializerOptions));
				return true;
			}
			catch (JsonException)
			{
				return false;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			catch (NotSupportedException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool TryParseDateTime(string? value, out DateTime dateTime)
	{
		dateTime = default;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		bool flag = false;
		Match match = RegexPatterns.Iso8601EndOfDay.Match(value);
		if (match.Success)
		{
			value = value.Remove(match.Index + 1, 2).Insert(match.Index + 1, "00");
			flag = true;
		}
		if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			if (flag)
			{
				result = result.AddDays(1.0);
			}
			dateTime = result.LocalDateTime;
			return true;
		}
		if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result2))
		{
			return false;
		}
		dateTime = NormalizeDateTime(flag ? result2.AddDays(1.0) : result2);
		return true;
	}

	public static DateTime NormalizeDateTime(DateTime dateTime)
	{
		if (dateTime.Kind != DateTimeKind.Utc)
		{
			return dateTime;
		}
		return dateTime.ToLocalTime();
	}

	public static void Merge(this JsonObject target, JsonNode? source)
	{
		if (!(source is JsonObject source2))
		{
			return;
		}
		KeyValuePair<string, JsonNode>[] array = source2.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, JsonNode> keyValuePair = array[i];
			JsonNode jsonNode = target[keyValuePair.Key];
			if (!(jsonNode is JsonObject target2))
			{
				if (jsonNode is JsonArray target3 && keyValuePair.Value is JsonArray source3)
				{
					target3.Merge(source3);
					continue;
				}
			}
			else if (keyValuePair.Value is JsonObject source4)
			{
				target2.Merge(source4);
				continue;
			}
			target[keyValuePair.Key] = keyValuePair.Value?.DeepClone();
		}
	}

	public static void Merge(this JsonArray target, JsonNode? source)
	{
		if (!(source is JsonArray jsonArray))
		{
			return;
		}
		foreach (JsonNode item in jsonArray)
		{
			target.Add(item?.DeepClone());
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.App.Cli;

public sealed class CommandLineJsonConverter : JsonConverter<CommandLine>
{
	public override CommandLine? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected object for CommandLine.");
		}
		string text = null;
		CommandLine subcommand = null;
		Dictionary<string, CommandArgument> dictionary = new Dictionary<string, CommandArgument>();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected property name.");
			}
			string text2 = reader.GetString();
			if (!reader.Read())
			{
				throw new JsonException("Unexpected end of json.");
			}
			switch (text2)
			{
			case "cmd":
				text = reader.GetString() ?? throw new JsonException("cmd cannot be null.");
				break;
			case "sub":
				subcommand = JsonSerializer.Deserialize<CommandLine>(ref reader, options);
				break;
			case "args":
				_ReadArguments(ref reader, dictionary);
				break;
			default:
				reader.Skip();
				break;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			throw new JsonException("Missing required property: cmd.");
		}
		return new CommandLine
		{
			CommandText = text,
			Arguments = dictionary.AsReadOnly(),
			Subcommand = subcommand
		};
	}

	public override void Write(Utf8JsonWriter writer, CommandLine value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("cmd", value.CommandText);
		if (value.Arguments.Count > 0)
		{
			writer.WritePropertyName("args");
			writer.WriteStartArray();
			foreach (CommandArgument value2 in value.Arguments.Values)
			{
				writer.WriteStartObject();
				writer.WriteString("k", value2.Key);
				writer.WriteNumber("t", (int)value2.ValueKind);
				writer.WriteString("v", value2.ValueText);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
		if (value.Subcommand != null)
		{
			writer.WritePropertyName("sub");
			JsonSerializer.Serialize(writer, value.Subcommand, options);
		}
		writer.WriteEndObject();
	}

	private static void _ReadArguments(ref Utf8JsonReader reader, Dictionary<string, CommandArgument> target)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException("args must be an array.");
		}
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
			{
				return;
			}
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException("Argument item must be an object.");
			}
			string text = null;
			ArgumentValueKind? argumentValueKind = null;
			string text2 = null;
			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType != JsonTokenType.PropertyName)
				{
					throw new JsonException("Expected argument property name.");
				}
				string text3 = reader.GetString();
				if (!reader.Read())
				{
					throw new JsonException("Unexpected end of json.");
				}
				switch (text3)
				{
				case "k":
					text = reader.GetString();
					break;
				case "t":
					argumentValueKind = (ArgumentValueKind)reader.GetInt32();
					break;
				case "v":
					text2 = reader.GetString() ?? string.Empty;
					break;
				default:
					reader.Skip();
					break;
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				throw new JsonException("Argument missing key(k).");
			}
			if (!argumentValueKind.HasValue)
			{
				throw new JsonException("Argument missing type(t).");
			}
			if (text2 == null)
			{
				text2 = string.Empty;
			}
			CommandArgument commandArgument = argumentValueKind.Value switch
			{
				ArgumentValueKind.Bool => (CommandArgument)new BoolArgument
				{
					Key = text,
					ValueText = text2
				}, 
				ArgumentValueKind.Decimal => new DecimalArgument
				{
					Key = text,
					ValueText = text2
				}, 
				ArgumentValueKind.Text => new TextArgument
				{
					Key = text,
					ValueText = text2
				}, 
				_ => throw new JsonException($"Unsupported argument type: {(int)argumentValueKind.Value}"), 
			};
			target[commandArgument.Key] = commandArgument;
		}
		throw new JsonException("args array is not closed.");
	}
}

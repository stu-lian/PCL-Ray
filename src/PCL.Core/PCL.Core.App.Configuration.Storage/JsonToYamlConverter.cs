using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.Utils;
using YamlDotNet.Serialization;

namespace PCL.Core.App.Configuration.Storage;

public static class JsonToYamlConverter
{
	public static void Convert(Stream jsonInput, Stream yamlOutput, bool leaveOpen = false)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		ArgumentNullException.ThrowIfNull(jsonInput, "jsonInput");
		ArgumentNullException.ThrowIfNull(yamlOutput, "yamlOutput");
		if (!jsonInput.CanRead)
		{
			throw new ArgumentException("must be readable", "jsonInput");
		}
		if (!yamlOutput.CanWrite)
		{
			throw new ArgumentException("must be writable", "yamlOutput");
		}
		using JsonDocument jsonDocument = JsonDocument.Parse(jsonInput, JsonCompat.DocumentOptions);
		object obj = _ConvertElement(jsonDocument.RootElement);
		ISerializer val = new SerializerBuilder().Build();
		using StreamWriter streamWriter = new StreamWriter(yamlOutput, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 8192, leaveOpen);
		val.Serialize((TextWriter)streamWriter, obj);
		streamWriter.Flush();
	}

	public static async Task ConvertAsync(Stream jsonInput, Stream yamlOutput, bool leaveOpen = false)
	{
		ArgumentNullException.ThrowIfNull(jsonInput, "jsonInput");
		ArgumentNullException.ThrowIfNull(yamlOutput, "yamlOutput");
		if (!jsonInput.CanRead)
		{
			throw new ArgumentException("jsonInput must be readable", "jsonInput");
		}
		if (!yamlOutput.CanWrite)
		{
			throw new ArgumentException("yamlOutput must be writable", "yamlOutput");
		}
		using JsonDocument doc = await JsonDocument.ParseAsync(jsonInput, JsonCompat.DocumentOptions).ConfigureAwait(continueOnCapturedContext: false);
		object obj = _ConvertElement(doc.RootElement);
		ISerializer val = new SerializerBuilder().Build();
		await using StreamWriter streamWriter = new StreamWriter(yamlOutput, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 8192, leaveOpen);
		val.Serialize((TextWriter)streamWriter, obj);
		await streamWriter.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	private static object? _ConvertElement(JsonElement element)
	{
		switch (element.ValueKind)
		{
		case JsonValueKind.Object:
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
			{
				foreach (JsonProperty item in element.EnumerateObject())
				{
					dictionary[item.Name] = _ConvertElement(item.Value);
				}
				return dictionary;
			}
		}
		case JsonValueKind.Array:
			return element.EnumerateArray().Select(_ConvertElement).ToList();
		case JsonValueKind.String:
			return element.GetString();
		case JsonValueKind.Number:
		{
			if (element.TryGetInt64(out var value))
			{
				return value;
			}
			string rawText = element.GetRawText();
			if (decimal.TryParse(rawText, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}
			if (element.TryGetDouble(out var value2))
			{
				return value2;
			}
			return rawText;
		}
		case JsonValueKind.True:
			return true;
		case JsonValueKind.False:
			return false;
		default:
			return null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App.Configuration.Storage;

public class CatIniFileProvider : CommonFileProvider, IEnumerableKeyProvider
{
	private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

	public IEnumerable<string> Keys => _dict.Keys;

	public CatIniFileProvider(string path)
		: base(path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		while (true)
		{
			string text = streamReader.ReadLine();
			if (text != null)
			{
				if (!text.IsNullOrWhiteSpace())
				{
					string[] array = text.Split(':', 2);
					_dict[array[0]] = array[1];
				}
				continue;
			}
			break;
		}
	}

	public override T Get<T>(string key)
	{
		if (!_dict.TryGetValue(key, out string value))
		{
			throw new KeyNotFoundException("Not found: '" + key);
		}
		T val = value.Convert<T>();
		if (val == null)
		{
			throw new NullReferenceException();
		}
		return val;
	}

	public override void Set<T>(string key, T value)
	{
		_dict[key] = value.ConvertToString() ?? throw new NullReferenceException();
	}

	public override bool Exists(string key)
	{
		return _dict.ContainsKey(key);
	}

	public override void Remove(string key)
	{
		_dict.Remove(key);
	}

	protected override void WriteToStream(Stream stream)
	{
		StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
		foreach (var (input, input2) in _dict)
		{
			string text3 = input.ReplaceLineBreak();
			string text4 = input2.ReplaceLineBreak();
			streamWriter.WriteLine(text3 + ":" + text4);
		}
		streamWriter.Flush();
	}
}

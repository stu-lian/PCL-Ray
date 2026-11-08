using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Resources;

namespace PCL.Core.UI.Controls.SvgIcon;

public static class SvgIconLoader
{
	private readonly record struct SvgIconKey(string Pack, string Name)
	{
		public static SvgIconKey? TryParse(string? icon, string defaultPack)
		{
			if (string.IsNullOrWhiteSpace(icon))
			{
				return null;
			}
			string text = icon.Trim().Replace('\\', '/');
			if (text.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				text = text2.Substring(0, text2.Length - 4);
			}
			text = text.Trim('/');
			if (text.Length == 0 || text.Contains("..", StringComparison.Ordinal))
			{
				return null;
			}
			string[] array = text.Split('/', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			string text3 = ((array.Length == 2) ? array[0] : defaultPack);
			string text4 = ((array.Length == 2) ? array[1] : array[0]);
			if (!_IsSafeResourcePath(text3) || !_IsSafeResourcePath(text4))
			{
				return null;
			}
			return new SvgIconKey(text3, text4);
		}

		public override string ToString()
		{
			return Pack + "/" + Name;
		}

		private static bool _IsSafeResourcePath(string value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.StartsWith('/') || value.EndsWith('/'))
			{
				return false;
			}
			return value.Split('/', StringSplitOptions.RemoveEmptyEntries).All(_IsSafeResourceSegment);
		}

		private static bool _IsSafeResourceSegment(string value)
		{
			bool flag = string.IsNullOrWhiteSpace(value);
			if (!flag)
			{
				bool flag2 = ((value == "." || value == "..") ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return false;
			}
			return value.All((char ch) =>
			{
				bool flag3 = char.IsLetterOrDigit(ch);
				if (!flag3)
				{
					bool flag4 = ((ch == '-' || ch == '.' || ch == '_') ? true : false);
					flag3 = flag4;
				}
				return flag3;
			});
		}
	}

	public const string DefaultIconPack = "default";

	private static readonly string _AssemblyName = typeof(SvgIconLoader).Assembly.GetName().Name ?? "PCL.Core";

	private static readonly ConcurrentDictionary<string, Lazy<SvgIconModel?>> _Cache = new ConcurrentDictionary<string, Lazy<SvgIconModel>>(StringComparer.OrdinalIgnoreCase);

	internal static SvgIconModel? Load(string? icon, string? defaultPack = null)
	{
		SvgIconKey? key = SvgIconKey.TryParse(icon, defaultPack ?? "default");
		if (!key.HasValue)
		{
			if (!string.IsNullOrWhiteSpace(icon))
			{
				_LogDebug("无效的 SVG 图标标识：" + icon);
			}
			return null;
		}
		string key2 = key.Value.ToString();
		return _Cache.GetOrAdd(key2, (string _) => new Lazy<SvgIconModel>(() => _LoadCore(key.Value))).Value;
	}

	public static void ClearCache()
	{
		_Cache.Clear();
	}

	private static SvgIconModel? _LoadCore(SvgIconKey key)
	{
		try
		{
			Uri uri = new Uri($"pack://application:,,,/{_AssemblyName};component/UI/Assets/IconPacks/{key.Pack}/{key.Name}.svg", UriKind.Absolute);
			StreamResourceInfo resourceStream = Application.GetResourceStream(uri);
			if (resourceStream == null)
			{
				_LogDebug($"缺少 SVG 图标资源：{key} ({uri})");
				return null;
			}
			using Stream stream = resourceStream.Stream;
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			return SvgIconParser.Parse(streamReader.ReadToEnd());
		}
		catch (Exception ex)
		{
			_LogDebug($"加载 SVG 图标失败：{key}", ex);
			return null;
		}
	}

	private static void _LogDebug(string message, Exception? ex = null)
	{
	}
}

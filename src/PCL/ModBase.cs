using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xaml;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.Hash;
using PCL.Core.Utils.OS;

namespace PCL;

public static class ModBase
{
	public class MyColor
	{
		public double a = 255.0;

		public double b;

		public double g;

		public double r;

		public MyColor()
		{
		}

		public MyColor(System.Windows.Media.Color col)
		{
			a = (int)col.A;
			r = (int)col.R;
			g = (int)col.G;
			b = (int)col.B;
		}

		public MyColor(string hexString)
		{
			System.Windows.Media.Color color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexString);
			a = (int)color.A;
			r = (int)color.R;
			g = (int)color.G;
			b = (int)color.B;
		}

		public MyColor(double newA, MyColor col)
		{
			a = newA;
			r = col.r;
			g = col.g;
			b = col.b;
		}

		public MyColor(double newR, double newG, double newB)
		{
			a = 255.0;
			r = newR;
			g = newG;
			b = newB;
		}

		public MyColor(double newA, double newR, double newG, double newB)
		{
			a = newA;
			r = newR;
			g = newG;
			b = newB;
		}

		public MyColor(System.Windows.Media.Brush brush)
		{
			System.Windows.Media.Color color = ((SolidColorBrush)brush).Color;
			a = (int)color.A;
			r = (int)color.R;
			g = (int)color.G;
			b = (int)color.B;
		}

		public MyColor(SolidColorBrush brush)
		{
			System.Windows.Media.Color color = brush.Color;
			a = (int)color.A;
			r = (int)color.R;
			g = (int)color.G;
			b = (int)color.B;
		}

		public MyColor(object obj)
		{
			if (obj == null)
			{
				a = 255.0;
				r = 255.0;
				g = 255.0;
				b = 255.0;
			}
			else if (obj is SolidColorBrush)
			{
				System.Windows.Media.Color color = ((SolidColorBrush)obj).Color;
				a = (int)color.A;
				r = (int)color.R;
				g = (int)color.G;
				b = (int)color.B;
			}
			else
			{
				a = Convert.ToDouble(((dynamic)obj).A);
				r = Convert.ToDouble(((dynamic)obj).R);
				g = Convert.ToDouble(((dynamic)obj).G);
				b = Convert.ToDouble(((dynamic)obj).B);
			}
		}

		public static implicit operator MyColor(string str)
		{
			return new MyColor(str);
		}

		public static implicit operator MyColor(System.Windows.Media.Color col)
		{
			return new MyColor(col);
		}

		public static implicit operator System.Windows.Media.Color(MyColor conv)
		{
			return System.Windows.Media.Color.FromArgb(MathByte(conv.a), MathByte(conv.r), MathByte(conv.g), MathByte(conv.b));
		}

		public static implicit operator System.Drawing.Color(MyColor conv)
		{
			return System.Drawing.Color.FromArgb(MathByte(conv.a), MathByte(conv.r), MathByte(conv.g), MathByte(conv.b));
		}

		public static implicit operator MyColor(SolidColorBrush bru)
		{
			return new MyColor(bru.Color);
		}

		public static implicit operator SolidColorBrush(MyColor conv)
		{
			return new SolidColorBrush(System.Windows.Media.Color.FromArgb(MathByte(conv.a), MathByte(conv.r), MathByte(conv.g), MathByte(conv.b)));
		}

		public static implicit operator MyColor(System.Windows.Media.Brush bru)
		{
			return new MyColor(bru);
		}

		public static implicit operator System.Windows.Media.Brush(MyColor conv)
		{
			return new SolidColorBrush(System.Windows.Media.Color.FromArgb(MathByte(conv.a), MathByte(conv.r), MathByte(conv.g), MathByte(conv.b)));
		}

		public static MyColor operator +(MyColor a, MyColor b)
		{
			return new MyColor
			{
				a = a.a + b.a,
				b = a.b + b.b,
				g = a.g + b.g,
				r = a.r + b.r
			};
		}

		public static MyColor operator -(MyColor a, MyColor b)
		{
			return new MyColor
			{
				a = a.a - b.a,
				b = a.b - b.b,
				g = a.g - b.g,
				r = a.r - b.r
			};
		}

		public static MyColor operator *(MyColor a, double b)
		{
			return new MyColor
			{
				a = a.a * b,
				b = a.b * b,
				g = a.g * b,
				r = a.r * b
			};
		}

		public static MyColor operator /(MyColor a, double b)
		{
			return new MyColor
			{
				a = a.a / b,
				b = a.b / b,
				g = a.g / b,
				r = a.r / b
			};
		}

		public static bool operator ==(MyColor a, MyColor b)
		{
			if ((object)a == null && (object)b == null)
			{
				return true;
			}
			if ((object)a == null || (object)b == null)
			{
				return false;
			}
			if (a.a == b.a && a.r == b.r && a.g == b.g)
			{
				return a.b == b.b;
			}
			return false;
		}

		public static bool operator !=(MyColor a, MyColor b)
		{
			if ((object)a == null && (object)b == null)
			{
				return false;
			}
			if ((object)a == null || (object)b == null)
			{
				return true;
			}
			if (a.a == b.a && a.r == b.r && a.g == b.g)
			{
				return a.b != b.b;
			}
			return true;
		}

		public double Hue(double v1, double v2, double vH)
		{
			if (vH < 0.0)
			{
				vH++;
			}
			if (vH > 1.0)
			{
				vH--;
			}
			if (vH < 0.16667)
			{
				return v1 + (v2 - v1) * 6.0 * vH;
			}
			if (vH < 0.5)
			{
				return v2;
			}
			if (vH < 0.66667)
			{
				return v1 + (v2 - v1) * (4.0 - vH * 6.0);
			}
			return v1;
		}

		public MyColor FromHSL(double sH, double sS, double sL)
		{
			if (sS == 0.0)
			{
				r = sL * 2.55;
				g = r;
				b = r;
			}
			else
			{
				double num = sH / 360.0;
				double num2 = sS / 100.0;
				double num3 = sL / 100.0;
				num2 = ((num3 < 0.5) ? (num2 * num3 + num3) : (num2 * (1.0 - num3) + num3));
				num3 = 2.0 * num3 - num2;
				r = 255.0 * Hue(num3, num2, num + 1.0 / 3.0);
				g = 255.0 * Hue(num3, num2, num);
				b = 255.0 * Hue(num3, num2, num - 1.0 / 3.0);
			}
			a = 255.0;
			return this;
		}

		public MyColor FromHSL2(double sH, double sS, double sL)
		{
			if (sS == 0.0)
			{
				r = sL * 2.55;
				g = r;
				b = r;
			}
			else
			{
				sH = (sH + 3600000.0) % 360.0;
				double[] array = new double[14]
				{
					0.1, -0.06, -0.3, -0.19, -0.15, -0.24, -0.32, -0.09, 0.18, 0.05,
					-0.12, -0.02, 0.1, -0.06
				};
				double num = sH / 30.0;
				int num2 = (int)Math.Round(Math.Floor(num));
				num = 50.0 - ((1.0 - num + (double)num2) * array[num2] + (num - (double)num2) * array[num2 + 1]) * sS;
				sL = ((sL < num) ? (sL / num) : (1.0 + (sL - num) / (100.0 - num))) * 50.0;
				FromHSL(sH, sS, sL);
			}
			a = 255.0;
			return this;
		}

		public MyColor Alpha(double sA)
		{
			a = sA;
			return this;
		}

		public override string ToString()
		{
			return "(" + a + "," + r + "," + g + "," + b + ")";
		}

		public override bool Equals(object obj)
		{
			if (obj is MyColor myColor && a == myColor.a && r == myColor.r && g == myColor.g)
			{
				return b == myColor.b;
			}
			return false;
		}
	}

	public class MyRect
	{
		public double Width { get; set; }

		public double Height { get; set; }

		public double Left { get; set; }

		public double Top { get; set; }

		public MyRect()
		{
		}

		public MyRect(double left, double top, double width, double height)
		{
			Left = left;
			Top = top;
			Width = width;
			Height = height;
		}
	}

	public enum LoadState
	{
		Waiting,
		Loading,
		Finished,
		Failed,
		Aborted
	}

	public enum ProcessReturnValues
	{
		Aborted = -1,
		Success,
		Fail,
		Exception,
		Timeout,
		Cancel,
		TaskDone
	}

	public class EqualableList<T> : List<T>
	{
		public override bool Equals(object obj)
		{
			if (!(obj is List<T>))
			{
				return false;
			}
			List<T> list = (List<T>)obj;
			if (list.Count != Count)
			{
				return false;
			}
			int i = 0;
			for (int num = list.Count - 1; i <= num; i++)
			{
				if (!list[i].Equals(base[i]))
				{
					return false;
				}
			}
			return true;
		}

		public static bool operator ==(EqualableList<T> left, EqualableList<T> right)
		{
			return EqualityComparer<EqualableList<T>>.Default.Equals(left, right);
		}

		public static bool operator !=(EqualableList<T> left, EqualableList<T> right)
		{
			return !(left == right);
		}
	}

	public class FileChecker
	{
		public long actualSize = -1L;

		public bool canUseExistsFile = true;

		public string hash;

		public bool isJson;

		public long minSize = -1L;

		public FileChecker(long minSize = -1L, long actualSize = -1L, string hash = null, bool canUseExistsFile = true, bool isJson = false)
		{
			this.actualSize = actualSize;
			this.minSize = minSize;
			this.hash = hash;
			this.canUseExistsFile = canUseExistsFile;
			this.isJson = isJson;
		}

		public string Check(string localPath)
		{
			try
			{
				Log("[Checker] 开始校验文件 " + localPath, LogLevel.Developer);
				FileInfo fileInfo = new FileInfo(localPath);
				if (!fileInfo.Exists)
				{
					return "文件不存在：" + localPath;
				}
				long length = fileInfo.Length;
				List<string> list = new List<string>();
				bool flag = false;
				if (!string.IsNullOrEmpty(hash))
				{
					if (hash.Length < 35)
					{
						string fileMD = GetFileMD5(localPath);
						if ((hash.ToLowerInvariant() ?? "") != (fileMD ?? ""))
						{
							list.Add("文件 MD5 应为 " + hash + "，实际为 " + fileMD);
						}
					}
					else if (hash.Length == 64)
					{
						string fileSHA = GetFileSHA256(localPath);
						if ((hash.ToLowerInvariant() ?? "") != (fileSHA ?? ""))
						{
							list.Add("文件 SHA256 应为 " + hash + "，实际为 " + fileSHA);
						}
					}
					else
					{
						string fileSHA2 = GetFileSHA1(localPath);
						if ((hash.ToLowerInvariant() ?? "") != (fileSHA2 ?? ""))
						{
							list.Add("文件 SHA1 应为 " + hash + "，实际为 " + fileSHA2);
						}
					}
					flag = list.Count == 0;
				}
				if (actualSize >= 0 && actualSize != length && !flag)
				{
					list.Add($"文件大小应为 {actualSize} B，实际为 {length} B" + ((length < 2000) ? ("，内容为" + ReadFile(localPath)) : ""));
				}
				if (minSize >= 0 && minSize > length)
				{
					list.Add($"文件大小应大于 {minSize} B，实际为 {length} B" + ((length < 2000) ? ("，内容为：" + ReadFile(localPath)) : ""));
				}
				if (isJson)
				{
					string text = ReadFile(localPath);
					if (string.IsNullOrEmpty(text))
					{
						throw new Exception("读取到的文件为空");
					}
					try
					{
						GetJson(text);
					}
					catch (Exception innerException)
					{
						throw new Exception(Lang.Text("Common.Error.InvalidJson"), innerException);
					}
				}
				if (list.Count != 0)
				{
					list.Insert(0, "实际校验地址：" + localPath);
					return list.Join(";");
				}
				return null;
			}
			catch (Exception ex)
			{
				Log(ex, "检查文件出错");
				return ex.ToString();
			}
		}
	}

	public class SearchEntry<T>
	{
		public bool absoluteRight;

		public T item;

		public List<SearchSource> searchSource;

		public double similarity;
	}

	public class SearchSource
	{
		public string[] aliases;

		public double weight;

		public SearchSource(string[] aliases, double weight = 1.0)
		{
			this.aliases = aliases;
			this.weight = weight;
		}

		public SearchSource(string text, double weight = 1.0)
		{
			aliases = new string[1] { text };
			this.weight = weight;
		}
	}

	public class SafeList<T> : IEnumerable<T>, IEnumerable, IDisposable, ICollection<T>
	{
		private readonly List<T> _internalList;

		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		public T this[int index]
		{
			get
			{
				return _internalList[index];
			}
			set
			{
				_internalList[index] = value;
			}
		}

		public int Count
		{
			get
			{
				_lock.EnterReadLock();
				try
				{
					return _internalList.Count;
				}
				finally
				{
					_lock.ExitReadLock();
				}
			}
		}

		public bool IsReadOnly => ((ICollection<T>)_internalList).IsReadOnly;

		public SafeList()
		{
			_internalList = new List<T>();
		}

		public SafeList(IEnumerable<T> data)
		{
			_internalList = new List<T>(data);
		}

		public void Add(T item)
		{
			_lock.EnterWriteLock();
			try
			{
				_internalList.Add(item);
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		public bool Remove(T item)
		{
			_lock.EnterWriteLock();
			try
			{
				return _internalList.Remove(item);
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		public void Clear()
		{
			_lock.EnterWriteLock();
			try
			{
				_internalList.Clear();
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		public bool Contains(T item)
		{
			return ((ICollection<T>)_internalList).Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			((ICollection<T>)_internalList).CopyTo(array, arrayIndex);
		}

		public void Dispose()
		{
			_lock.Dispose();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public List<T> ToList()
		{
			_lock.EnterReadLock();
			try
			{
				return _internalList.ToList();
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}

		public void RemoveAt(int index)
		{
			_lock.EnterWriteLock();
			try
			{
				_internalList.RemoveAt(index);
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}
	}

	public class RestartException : Exception
	{
	}

	public class CancelledException : Exception
	{
	}

	public sealed class RouteEventArgs : EventArgs
	{
		public bool handled;

		public bool raiseByMouse;

		public RouteEventArgs(bool raiseByMouse = false)
		{
			this.raiseByMouse = raiseByMouse;
		}
	}

	public delegate bool ComparisonBoolean<T>(T left, T right);

	public enum LogLevel
	{
		Normal,
		Developer,
		Debug,
		Hint,
		Msgbox,
		Feedback,
		Critical
	}

	public static readonly string versionBaseName = Basics.VersionName;

	public static readonly string versionStandardCode = Basics.Metadata.Version.StandardVersion;

	public static readonly string upstreamVersion = Basics.Metadata.Version.UpstreamVersion;

	public static readonly string commitHash = Basics.Metadata.Version.Commit;

	public static readonly string commitHashShort = Basics.Metadata.Version.CommitDigest;

	public static readonly int versionCode = Basics.VersionCode;

	public const string versionBranchName = "Publish";

	public const string versionBranchCode = "0";

	public static nint frmHandle;

	public static readonly string exePath = (Basics.ExecutableDirectory.EndsWith("\\") ? Basics.ExecutableDirectory : (Basics.ExecutableDirectory + "\\"));

	public static readonly string pathImage = "pack://application:,,,/Plain Craft Launcher 2;component/Images/";

	public static string currentLang = "zh_CN";

	public static ModSetup setup = new ModSetup();

	public static long applicationStartTick = TimeUtils.GetTimeTick();

	public static DateTime applicationOpenTime = DateTime.Now;

	public static bool isProgramEnded = false;

	public static string pathTemp = Paths.Temp + "\\";

	public static string pathAppdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCL") + "\\";

	public static string pathAppdataConfig = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.pclce\\";

	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> iniCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

	private static readonly object writeIniLock = new object();

	public static char vbLQ = Convert.ToChar(8220);

	public static char vbRQ = Convert.ToChar(8221);

	public const int MaxLocalSearchDepth = 25;

	public static string pathPure = GetPureASCIIDir();

	private static int uuid = 1;

	private static object uuidLock;

	public static readonly int dpi = (int)Math.Round(Graphics.FromHwnd(IntPtr.Zero).DpiX);

	private static readonly int uiThreadId = Thread.CurrentThread.ManagedThreadId;

	public static bool modeDebug = false;

	private static bool isCriticalErrorTriggered;

	public static string RadixConvert(string input, int fromRadix, int toRadix)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "0";
		}
		bool flag = input.StartsWithF("-");
		if (flag)
		{
			input = input.TrimStart('-');
		}
		long num = 0L;
		long num2 = 1L;
		foreach (int item in from l in input.Reverse()
			select "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+=".IndexOfF(l.ToString()))
		{
			num += item * num2;
			num2 *= fromRadix;
		}
		string text = "";
		while (num > 0)
		{
			int num3 = (int)(num % toRadix);
			num = (long)Math.Round((double)(num - num3) / (double)toRadix);
			text = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+="[num3] + text;
		}
		return (flag ? "-" : "") + text;
	}

	public static double MathBezier(double x, double x1, double y1, double x2, double y2, double acc = 0.01)
	{
		if (x <= 0.0 || double.IsNaN(x))
		{
			return 0.0;
		}
		if (x >= 1.0)
		{
			return 1.0;
		}
		double num = x;
		double num2;
		do
		{
			num2 = 3.0 * num * ((0.33333333 + x1 - x2) * num * num + (x2 - 2.0 * x1) * num + x1);
			num += (x - num2) * 0.5;
		}
		while (!(Math.Abs(num2 - x) < acc));
		return 3.0 * num * ((0.33333333 + y1 - y2) * num * num + (y2 - 2.0 * y1) * num + y1);
	}

	public static byte MathByte(double d)
	{
		if (d < 0.0)
		{
			d = 0.0;
		}
		if (d > 255.0)
		{
			d = 255.0;
		}
		return (byte)Math.Round(Math.Round(d));
	}

	public static MyColor MathRound(MyColor col, int w = 0)
	{
		return new MyColor
		{
			a = Math.Round(col.a, w),
			r = Math.Round(col.r, w),
			g = Math.Round(col.g, w),
			b = Math.Round(col.b, w)
		};
	}

	public static double MathPercent(double valueA, double valueB, double percent)
	{
		return Math.Round(valueA * (1.0 - percent) + valueB * percent, 6);
	}

	public static MyColor MathPercent(MyColor valueA, MyColor valueB, double percent)
	{
		return MathRound(valueA * (1.0 - percent) + valueB * percent, 6);
	}

	public static double MathClamp(double value, double min, double max)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	public static int MathSgn(double value)
	{
		if (value == 0.0)
		{
			return 0;
		}
		if (value > 0.0)
		{
			return 1;
		}
		return -1;
	}

	public static void IniClearCache(string fileName)
	{
		if (!fileName.Contains(":\\"))
		{
			fileName = exePath + "PCL\\" + fileName + ".ini";
		}
		if (iniCache.ContainsKey(fileName))
		{
			iniCache.Remove(fileName, out ConcurrentDictionary<string, string> _);
		}
	}

	private static ConcurrentDictionary<string, string> IniGetContent(string fileName)
	{
		try
		{
			if (!fileName.Contains(":\\"))
			{
				fileName = exePath + "PCL\\" + fileName + ".ini";
			}
			if (iniCache.ContainsKey(fileName))
			{
				return iniCache[fileName];
			}
			if (!File.Exists(fileName))
			{
				return null;
			}
			ConcurrentDictionary<string, string> concurrentDictionary = new ConcurrentDictionary<string, string>();
			string[] array = ReadFile(fileName).Split("\r\n".ToArray(), StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				int num = text.IndexOfF(":");
				if (num > 0)
				{
					concurrentDictionary[text.Substring(0, num)] = text.Substring(num + 1);
				}
			}
			iniCache[fileName] = concurrentDictionary;
			return concurrentDictionary;
		}
		catch (Exception ex)
		{
			Log(ex, "生成 ini 文件缓存失败（" + fileName + "）", LogLevel.Hint);
			return null;
		}
	}

	public static string ReadIni(string fileName, string key, string defaultValue = "")
	{
		ConcurrentDictionary<string, string> concurrentDictionary = IniGetContent(fileName);
		if (concurrentDictionary == null || !concurrentDictionary.ContainsKey(key))
		{
			return defaultValue;
		}
		return concurrentDictionary[key];
	}

	public static bool HasIniKey(string fileName, string key)
	{
		return IniGetContent(fileName)?.ContainsKey(key) ?? false;
	}

	public static void DeleteIniKey(string fileName, string key)
	{
		WriteIni(fileName, key, null);
	}

	public static void WriteIni(string fileName, string key, string value)
	{
		try
		{
			if (key.Contains(":"))
			{
				throw new Exception("尝试写入 ini 文件 " + fileName + " 的键名中包含了冒号：" + key);
			}
			key = key.Replace("\r", "").Replace("\n", "");
			value = value?.Replace("\r", "").Replace("\n", "");
			lock (writeIniLock)
			{
				ConcurrentDictionary<string, string> concurrentDictionary = IniGetContent(fileName);
				if (concurrentDictionary == null)
				{
					concurrentDictionary = new ConcurrentDictionary<string, string>();
				}
				if (value == null)
				{
					if (!concurrentDictionary.ContainsKey(key))
					{
						return;
					}
					concurrentDictionary.Remove(key, out var _);
				}
				else
				{
					if (concurrentDictionary.ContainsKey(key) && (concurrentDictionary[key] ?? "") == (value ?? ""))
					{
						return;
					}
					concurrentDictionary[key] = value;
				}
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<string, string> item in concurrentDictionary)
				{
					stringBuilder.Append(item.Key);
					stringBuilder.Append(":");
					stringBuilder.Append(item.Value);
					stringBuilder.Append("\r\n");
				}
				if (!fileName.Contains(":\\"))
				{
					fileName = exePath + "PCL\\" + fileName + ".ini";
				}
				WriteFile(fileName, stringBuilder.ToString());
			}
		}
		catch (Exception ex)
		{
			Log(ex, $"写入文件失败（{fileName} → {key}:{value}）", LogLevel.Hint);
		}
	}

	public static string GetPathFromFullPath(string filePath)
	{
		string text = null;
		if (!filePath.Contains("\\") && !filePath.Contains("/"))
		{
			throw new Exception("不包含路径：" + filePath);
		}
		if (filePath.EndsWithF("\\") || filePath.EndsWithF("/"))
		{
			bool flag = filePath.EndsWithF("\\");
			filePath = filePath.Substring(0, filePath.Length - 1);
			text = filePath.Substring(0, filePath.LastIndexOfAny(new char[2] { '\\', '/' })) + (flag ? "\\" : "/");
		}
		else
		{
			text = filePath.Substring(0, filePath.LastIndexOfAny(new char[2] { '\\', '/' }) + 1);
			if (string.IsNullOrEmpty(text))
			{
				throw new Exception("不包含路径：" + filePath);
			}
		}
		return text;
	}

	public static string GetFileNameFromPath(string filePath)
	{
		filePath = filePath.Replace("/", "\\");
		if (filePath.EndsWithF("\\"))
		{
			throw new Exception("不包含文件名：" + filePath);
		}
		if (filePath.Contains("?"))
		{
			filePath = filePath.Substring(0, filePath.IndexOfF("?"));
		}
		if (filePath.Contains("\\"))
		{
			filePath = filePath.Substring(filePath.LastIndexOfF("\\") + 1);
		}
		int length = filePath.Length;
		if (length == 0)
		{
			throw new Exception("不包含文件名：" + filePath);
		}
		if (length > 250)
		{
			throw new PathTooLongException("文件名过长：" + filePath);
		}
		return filePath;
	}

	public static string GetFileNameWithoutExtentionFromPath(string filePath)
	{
		return Path.GetFileNameWithoutExtension(filePath);
	}

	public static string GetFolderNameFromPath(string folderPath)
	{
		if (folderPath.EndsWithF(":\\") || folderPath.EndsWithF(":\\\\"))
		{
			return folderPath.Substring(0, 1);
		}
		if (folderPath.EndsWithF("\\") || folderPath.EndsWithF("/"))
		{
			folderPath = folderPath.Substring(0, folderPath.Length - 1);
		}
		return GetFileNameFromPath(folderPath);
	}

	public static void CopyFile(string fromPath, string toPath)
	{
		try
		{
			if (!fromPath.Contains(":\\"))
			{
				fromPath = exePath + fromPath;
			}
			if (!toPath.Contains(":\\"))
			{
				toPath = exePath + toPath;
			}
			if (!((fromPath ?? "") == (toPath ?? "")))
			{
				Directory.CreateDirectory(GetPathFromFullPath(toPath));
				File.Copy(fromPath, toPath, overwrite: true);
			}
		}
		catch (Exception innerException)
		{
			throw new Exception("复制文件出错：" + fromPath + " → " + toPath, innerException);
		}
	}

	public static byte[] ReadFileBytes(string filePath, Encoding encoding = null)
	{
		try
		{
			if (!filePath.Contains(":\\"))
			{
				filePath = exePath + filePath;
			}
			if (File.Exists(filePath))
			{
				using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using MemoryStream memoryStream = new MemoryStream();
					fileStream.CopyTo(memoryStream);
					return memoryStream.ToArray();
				}
			}
			Log("[System] 欲读取的文件不存在，已返回空内容：" + filePath);
			return Array.Empty<byte>();
		}
		catch (Exception ex)
		{
			Log(ex, "读取文件出错：" + filePath);
			return Array.Empty<byte>();
		}
	}

	public static string ReadFile(string filePath, Encoding encoding = null)
	{
		byte[] bytes = ReadFileBytes(filePath);
		if (encoding != null)
		{
			return encoding.GetString(bytes);
		}
		return DecodeBytes(bytes);
	}

	public static string ReadFile(Stream stream, Encoding encoding = null)
	{
		try
		{
			MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			byte[] array = memoryStream.ToArray();
			return (encoding ?? EncodingDetector.DetectEncoding(array)).GetString(array);
		}
		catch (Exception ex)
		{
			Log(ex, "读取流出错");
			return "";
		}
	}

	public static void WriteFile(string filePath, string text, bool append = false, Encoding? encoding = null)
	{
		if (!filePath.Contains(":\\"))
		{
			filePath = exePath + filePath;
		}
		Directory.CreateDirectory(GetPathFromFullPath(filePath));
		if (append)
		{
			using (StreamWriter streamWriter = new StreamWriter(filePath, append: true, encoding ?? EncodingDetector.DetectEncoding(ReadFileBytes(filePath))))
			{
				streamWriter.Write(text);
				return;
			}
		}
		byte[] bytes = ((encoding == null) ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text) : encoding.GetBytes(text));
		string text2 = filePath + ".pcltmp." + Guid.NewGuid().ToString("N");
		File.WriteAllBytes(text2, bytes);
		File.Move(text2, filePath, overwrite: true);
	}

	public static void WriteFile(string filePath, byte[] content, bool append = false)
	{
		if (!filePath.Contains(":\\"))
		{
			filePath = exePath + filePath;
		}
		Directory.CreateDirectory(GetPathFromFullPath(filePath));
		File.WriteAllBytes(filePath, content);
	}

	public static bool WriteFile(string filePath, Stream stream)
	{
		try
		{
			if (!filePath.Contains(":\\"))
			{
				filePath = exePath + filePath;
			}
			Directory.CreateDirectory(GetPathFromFullPath(filePath));
			using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				fileStream.SetLength(0L);
				stream.CopyTo(fileStream);
			}
			return true;
		}
		catch (Exception ex)
		{
			Log(ex, "保存流出错");
			return false;
		}
	}

	public static string DecodeBytes(byte[] bytes)
	{
		int num = bytes.Length;
		if (num < 3)
		{
			return Encoding.UTF8.GetString(bytes);
		}
		if (bytes[0] >= 239)
		{
			if (bytes[0] == 239 && bytes[1] == 187)
			{
				return Encoding.UTF8.GetString(bytes, 3, num - 3);
			}
			if (bytes[0] == 254 && bytes[1] == byte.MaxValue)
			{
				return Encoding.BigEndianUnicode.GetString(bytes, 3, num - 3);
			}
			if (bytes[0] == byte.MaxValue && bytes[1] == 254)
			{
				return Encoding.Unicode.GetString(bytes, 3, num - 3);
			}
			return Encoding.GetEncoding("GB18030").GetString(bytes, 3, num - 3);
		}
		string text = Encoding.UTF8.GetString(bytes);
		char value = Encoding.UTF8.GetString(new byte[3] { 239, 191, 189 }).ToCharArray()[0];
		if (text.Contains(value))
		{
			return Encoding.GetEncoding("GB18030").GetString(bytes);
		}
		return text;
	}

	public static object GetHexString(Memory<byte> bytes)
	{
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
		Span<byte> span = bytes.Span;
		for (int i = 0; i < span.Length; i++)
		{
			byte b = span[i];
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	public static string GetFileMD5(string filePath)
	{
		bool flag = false;
		while (true)
		{
			try
			{
				using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return (string)GetHexString(MD5Provider.Instance.ComputeHash((Stream)fileStream));
			}
			catch (Exception ex)
			{
				if (flag || ex is FileNotFoundException)
				{
					Log(ex, "获取文件 MD5 失败：" + filePath);
					return "";
				}
				flag = true;
				Log(ex, "获取文件 MD5 可重试失败：" + filePath, LogLevel.Normal);
				Thread.Sleep(RandomUtils.NextInt(200, 500));
			}
		}
	}

	public static string GetFileSHA512(string filePath)
	{
		bool flag = false;
		while (true)
		{
			try
			{
				using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return (string)GetHexString(SHA512Provider.Instance.ComputeHash((Stream)fileStream));
			}
			catch (Exception ex)
			{
				if (flag || ex is FileNotFoundException)
				{
					Log(ex, "获取文件 SHA512 失败：" + filePath);
					return "";
				}
				flag = true;
				Log(ex, "获取文件 SHA512 可重试失败：" + filePath, LogLevel.Normal);
				Thread.Sleep(RandomUtils.NextInt(200, 500));
			}
		}
	}

	public static string GetFileSHA256(string filePath)
	{
		bool flag = false;
		while (true)
		{
			try
			{
				using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return (string)GetHexString(SHA256Provider.Instance.ComputeHash((Stream)fileStream));
			}
			catch (Exception ex)
			{
				if (flag || ex is FileNotFoundException)
				{
					Log(ex, "获取文件 SHA256 失败：" + filePath);
					return "";
				}
				flag = true;
				Log(ex, "获取文件 SHA256 可重试失败：" + filePath, LogLevel.Normal);
				Thread.Sleep(RandomUtils.NextInt(200, 500));
			}
		}
	}

	public static string GetFileSHA1(string filePath)
	{
		bool flag = false;
		while (true)
		{
			try
			{
				using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return (string)GetHexString(SHA1Provider.Instance.ComputeHash((Stream)fileStream));
			}
			catch (Exception ex)
			{
				if (flag || ex is FileNotFoundException)
				{
					Log(ex, "获取文件 SHA1 失败：" + filePath);
					return "";
				}
				flag = true;
				Log(ex, "获取文件 SHA1 可重试失败：" + filePath, LogLevel.Normal);
				Thread.Sleep(RandomUtils.NextInt(200, 500));
			}
		}
	}

	public static string GetAuthSHA1(Stream inputStream)
	{
		try
		{
			return (string)GetHexString(SHA1Provider.Instance.ComputeHash(inputStream));
		}
		catch (Exception ex)
		{
			Log(ex, "获取流 SHA1 失败");
			return "";
		}
	}

	public static void WaitForFileReady(string filePath, int timeoutMs = 2000)
	{
		WaitForFileReady(filePath, timeoutMs, requireJson: false);
	}

	public static void WaitForFileReady(string filePath, int timeoutMs, bool requireJson)
	{
		filePath = (filePath.Contains(":\\") ? filePath : (exePath + filePath));
		int tickCount = Environment.TickCount;
		long num = -1L;
		while (Environment.TickCount - tickCount < timeoutMs)
		{
			if (File.Exists(filePath))
			{
				try
				{
					long length = new FileInfo(filePath).Length;
					if (length <= 0)
					{
						continue;
					}
					if (!requireJson)
					{
						if (length == num)
						{
							break;
						}
						num = length;
					}
					else
					{
						string text = ReadFile(filePath);
						if (!string.IsNullOrEmpty(text) && text.Trim().StartsWith("{"))
						{
							break;
						}
					}
					goto IL_0079;
				}
				catch (IOException)
				{
					goto IL_0079;
				}
			}
			goto IL_0079;
			IL_0079:
			Thread.Sleep(50);
		}
	}

	public static void ExtractFile(string compressFilePath, string destDirectory, Encoding encode = null, Action<double> progressIncrementHandler = null)
	{
		Directory.CreateDirectory(destDirectory);
		destDirectory = Path.GetFullPath(destDirectory);
		if (!destDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			destDirectory += Path.DirectorySeparatorChar;
		}
		if (compressFilePath.EndsWithF(".gz", ignoreCase: true))
		{
			using (FileStream stream = new FileStream(compressFilePath, FileMode.Open, FileAccess.Read))
			{
				using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
				using FileStream destination = new FileStream(Path.Combine(destDirectory, GetFileNameFromPath(compressFilePath).ToLower().Replace(".tar", "").Replace(".gz", "")), FileMode.OpenOrCreate, FileAccess.Write);
				gZipStream.CopyTo(destination);
				return;
			}
		}
		using ZipArchive zipArchive = ZipFile.Open(compressFilePath, ZipArchiveMode.Read, encode ?? Encoding.GetEncoding("GB18030"));
		int count = zipArchive.Entries.Count;
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			progressIncrementHandler?.Invoke(1.0 / (double)count);
			string fullPath = Path.GetFullPath(Path.Combine(destDirectory, entry.FullName));
			if (!fullPath.StartsWithF(destDirectory))
			{
				throw new Exception($"解压文件 {entry.FullName} 错误：解压文件路径 {fullPath} 不在目标目录 {destDirectory} 内");
			}
			if (!fullPath.EndsWithF("\\") && !fullPath.EndsWithF("/"))
			{
				Directory.CreateDirectory(GetPathFromFullPath(fullPath));
				entry.ExtractToFile(fullPath, overwrite: true);
			}
		}
	}

	public static int DeleteDirectory(string path, bool ignoreIssue = false)
	{
		if (!Directory.Exists(path))
		{
			return 0;
		}
		int num = 0;
		string[] files;
		try
		{
			files = Directory.GetFiles(path);
		}
		catch (DirectoryNotFoundException ex)
		{
			Log(ex, "疑似为孤立符号链接，尝试直接删除（" + path + "）", LogLevel.Developer);
			Directory.Delete(path);
			return 0;
		}
		string[] array = files;
		foreach (string text in array)
		{
			bool flag = false;
			while (true)
			{
				try
				{
					File.Delete(text);
					num++;
				}
				catch (Exception ex2)
				{
					if (!flag)
					{
						flag = true;
						Log(ex2, "删除文件失败，将在 0.3s 后重试（" + text + "）");
						Thread.Sleep(300);
						continue;
					}
					if (ignoreIssue)
					{
						Log(ex2, "删除单个文件可忽略地失败");
						break;
					}
					throw;
				}
				break;
			}
		}
		array = Directory.GetDirectories(path);
		for (int i = 0; i < array.Length; i++)
		{
			DeleteDirectory(array[i], ignoreIssue);
		}
		bool flag2 = false;
		while (true)
		{
			try
			{
				Directory.Delete(path, recursive: true);
			}
			catch (Exception ex3)
			{
				if (!flag2 && !RunInUi())
				{
					flag2 = true;
					Log(ex3, "删除文件夹失败，将在 0.3s 后重试（" + path + "）");
					Thread.Sleep(300);
					continue;
				}
				if (ignoreIssue)
				{
					Log(ex3, "删除单个文件夹可忽略地失败");
					break;
				}
				throw;
			}
			break;
		}
		return num;
	}

	public static void CopyDirectory(string fromPath, string toPath, Action<double> progressIncrementHandler = null)
	{
		fromPath = fromPath.Replace("/", "\\");
		if (!fromPath.EndsWithF("\\"))
		{
			fromPath += "\\";
		}
		toPath = toPath.Replace("/", "\\");
		if (!toPath.EndsWithF("\\"))
		{
			toPath += "\\";
		}
		List<FileInfo> list = EnumerateFiles(fromPath).ToList();
		int count = list.Count;
		foreach (FileInfo item in list)
		{
			CopyFile(item.FullName, item.FullName.Replace(fromPath, toPath));
			progressIncrementHandler?.Invoke(1.0 / (double)count);
		}
	}

	public static IEnumerable<FileInfo> EnumerateFiles(string directory)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(ShortenPath(directory));
		if (!directoryInfo.Exists)
		{
			return new List<FileInfo>();
		}
		return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);
	}

	public static string ShortenPath(string longPath, int shortenThreshold = 247)
	{
		if (longPath.Length <= shortenThreshold)
		{
			return longPath;
		}
		StringBuilder stringBuilder = new StringBuilder(260);
		GetShortPathName(longPath, stringBuilder, 260);
		return stringBuilder.ToString();
	}

	public static void MoveDirectory(string sourceDir, string targetDir)
	{
		if (!Directory.Exists(targetDir))
		{
			Directory.CreateDirectory(targetDir);
		}
		string[] files = Directory.GetFiles(sourceDir);
		foreach (string obj in files)
		{
			string fileNameFromPath = GetFileNameFromPath(obj);
			File.Move(obj, Path.Combine(targetDir, fileNameFromPath));
		}
		files = Directory.GetDirectories(sourceDir);
		foreach (string obj2 in files)
		{
			string folderNameFromPath = GetFolderNameFromPath(obj2);
			MoveDirectory(obj2, Path.Combine(targetDir, folderNameFromPath));
		}
	}

	[DllImport("kernel32", EntryPoint = "GetShortPathNameA")]
	private static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

	public static void CreateSymbolicLink(string linkPath, string targetPath, int flags)
	{
		Process process = new Process();
		string fileName = ModLaunch.ExtractLinkD();
		ProcessStartInfo startInfo = process.StartInfo;
		startInfo.FileName = fileName;
		startInfo.Arguments = $"\"{linkPath}\" \"{targetPath}\"";
		startInfo.CreateNoWindow = true;
		startInfo.UseShellExecute = false;
		process.Start();
		while (!process.HasExited)
		{
		}
	}

	public static string GetStringFromEnum(Enum enumData)
	{
		return Enum.GetName(enumData.GetType(), enumData);
	}

	public static string GetString(long fileSize)
	{
		return ByteStream.GetReadableLength(fileSize, 0, (IFormatProvider)Lang.Culture);
	}

	public static JsonNode GetJson(string data)
	{
		try
		{
			return JsonCompat.ParseNode(data);
		}
		catch (Exception innerException)
		{
			string text = data ?? "";
			int length = text.Length;
			throw new Exception("格式化 JSON 失败：" + ((length > 2000) ? (text.Substring(0, 500) + $"...(全长 {length} 个字符)..." + text.Substring(length - 500)) : text), innerException);
		}
	}

	public static string Capitalize(this string word)
	{
		if (string.IsNullOrEmpty(word))
		{
			return word;
		}
		return word.Substring(0, 1).ToUpperInvariant() + word.Substring(1).ToLowerInvariant();
	}

	public static string StrFill(string str, string code, byte length)
	{
		if (str.Length > length)
		{
			return str.Substring(0, length);
		}
		return str.PadRight(length, code[0]).Substring(str.Length) + str;
	}

	public static string StrFillNum(double num, int length)
	{
		return Lang.Number<double>(num, $"F{length}");
	}

	public static object StrTrim(string str, bool removeQuote = true)
	{
		if (removeQuote)
		{
			str = str.Split("（")[0].Split("：")[0].Split("(")[0].Split(":")[0];
		}
		return str.Trim(new char[9] { '.', '。', '！', ' ', '!', '?', '？', '\r', '\n' });
	}

	public static string Join(this IEnumerable list, string split)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (object item in list)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				stringBuilder.Append(split);
			}
			if (item != null)
			{
				stringBuilder.Append(item);
			}
		}
		return stringBuilder.ToString();
	}

	public static string[] Split(this string fullStr, string splitStr)
	{
		if (splitStr.Length == 1)
		{
			return fullStr.Split(splitStr[0]);
		}
		return fullStr.Split(new string[1] { splitStr }, StringSplitOptions.None);
	}

	public static ulong GetHash(string str)
	{
		ulong num = 0uL;
		num = 5381uL;
		int i = 0;
		for (int num2 = str.Length - 1; i <= num2; i++)
		{
			num = (num << 5) ^ num ^ str[i];
		}
		return num ^ 0xA98F501BC684032FuL;
	}

	public static string GetStringMD5(string str)
	{
		return (string)GetHexString(MD5Provider.Instance.ComputeHash(str, (Encoding)null));
	}

	public static bool IsASCII(this string input)
	{
		return input.All((char c) => c < '\u0080');
	}

	public static string BeforeFirst(this string str, string text, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(text) ? (-1) : str.IndexOfF(text, ignoreCase));
		if (num >= 0)
		{
			return str.Substring(0, num);
		}
		return str;
	}

	public static string BeforeLast(this string str, string text, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(text) ? (-1) : str.LastIndexOfF(text, ignoreCase));
		if (num >= 0)
		{
			return str.Substring(0, num);
		}
		return str;
	}

	public static string AfterFirst(this string str, string text, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(text) ? (-1) : str.IndexOfF(text, ignoreCase));
		if (num >= 0)
		{
			return str.Substring(num + text.Length);
		}
		return str;
	}

	public static string AfterLast(this string str, string text, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(text) ? (-1) : str.LastIndexOfF(text, ignoreCase));
		if (num >= 0)
		{
			return str.Substring(num + text.Length);
		}
		return str;
	}

	public static string Between(this string str, string after, string before, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(after) ? (-1) : str.LastIndexOfF(after, ignoreCase));
		num = ((num >= 0) ? (num + after.Length) : 0);
		int num2 = (string.IsNullOrEmpty(before) ? (-1) : str.IndexOfF(before, num, ignoreCase));
		if (num2 >= 0)
		{
			return str.Substring(num, num2 - num);
		}
		if (num > 0)
		{
			return str.Substring(num);
		}
		return str;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool StartsWithF(this string str, string prefix, bool ignoreCase = false)
	{
		return str.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EndsWithF(this string str, string suffix, bool ignoreCase = false)
	{
		return str.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ContainsF(this string str, string subStr, bool ignoreCase = false)
	{
		return str.IndexOf(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfF(this string str, string subStr, bool ignoreCase = false)
	{
		return str.IndexOf(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfF(this string str, string subStr, int startIndex, bool ignoreCase = false)
	{
		return str.IndexOf(subStr, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfF(this string str, string subStr, bool ignoreCase = false)
	{
		return str.LastIndexOf(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfF(this string str, string subStr, int startIndex, bool ignoreCase = false)
	{
		return str.LastIndexOf(subStr, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	public static double Val(object str)
	{
		try
		{
			return (str is string text && text == "&") ? 0.0 : Conversion.Val(str);
		}
		catch
		{
			return 0.0;
		}
	}

	public static string EscapeXML(string str)
	{
		if (str.StartsWithF("{"))
		{
			str = "{}" + str;
		}
		return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
			.Replace("'", "&apos;")
			.Replace("\"", "&quot;")
			.Replace("\r\n", "&#xa;");
	}

	public static string EscapeLikePattern(string input)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in input)
		{
			switch (c)
			{
			case '#':
			case '*':
			case '?':
			case '[':
			case ']':
				stringBuilder.Append('[').Append(c).Append(']');
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	public static List<string> RegexSearch(this string str, string regex, RegexOptions options = RegexOptions.None)
	{
		List<string> list = null;
		try
		{
			list = new List<string>();
			MatchCollection matchCollection = new Regex(regex, options).Matches(str);
			if (matchCollection == null)
			{
				return list;
			}
			foreach (Match item in matchCollection)
			{
				list.Add(item.Value);
			}
			return list;
		}
		catch (Exception ex)
		{
			Log(ex, "正则匹配全部项出错");
			return new List<string>();
		}
	}

	public static List<string> RegexSearch(this string str, Regex regex)
	{
		try
		{
			List<string> list = new List<string>();
			foreach (Match item in regex.Matches(str))
			{
				list.Add(item.Value);
			}
			return list;
		}
		catch (Exception ex)
		{
			Log(ex, "正则匹配全部项出错");
			return new List<string>();
		}
	}

	public static string RegexSeek(this string str, string regex, RegexOptions options = RegexOptions.None)
	{
		try
		{
			string value = Regex.Match(str, regex, options).Value;
			return string.IsNullOrEmpty(value) ? null : value;
		}
		catch (Exception ex)
		{
			Log(ex, "正则匹配第一项出错");
			return null;
		}
	}

	public static string RegexSeek(this string str, Regex regex, RegexOptions options = RegexOptions.None)
	{
		try
		{
			string value = regex.Match(str, (int)options).Value;
			return string.IsNullOrEmpty(value) ? null : value;
		}
		catch (Exception ex)
		{
			Log(ex, "正则匹配第一项出错");
			return null;
		}
	}

	public static bool RegexCheck(this string str, string regex, RegexOptions options = RegexOptions.None)
	{
		try
		{
			return Regex.IsMatch(str, regex, options);
		}
		catch (Exception ex)
		{
			Log(ex, "正则检查出错");
			return false;
		}
	}

	public static string RegexReplace(this string allContents, string searchRegex, string replaceTo, RegexOptions options = RegexOptions.None)
	{
		return Regex.Replace(allContents, searchRegex, replaceTo, options);
	}

	public static string RegexReplaceEach(this string allContents, string searchRegex, MatchEvaluator replaceTo, RegexOptions options = RegexOptions.None)
	{
		return Regex.Replace(allContents, searchRegex, replaceTo, options);
	}

	private static double SearchSimilarity(string source, string query)
	{
		int i = 0;
		double num = 0.0;
		source = source.ToLower().Replace(" ", "");
		query = query.ToLower().Replace(" ", "");
		int length = source.Length;
		int length2;
		int num2;
		for (length2 = query.Length; i < length2; i += Math.Max(1, num2))
		{
			int j = 0;
			num2 = 0;
			int num3 = 0;
			int k;
			for (; j < source.Length; j += Math.Max(1, k))
			{
				for (k = 0; i + k < length2 && j + k < source.Length && source[j + k] == query[i + k]; k++)
				{
				}
				if (k > num2)
				{
					num2 = k;
					num3 = j;
				}
			}
			if (num2 > 0)
			{
				source = source.Substring(0, num3) + ((source.Count() > num3 + num2) ? source.Substring(num3 + num2) : string.Empty);
				double num4 = Math.Pow(1.4, 3 + num2) - 3.6;
				num4 *= 1.0 + 0.3 * (double)Math.Max(0, 3 - Math.Abs(i - num3));
				num += num4;
			}
		}
		return num / (double)length2 * (3.0 / Math.Pow(length + 15, 0.5)) * (double)((length2 > 2) ? 1 : (3 - length2));
	}

	private static double SearchSimilarityWeighted(List<SearchSource> source, string query)
	{
		double num = 0.0;
		double num2 = 0.0;
		foreach (SearchSource item in source)
		{
			if (item.aliases.Any())
			{
				num2 += item.aliases.Max((string a) => SearchSimilarity(a, query)) * item.weight;
			}
			num += item.weight;
		}
		return num2 / num;
	}

	public static List<SearchEntry<T>> Search<T>(List<SearchEntry<T>> entries, string query, int maxBlurCount = 5, double minBlurSimilarity = 0.1)
	{
		List<SearchEntry<T>> list = new List<SearchEntry<T>>();
		if (entries == null || !entries.Any())
		{
			return list;
		}
		string[] array = query.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			list.AddRange(entries);
			return list;
		}
		string[] array2 = array.Select((string q) => q.ToLower()).ToArray();
		foreach (SearchEntry<T> entry in entries)
		{
			entry.similarity = SearchSimilarityWeighted(entry.searchSource, query);
			List<string[]> list2 = entry.searchSource.Select((SearchSource s) =>
			{
				for (int i = 0; i < s.aliases.Length; i++)
				{
					s.aliases[i] = s.aliases[i].Replace(" ", "").ToLower();
				}
				return s.aliases;
			}).ToList();
			bool absoluteRight = true;
			string[] array3 = array2;
			foreach (string qp in array3)
			{
				bool flag = false;
				foreach (string[] item in list2)
				{
					if (item.Any((string p) => p.Contains(qp)))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					absoluteRight = false;
					break;
				}
			}
			entry.absoluteRight = absoluteRight;
		}
		List<SearchEntry<T>> list3 = (from e in entries
			orderby e.absoluteRight descending, e.similarity descending
			select e).ToList();
		int num2 = 0;
		foreach (SearchEntry<T> item2 in list3)
		{
			if (item2.absoluteRight)
			{
				list.Add(item2);
				continue;
			}
			if (item2.similarity < minBlurSimilarity || num2 >= maxBlurCount)
			{
				break;
			}
			list.Add(item2);
			num2++;
		}
		return list;
	}

	public static bool IsUtf8CodePage()
	{
		return Encoding.Default.CodePage == 65001;
	}

	private static string GetPureASCIIDir()
	{
		if (exePath.IsASCII())
		{
			return exePath + "PCL\\";
		}
		if (pathAppdata.IsASCII())
		{
			return pathAppdata;
		}
		if (pathTemp.IsASCII())
		{
			return pathTemp;
		}
		return Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL");
	}

	public static bool IsInstanceOfGenericType(this Type genericType, object obj)
	{
		if (obj == null)
		{
			return false;
		}
		Type type = obj.GetType();
		while ((object)type != null)
		{
			if (type.IsGenericType && (object)type.GetGenericTypeDefinition() == genericType)
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	public static int GetUuid()
	{
		if (uuidLock == null)
		{
			uuidLock = new object();
		}
		lock (uuidLock)
		{
			uuid++;
			return uuid;
		}
	}

	public static List<T> GetFullList<T>(IList data)
	{
		List<T> list = null;
		list = new List<T>();
		int i = 0;
		for (int num = data.Count - 1; i <= num; i++)
		{
			if (data[i] is ICollection)
			{
				list.AddRange((IEnumerable<T>)data[i]);
			}
			else
			{
				list.Add((T)data[i]);
			}
		}
		return list;
	}

	public static List<T> Distinct<T>(this ICollection<T> arr, ComparisonBoolean<T> isEqual)
	{
		List<T> list = new List<T>();
		int i = 0;
		for (int num = arr.Count - 1; i <= num; i++)
		{
			int num2 = i + 1;
			int num3 = arr.Count - 1;
			while (true)
			{
				if (num2 <= num3)
				{
					if (isEqual(arr.ElementAtOrDefault(i), arr.ElementAtOrDefault(num2)))
					{
						break;
					}
					num2++;
					continue;
				}
				list.Add(arr.ElementAtOrDefault(i));
				break;
			}
		}
		return list;
	}

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
	{
		foreach (T item in collection)
		{
			action(item);
		}
		return collection;
	}

	public static void ShellOnly(string fileName, string arguments = "")
	{
		try
		{
			fileName = ShortenPath(fileName);
			using Process process = new Process();
			process.StartInfo.Arguments = arguments;
			process.StartInfo.FileName = fileName;
			process.StartInfo.UseShellExecute = true;
			Log("[System] 执行外部命令：" + fileName + " " + arguments);
			process.Start();
		}
		catch (Exception ex)
		{
			Log(ex, "打开文件或程序失败：" + fileName, LogLevel.Msgbox, null, Lang.Text("SystemDialog.File.OpenFailed.Message", new object[1] { fileName }));
		}
	}

	public static ProcessReturnValues ShellAndGetExitCode(string fileName, string arguments = "", int timeout = 1000000)
	{
		try
		{
			using Process process = new Process();
			process.StartInfo.Arguments = arguments;
			process.StartInfo.FileName = fileName;
			Log("[System] 执行外部命令并等待返回码：" + fileName + " " + arguments);
			process.Start();
			if (process.WaitForExit(timeout))
			{
				return (ProcessReturnValues)process.ExitCode;
			}
			return ProcessReturnValues.Timeout;
		}
		catch (Exception ex)
		{
			Log(ex, "执行命令失败：" + fileName, LogLevel.Msgbox);
			return ProcessReturnValues.Fail;
		}
	}

	public static string ShellAndGetOutput(string fileName, string arguments = "", int timeout = 1000000, string workingDirectory = null)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		if (!string.IsNullOrEmpty(workingDirectory))
		{
			processStartInfo.WorkingDirectory = workingDirectory.TrimEnd('\\');
		}
		Log("[System] 执行外部命令并等待返回结果：" + fileName + " " + arguments);
		using Process process = new Process
		{
			StartInfo = processStartInfo
		};
		process.Start();
		Task<string> task = process.StandardOutput.ReadToEndAsync();
		Task<string> task2 = process.StandardError.ReadToEndAsync();
		if (process.WaitForExit(timeout))
		{
			InlineArray2<Task> buffer = default;
			buffer[0] = task;
			buffer[1] = task2;
			Task.WaitAll(buffer);
		}
		else
		{
			process.Kill();
			InlineArray2<Task> buffer2 = default;
			buffer2[0] = task;
			buffer2[1] = task2;
			Task.WaitAll(buffer2);
		}
		return task.Result + task2.Result;
	}

	public static Thread RunInNewThread(Action action, string name = null, ThreadPriority priority = ThreadPriority.Normal)
	{
		Thread thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (ThreadInterruptedException)
			{
				Log(name + "：线程已中止");
			}
			catch (Exception ex2)
			{
				Log(ex2, name + "：线程执行失败", LogLevel.Feedback);
			}
		});
		thread.Name = name ?? ("Runtime New Invoke " + GetUuid() + "#");
		thread.Priority = priority;
		thread.Start();
		return thread;
	}

	public static Output RunInUiWait<Output>(Func<Output> action)
	{
		if (RunInUi())
		{
			return action();
		}
		return ((DispatcherObject)System.Windows.Application.Current).Dispatcher.Invoke<Output>(action);
	}

	public static void RunInUiWait(Action action)
	{
		if (System.Windows.Application.Current != null)
		{
			if (RunInUi())
			{
				action();
			}
			else
			{
				((DispatcherObject)System.Windows.Application.Current).Dispatcher.Invoke(action);
			}
		}
	}

	public static void RunInUi(Action action, bool forceWaitUntilLoaded = false)
	{
		if (System.Windows.Application.Current != null)
		{
			if (RunInUi())
			{
				action();
			}
			else
			{
				((DispatcherObject)System.Windows.Application.Current).Dispatcher.InvokeAsync(action, (DispatcherPriority)(forceWaitUntilLoaded ? 6 : 9));
			}
		}
	}

	public static void RunInThread(Action action)
	{
		if (RunInUi())
		{
			RunInNewThread(action, "Runtime Invoke " + GetUuid() + "#");
		}
		else
		{
			action();
		}
	}

	public static List<T> Sort<T>(this IList<T> list, ComparisonBoolean<T> sortRule)
	{
		List<T> array = new List<T>(list);
		if (array.Count <= 1)
		{
			return array;
		}
		MergeSort_Sort(ref array, 0, array.Count - 1, sortRule);
		return array;
	}

	private static void MergeSort_Sort<T>(ref List<T> array, int left, int right, ComparisonBoolean<T> comparator)
	{
		if (left < right)
		{
			int num = (left + right) / 2;
			MergeSort_Sort(ref array, left, num, comparator);
			MergeSort_Sort(ref array, num + 1, right, comparator);
			MergeSort_Merge(ref array, left, num, right, comparator);
		}
	}

	private static void MergeSort_Merge<T>(ref List<T> array, int left, int mid, int right, ComparisonBoolean<T> comparator)
	{
		List<T> list = new List<T>();
		List<T> list2 = new List<T>();
		for (int i = left; i <= mid; i++)
		{
			list.Add(array[i]);
		}
		for (int j = mid + 1; j <= right; j++)
		{
			list2.Add(array[j]);
		}
		int num = 0;
		int num2 = 0;
		int num3 = left;
		while (num < list.Count && num2 < list2.Count)
		{
			if (comparator(list[num], list2[num2]))
			{
				array[num3] = list[num];
				num++;
			}
			else
			{
				array[num3] = list2[num2];
				num2++;
			}
			num3++;
		}
		while (num < list.Count)
		{
			array[num3] = list[num];
			num++;
			num3++;
		}
		while (num2 < list2.Count)
		{
			array[num3] = list2[num2];
			num2++;
			num3++;
		}
	}

	public static IList<T> Clone<T>(this IList<T> list)
	{
		return new List<T>(list);
	}

	public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
	{
		if (dict.ContainsKey(key))
		{
			return dict[key];
		}
		return defaultValue;
	}

	public static void AddToList<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
	{
		if (dict.ContainsKey(key))
		{
			dict[key].Add(value);
			return;
		}
		dict.Add(key, new List<TValue> { value });
	}

	public static object GetProgramArgument(string name, object defaultValue = null)
	{
		string[] array = Interaction.Command().Split(" ");
		int i = 0;
		for (int num = array.Length - 1; i <= num; i++)
		{
			if ((array[i] ?? "") == "-" + name)
			{
				if (array.Length == i + 1 || array[i + 1].StartsWithF("-"))
				{
					return true;
				}
				return array[i + 1];
			}
		}
		return defaultValue;
	}

	public static void OpenWebsite(string url)
	{
		try
		{
			if (!url.StartsWithF("http", ignoreCase: true) && !url.StartsWithF("minecraft://", ignoreCase: true))
			{
				throw new Exception(url + " 不是一个有效的网址，它必须以 http 开头！");
			}
			Log("[System] 正在打开网页：" + url);
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Log(ex, "无法打开网页（" + url + "）");
			ClipboardSet(url, showSuccessHint: false);
			ModMain.MyMsgBox(ExceptionDetails.Compose(Lang.Text("SystemDialog.Browser.OpenFailed.Message", new object[1] { url }), ex), Lang.Text("SystemDialog.Browser.OpenFailed.Title"));
		}
	}

	public static void OpenExplorer(string location)
	{
		try
		{
			location = ShortenPath(location.Replace("/", "\\").Trim(new char[2] { ' ', '"' }));
			Log("[System] 正在打开资源管理器：" + location);
			if (location.EndsWithF("\\"))
			{
				ShellOnly(location);
			}
			else
			{
				ShellOnly("explorer", "/select,\"" + location + "\"");
			}
		}
		catch (Exception ex)
		{
			Log(ex, "打开资源管理器失败，请尝试关闭安全软件（如 360 安全卫士）", LogLevel.Msgbox, null, Lang.Text("SystemDialog.Folder.OpenFailed.Message", new object[1] { location }));
		}
	}

	public static void ClipboardSet(string text, bool showSuccessHint = true)
	{
		RunInThread(() =>
		{
			bool flag = false;
			for (int i = 0; i <= 5; i++)
			{
				try
				{
					RunInUi(() =>
					{
						Clipboard.SetText(text);
					});
					flag = true;
				}
				catch (Exception) when (i < 5)
				{
					Thread.Sleep(20);
					continue;
				}
				catch (Exception ex2)
				{
					Log(ex2, "剪贴板被占用，文本复制失败", LogLevel.Hint, null, Lang.Text("Common.Hint.CopyFailed"));
					continue;
				}
				break;
			}
			if (flag & showSuccessHint)
			{
				RunInUi(() =>
				{
					HintService.Hint(Lang.Text("Common.Hint.Copied"), HintType.Success);
				});
			}
		});
	}

	public static int PasteFileFromClipboard(string dest, bool copyFile = true, bool copyDir = true)
	{
		Log("[System] 从剪贴板粘贴文件到：" + dest);
		try
		{
			StringCollection fileDropList = Clipboard.GetFileDropList();
			if (fileDropList.Count.Equals(0))
			{
				Log("[System] 剪贴板内无文件可粘贴");
				return 0;
			}
			int num = 0;
			int num2 = 0;
			StringEnumerator enumerator = fileDropList.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					if (copyFile && File.Exists(current))
					{
						try
						{
							string text = dest + GetFileNameFromPath(current);
							if (File.Exists(text))
							{
								Log("[System] 已存在同名文件：" + text);
							}
							else
							{
								File.Copy(current, text);
								num++;
							}
						}
						catch (Exception ex)
						{
							Log(ex, "[System] 复制文件时出错");
							continue;
						}
					}
					if (!copyDir || !Directory.Exists(current))
					{
						continue;
					}
					try
					{
						string text2 = dest + GetFolderNameFromPath(current);
						if (Directory.Exists(text2))
						{
							Log("[System] 已存在同名文件夹：" + text2);
							continue;
						}
						CopyDirectory(current, text2);
						num2++;
					}
					catch (Exception ex2)
					{
						Log(ex2, "[System] 复制文件时出错");
					}
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			HintService.Hint(Lang.Text("Common.Hint.FilesPasted", new object[2] { num, num2 }));
		}
		catch (Exception ex3)
		{
			Log(ex3, "[System] 从剪切板粘贴文件失败", LogLevel.Hint);
		}
		return 0;
	}

	public static Stream GetResourceStream(string path)
	{
		return System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/" + path, UriKind.Absolute))?.Stream;
	}

	public static bool CheckPermission(string path)
	{
		try
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}
			if (!path.EndsWithF("\\"))
			{
				path += "\\";
			}
			if (path.EndsWithF(":\\System Volume Information\\") || path.EndsWithF(":\\$RECYCLE.BIN\\"))
			{
				return false;
			}
			if (!Directory.Exists(path))
			{
				return false;
			}
			string text = "CheckPermission" + GetUuid();
			if (File.Exists(path + text))
			{
				File.Delete(path + text);
			}
			File.Create(path + text).Dispose();
			File.Delete(path + text);
			return true;
		}
		catch (Exception ex)
		{
			Log(ex, "没有对文件夹 " + path + " 的权限，请尝试以管理员权限运行 PCL");
			return false;
		}
	}

	public static void CheckPermissionWithException(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentNullException("文件夹名不能为空！");
		}
		if (!path.EndsWithF("\\"))
		{
			path += "\\";
		}
		if (!Directory.Exists(path))
		{
			throw new DirectoryNotFoundException("文件夹不存在！");
		}
		if (File.Exists(path + "CheckPermission"))
		{
			File.Delete(path + "CheckPermission");
		}
		File.Create(path + "CheckPermission").Dispose();
		File.Delete(path + "CheckPermission");
	}

	public static void SetLaunchFont(string fontName = null)
	{
		try
		{
			LocalizationFontService.ApplyLaunchFont(fontName, LocalizationService.CurrentLanguage);
		}
		catch (Exception ex)
		{
			Log(ex, "设置字体失败", LogLevel.Hint);
		}
	}

	public static void DeltaLeft(FrameworkElement control, double newValue)
	{
		DebugAssert(!double.IsNaN(newValue));
		DebugAssert(!double.IsInfinity(newValue));
		if (control is Window)
		{
			((Window)control).Left += newValue;
			return;
		}
		switch (control.HorizontalAlignment)
		{
		case HorizontalAlignment.Left:
		case HorizontalAlignment.Stretch:
			control.Margin = new Thickness(control.Margin.Left + newValue, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
			break;
		case HorizontalAlignment.Right:
			control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right - newValue, control.Margin.Bottom);
			break;
		default:
			DebugAssert(exp: false);
			break;
		}
	}

	public static void SetLeft(FrameworkElement control, double newValue)
	{
		DebugAssert(control.HorizontalAlignment == HorizontalAlignment.Left);
		control.Margin = new Thickness(newValue, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
	}

	public static void DeltaTop(FrameworkElement control, double newValue)
	{
		DebugAssert(!double.IsNaN(newValue));
		DebugAssert(!double.IsInfinity(newValue));
		if (control is Window)
		{
			((Window)control).Top += newValue;
			return;
		}
		switch (control.VerticalAlignment)
		{
		case VerticalAlignment.Top:
			control.Margin = new Thickness(control.Margin.Left, control.Margin.Top + newValue, control.Margin.Right, control.Margin.Bottom);
			break;
		case VerticalAlignment.Bottom:
			control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom - newValue);
			break;
		default:
			DebugAssert(exp: false);
			break;
		}
	}

	public static void SetTop(FrameworkElement control, double newValue)
	{
		DebugAssert(control.VerticalAlignment == VerticalAlignment.Top);
		control.Margin = new Thickness(control.Margin.Left, newValue, control.Margin.Right, control.Margin.Bottom);
	}

	public static double GetPixelSize(double wPFSize)
	{
		return wPFSize / 96.0 * (double)dpi;
	}

	public static double GetWPFSize(double pixelSize)
	{
		return pixelSize * 96.0 / (double)dpi;
	}

	public static ImageBrush ControlBrush(FrameworkElement uI)
	{
		double actualWidth = uI.ActualWidth;
		double actualHeight = uI.ActualHeight;
		if (actualWidth < 1.0 || actualHeight < 1.0)
		{
			return new ImageBrush();
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)Math.Round(GetPixelSize(actualWidth)), (int)Math.Round(GetPixelSize(actualHeight)), dpi, dpi, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(uI);
		return new ImageBrush(renderTargetBitmap);
	}

	public static ImageBrush ControlBrush(FrameworkElement uI, double width, double height, double left = 0.0, double top = 0.0)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		uI.Measure(new System.Windows.Size(width, height));
		uI.Arrange(new Rect(0.0, 0.0, width, height));
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)Math.Round(GetPixelSize(width)), (int)Math.Round(GetPixelSize(height)), dpi, dpi, PixelFormats.Default);
		renderTargetBitmap.Render(uI);
		if (left != 0.0 || top != 0.0)
		{
			uI.Arrange(new Rect(left, top, width, height));
		}
		return new ImageBrush(renderTargetBitmap);
	}

	public static void ControlFreeze(Panel uI)
	{
		uI.Background = ControlBrush(uI);
		uI.Children.Clear();
	}

	public static void ControlFreeze(Border uI)
	{
		uI.Background = ControlBrush(uI);
		uI.Child = null;
	}

	public static object GetObjectFromXML(XElement str)
	{
		return GetObjectFromXML(str.ToString());
	}

	public static object GetObjectFromXML(string str)
	{
		XamlEventSanitizer.SanitizeResult sanitizeResult;
		return GetObjectFromXML(str, out sanitizeResult);
	}

	public static object GetObjectFromXML(string str, out XamlEventSanitizer.SanitizeResult sanitizeResult)
	{
		str = str.Replace("EventType=\"", "local:CustomEventService.EventType=\"").Replace("EventData=\"", "local:CustomEventService.EventData=\"").Replace("Property=\"EventType\"", "Property=\"local:CustomEventService.EventType\"")
			.Replace("Property=\"EventData\"", "Property=\"local:CustomEventService.EventData\"");
		str = str.Replace("local:CustomEventService.local:CustomEventService.", "local:CustomEventService.");
		sanitizeResult = XamlEventSanitizer.Sanitize(str);
		str = sanitizeResult.SanitizedXaml;
		using MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(str));
		using (XamlXmlReader xamlXmlReader = new XamlXmlReader((Stream)memoryStream))
		{
			while (xamlXmlReader.Read())
			{
				Type[] array = new Type[7]
				{
					typeof(WebBrowser),
					typeof(Frame),
					typeof(MediaElement),
					typeof(ObjectDataProvider),
					typeof(System.Xaml.XamlReader),
					typeof(Window),
					typeof(XmlDataProvider)
				};
				foreach (Type type in array)
				{
					if ((object)xamlXmlReader.Type != null && type.IsAssignableFrom(xamlXmlReader.Type.UnderlyingType))
					{
						throw new UnauthorizedAccessException("不允许使用 " + type.Name + " 类型。");
					}
					if (xamlXmlReader.Value != null && object.Equals(xamlXmlReader.Value, type.Name))
					{
						throw new UnauthorizedAccessException("不允许使用 " + type.Name + " 值。");
					}
				}
				string[] array2 = new string[3] { "Code", "FactoryMethod", "Static" };
				foreach (string text in array2)
				{
					if ((object)xamlXmlReader.Member != null && (xamlXmlReader.Member.Name ?? "") == (text ?? ""))
					{
						throw new UnauthorizedAccessException("不允许使用 " + text + " 成员。");
					}
				}
			}
		}
		memoryStream.Position = 0L;
		using StreamWriter streamWriter = new StreamWriter(memoryStream);
		streamWriter.Write(str);
		streamWriter.Flush();
		memoryStream.Position = 0L;
		return System.Windows.Markup.XamlReader.Load((Stream)memoryStream);
	}

	public static bool RunInUi()
	{
		return Thread.CurrentThread.ManagedThreadId == uiThreadId;
	}

	public static void Log(string text, LogLevel level = LogLevel.Normal, string? title = null, string? userSummary = null)
	{
		if (new LogLevel[2]
		{
			LogLevel.Msgbox,
			LogLevel.Hint
		}.Contains(level, null))
		{
			LogWrapper.Warn(text);
		}
		else if (LogLevel.Feedback == level)
		{
			LogWrapper.Error(text);
		}
		else if (LogLevel.Critical == level)
		{
			LogWrapper.Fatal(text);
		}
		else if (LogLevel.Debug == level)
		{
			LogWrapper.Debug(text);
		}
		else if (LogLevel.Developer == level)
		{
			LogWrapper.Trace(text);
		}
		else
		{
			LogWrapper.Info(text);
		}
		if (isProgramEnded || level == LogLevel.Normal)
		{
			return;
		}
		string text2 = text.RegexReplace("\\[[^\\]]+?\\] ", "");
		string text3 = (string.IsNullOrWhiteSpace(userSummary) ? Lang.Text("SystemDialog.Error.UserVisible.Message", new object[1] { text2 }) : userSummary);
		string title2 = _GetUserDialogTitle(title);
		switch (level)
		{
		case LogLevel.Debug:
			if (modeDebug)
			{
				HintService.Hint("[调试模式] " + text, HintType.Info, log: false);
			}
			break;
		case LogLevel.Hint:
			HintService.Hint(text3, HintType.Error, log: false);
			break;
		case LogLevel.Msgbox:
			ModMain.MyMsgBox(text3, title2, null, "", "", isWarn: true);
			break;
		case LogLevel.Feedback:
			_ShowFeedbackPrompt(text3, title2, isCritical: false);
			break;
		case LogLevel.Critical:
			_ShowFeedbackPrompt(text3, title2, isCritical: true);
			break;
		case LogLevel.Developer:
			break;
		}
	}

	public static void Log(Exception ex, string desc, LogLevel level = LogLevel.Debug, string? title = null, string? userSummary = null)
	{
		if (ex is ThreadInterruptedException)
		{
			return;
		}
		if (new LogLevel[2]
		{
			LogLevel.Msgbox,
			LogLevel.Hint
		}.Contains(level, null))
		{
			LogWrapper.Warn(ex, desc);
		}
		else if (LogLevel.Feedback == level)
		{
			LogWrapper.Error(ex, desc);
		}
		else if (LogLevel.Critical == level)
		{
			LogWrapper.Fatal(ex, desc);
		}
		else if (LogLevel.Debug == level)
		{
			LogWrapper.Debug($"{desc}:{ex}");
		}
		else if (LogLevel.Developer == level)
		{
			LogWrapper.Trace($"{desc}:{ex}");
		}
		else
		{
			LogWrapper.Error(ex, desc);
		}
		if (isProgramEnded)
		{
			return;
		}
		string text = _GetUserExceptionMessage(desc, ex, userSummary);
		string title2 = _GetUserDialogTitle(title);
		switch (level)
		{
		case LogLevel.Debug:
			if (modeDebug)
			{
				HintService.Hint("[调试模式] " + desc + "：" + ex, HintType.Info, log: false);
			}
			break;
		case LogLevel.Hint:
			HintService.Hint(text, HintType.Error, log: false);
			break;
		case LogLevel.Msgbox:
			ModMain.MyMsgBox(text, title2, null, "", "", isWarn: true);
			break;
		case LogLevel.Feedback:
			_ShowFeedbackPrompt(text, title2, isCritical: false);
			break;
		case LogLevel.Critical:
			_ShowFeedbackPrompt(text, title2, isCritical: true);
			break;
		case LogLevel.Normal:
		case LogLevel.Developer:
			break;
		}
	}

	private static string _GetUserDialogTitle(string? title)
	{
		if (!string.IsNullOrWhiteSpace(title))
		{
			return title;
		}
		return Lang.Text("SystemDialog.Error.Title");
	}

	private static string _GetUserExceptionMessage(string desc, Exception ex, string? userSummary)
	{
		if (!string.IsNullOrWhiteSpace(userSummary))
		{
			return ExceptionDetails.Compose(userSummary, ex);
		}
		if (!(ex.GetType() == typeof(Win32Exception)))
		{
			return Lang.Text("SystemDialog.Error.OperationFailed.Message", new object[2]
			{
				desc,
				ex.ToString()
			});
		}
		return Lang.Text("SystemDialog.Error.OperationFailed.RuntimeMessage", new object[2]
		{
			desc,
			ex.ToString()
		});
	}

	private static void _ShowFeedbackPrompt(string userMessage, string title, bool isCritical)
	{
		if (isCritical && isCriticalErrorTriggered)
		{
			FormMain.EndProgramForce(ProcessReturnValues.Exception);
			return;
		}
		if (isCritical)
		{
			isCriticalErrorTriggered = true;
		}
		if (CanFeedback(showHint: false))
		{
			string text = Lang.Text("Setup.Feedback.ErrorPrompt.Submit.Message", new object[1] { userMessage });
			if (isCritical ? (Interaction.MsgBox(text, MsgBoxStyle.YesNo | MsgBoxStyle.Critical, title) == MsgBoxResult.Yes) : (ModMain.MyMsgBox(text, title, Lang.Text("Setup.Feedback.ErrorPrompt.Submit.Action"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1))
			{
				Feedback(showMsgbox: false, forceOpenLog: true);
			}
		}
		else
		{
			string text2 = Lang.Text("Setup.Feedback.ErrorPrompt.Update.Message", new object[1] { userMessage });
			if (isCritical)
			{
				Interaction.MsgBox(text2, MsgBoxStyle.Critical, title);
			}
			else
			{
				ModMain.MyMsgBox(text2, title, null, "", "", isWarn: true);
			}
		}
	}

	public static string Base64Decode(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		byte[] bytes = Convert.FromBase64String(text);
		return Encoding.UTF8.GetString(bytes);
	}

	public static string Base64Encode(string text)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
	}

	public static string Base64Encode(byte[] bytes)
	{
		return Convert.ToBase64String(bytes);
	}

	public static void Feedback(bool showMsgbox = true, bool forceOpenLog = false)
	{
		FeedbackInfo();
		string text = DateTime.Now.ToString("yyyy-M-dd", CultureInfo.InvariantCulture);
		if (forceOpenLog || (showMsgbox && ModMain.MyMsgBox(Lang.Text("Setup.Feedback.Reminder.Message", new object[1] { text }), Lang.Text("Setup.Feedback.Reminder.Title"), Lang.Text("Common.Action.OpenFolder"), Lang.Text("Setup.Feedback.Reminder.NotNeeded")) == 1))
		{
			OpenExplorer(exePath + "PCL\\Log\\");
		}
		OpenWebsite("https://github.com/stu-lian/PCL-Ray/issues/");
	}

	public static bool CanFeedback(bool showHint)
	{
		UpdateEnums.VersionStatus versionStatus = UpdateManager.GetVersionStatus();
		if (versionStatus == UpdateEnums.VersionStatus.Latest)
		{
			return true;
		}
		if (!showHint)
		{
			return false;
		}
		if (ModMain.MyMsgBox((versionStatus == UpdateEnums.VersionStatus.NotLatest) ? Lang.Text("Setup.Feedback.Unavailable.NotLatest.Message") : Lang.Text("Setup.Feedback.Unavailable.CheckFailed.Message"), Lang.Text("Setup.Feedback.Unavailable.Title"), (versionStatus == UpdateEnums.VersionStatus.NotLatest) ? Lang.Text("Setup.Feedback.Unavailable.NotLatest.Action") : Lang.Text("Setup.Feedback.Unavailable.CheckFailed.Action"), Lang.Text("Common.Action.Cancel")) == 1)
		{
			ModMain.frmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.DownloadCompFavorites);
		}
		return false;
	}

	public static void FeedbackInfo()
	{
		try
		{
			(ulong, ulong) physicalMemoryBytes = KernelInterop.GetPhysicalMemoryBytes();
			ulong value = physicalMemoryBytes.Item2 / 1024 / 1024;
			ulong value2 = physicalMemoryBytes.Item1 / 1024 / 1024;
			double num = Math.Round((double)dpi / 96.0, 2);
			LogWrapper.Info($"[System] Diagnostic Information:\nOS: {RuntimeInformation.OSDescription} (32-bit: {SystemInfo.Is32BitSystem})\nMemory: {value} MiB / {value2} MiB\nDPI: {dpi} ({num * 100.0}%)\nMC Folder: {ModFolder.mcFolderSelected ?? "Nothing"}\nExecutable Path: {exePath}");
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Failed to collect feedback information");
		}
	}

	public static void DebugAssert(bool exp)
	{
		if (!exp)
		{
			throw new Exception("断言命中");
		}
	}

	public static string GetStackTrace()
	{
		return (from f in new StackTrace().GetFrames().Skip(1)
			select f.GetMethod() into f
			select f.Name + "(" + (from p in f.GetParameters()
				select p.ToString()).ToList().Join(", ") + ") - " + f.Module).ToList().Join("\r\n").Replace("\r\n\r\n", "\r\n");
	}
}

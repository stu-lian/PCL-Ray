using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;

namespace PCL;

public class MyImage : Image
{
	private static readonly ConcurrentDictionary<string, Task<string>> _downloadTasks = new ConcurrentDictionary<string, Task<string>>();

	public TimeSpan fileCacheExpiredTime = TimeSpan.FromDays(14.0);

	public static readonly DependencyProperty EnableCacheProperty = DependencyProperty.Register("EnableCache", typeof(bool), typeof(MyImage), new PropertyMetadata((object)true));

	public new static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(MyImage), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyImage)(object)sender).Source = e.NewValue.ToString();
		}
	})));

	private static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(MyImage), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)new CornerRadius(-1.0), (PropertyChangedCallback)OnCornerRadiusChanged));

	public string ActualSource
	{
		get
		{
			return field;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				value = null;
			}
			if ((field ?? "") == (value ?? ""))
			{
				return;
			}
			field = value;
			((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
			{
				try
				{
					MyBitmap myBitmap = ((value != null) ? (await Task.Run(() => new MyBitmap(value))) : null);
					ImageSource source = myBitmap;
					base.Source = source;
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "加载图片失败（" + value + "）");
					try
					{
						if (value.StartsWithF(ModBase.pathTemp) && File.Exists(value))
						{
							File.Delete(value);
						}
					}
					catch
					{
					}
				}
			}), Array.Empty<object>());
		}
	}

	public bool EnableCache
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(EnableCacheProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(EnableCacheProperty, (object)value);
		}
	}

	public new string Source
	{
		get
		{
			return field;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				value = null;
			}
			if (!((field ?? "") == (value ?? "")))
			{
				field = value;
				if (IsInitialized)
				{
					Load();
				}
			}
		}
	} = "";

	public string FallbackSource { get; set; }

	public string LoadingSource { get; set; } = "pack://application:,,,/images/Icons/NoIcon.png";

	public CornerRadius CornerRadius
	{
		get
		{
			return (CornerRadius)((DependencyObject)this).GetValue(CornerRadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(CornerRadiusProperty, (object)value);
		}
	}

	public MyImage()
	{
		Initialized += (object? _, EventArgs _) =>
		{
			Load();
		};
		SizeChanged += (object _, SizeChangedEventArgs _) =>
		{
			UpdateClip();
		};
	}

	private void Load()
	{
		if (Source == null)
		{
			ActualSource = null;
			return;
		}
		if (!Source.StartsWithF("http"))
		{
			ActualSource = Source;
			return;
		}
		string url = Source;
		string tempPath = GetTempPath(url);
		FileInfo tempFile = new FileInfo(tempPath);
		bool enableCache = EnableCache;
		if (enableCache && tempFile.Exists)
		{
			ActualSource = tempPath;
			if (DateTime.Now - tempFile.LastWriteTime < fileCacheExpiredTime)
			{
				return;
			}
		}
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			_ = 1;
			try
			{
				ActualSource = LoadingSource;
				string text = await DownloadImageAsync(url);
				if (!string.IsNullOrEmpty(text))
				{
					ActualSource = text;
				}
				else
				{
					text = await DownloadImageAsync(FallbackSource);
					if (!string.IsNullOrEmpty(text))
					{
						ActualSource = text;
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, $"Online image get fail（source = {url}, fallback = {FallbackSource}）", ModBase.LogLevel.Developer);
				tempPath = GetTempPath(url);
				tempFile = new FileInfo(tempPath);
				if (enableCache && tempFile.Exists)
				{
					ActualSource = tempPath;
					if (!(DateTime.Now - tempFile.LastWriteTime < fileCacheExpiredTime))
					{
					}
				}
			}
		}), Array.Empty<object>());
	}

	public static Task<string> DownloadImageAsync(string url)
	{
		return _downloadTasks.GetOrAdd(url, (string key) =>
		{
			Task<string> task = DownloadImageInternalAsync(key);
			task.ContinueWith((Task<string> _) => _downloadTasks.TryRemove(url, out _));
			return task;
		});
	}

	public static string GetTempPath(string url)
	{
		return Path.Combine(ModBase.pathTemp, "Cache", "Images", ModBase.GetStringMD5(url) + ".png");
	}

	private static async Task<string> DownloadImageInternalAsync(string url)
	{
		string tempPath = GetTempPath(url);
		string tempDownloadingPath = tempPath + RandomUtils.NextInt(0, 1000000);
		try
		{
			Directory.CreateDirectory(ModBase.GetPathFromFullPath(tempPath));
			using (FileStream fs = new FileStream(tempDownloadingPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				using HttpResponseMessage response = await HttpSenderExtension.SendAsync(HttpRequest.Create(url), (HttpClient)null, false, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken));
				response.EnsureSuccessStatusCode();
				using Stream nfs = await HttpResponseExtension.AsStreamAsync(response, default(CancellationToken));
				fs.SetLength(0L);
				await nfs.CopyToAsync(fs);
			}
			File.Move(tempDownloadingPath, tempPath, overwrite: true);
			return tempPath;
		}
		catch (Exception ex)
		{
			if (File.Exists(tempPath))
			{
				File.Delete(tempPath);
			}
			if (File.Exists(tempDownloadingPath))
			{
				File.Delete(tempDownloadingPath);
			}
			ModBase.Log(ex, $"Try to get online image fail (url = {url}, dest = {tempPath})");
			return string.Empty;
		}
	}

	private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((MyImage)(object)d).UpdateClip();
	}

	private void UpdateClip()
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (ActualWidth > 0.0 && ActualHeight > 0.0 && CornerRadius.TopLeft >= 0.0 && CornerRadius.TopRight >= 0.0)
		{
			Clip = new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight), CornerRadius.TopLeft, CornerRadius.TopRight);
		}
	}

	static MyImage()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected Obj, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected Obj, but got Unknown
	}
}

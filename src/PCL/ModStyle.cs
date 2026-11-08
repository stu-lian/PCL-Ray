using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.UI.Controls;
using PCL.Core.Utils;

namespace PCL;

internal static class ModStyle
{
	public class TimerRun : Run, IDisposable
	{
		public delegate void TimerTickDelegate(TimerRun sender);

		public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty.Register("UpdateInterval", typeof(TimeSpan), typeof(TimerRun), new PropertyMetadata((object)TimeSpan.FromSeconds(1.0)));

		private object _isDisposed = false;

		private DispatcherTimer _timer;

		private object _isTimerRunning => _timer != null && _timer.IsEnabled;

		public TimeSpan UpdateInterval
		{
			get
			{
				return (TimeSpan)((DependencyObject)this).GetValue(UpdateIntervalProperty);
			}
			set
			{
				if (value > TimeSpan.Zero)
				{
					((DependencyObject)this).SetValue(UpdateIntervalProperty, (object)value);
				}
			}
		}

		public bool AutoStart { get; set; }

		public event TimerTickDelegate? TimerTick;

		public TimerRun(TimeSpan interval = default(TimeSpan), bool autoStart = false)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected Obj, but got Unknown
			_timer = new DispatcherTimer();
			_timer.Tick += _TimerTick;
			UpdateInterval = ((interval == default(TimeSpan)) ? TimeSpan.FromSeconds(1.0) : interval);
			AutoStart = autoStart;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		public void Dispose()
		{
			if (!(bool)_isDisposed)
			{
				_isDisposed = true;
				_timer.Tick -= _TimerTick;
				DispatcherTimer timer = _timer;
				if (timer != null)
				{
					timer.Stop();
				}
				_timer = null;
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			base.OnPropertyChanged(e);
			if (e.Property == UpdateIntervalProperty && _timer != null)
			{
				_timer.Interval = UpdateInterval;
			}
		}

		private void _TimerTick(object sender, EventArgs e)
		{
			TimerTick?.Invoke(this);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (AutoStart)
			{
				StartTimer();
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			StopTimer();
		}

		public void StartTimer()
		{
			if (((DispatcherObject)this).Dispatcher == null)
			{
				ModBase.Log("[TimerRun] Dispatcher is null, unable to run", ModBase.LogLevel.Critical, null, Lang.Text("Minecraft.Launch.Error.DispatcherUnavailable"));
			}
			else if (!(bool)_isTimerRunning)
			{
				DispatcherTimer timer = _timer;
				if (timer != null)
				{
					timer.Start();
				}
			}
		}

		public void StopTimer()
		{
			if ((bool)_isTimerRunning)
			{
				DispatcherTimer timer = _timer;
				if (timer != null)
				{
					timer.Stop();
				}
			}
		}

		static TimerRun()
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected Obj, but got Unknown
		}
	}

	public class MinecraftFormatter
	{
		private static readonly Dictionary<string, string> colorMap = new Dictionary<string, string>
		{
			{ "black", "0" },
			{ "dark_blue", "1" },
			{ "dark_green", "2" },
			{ "dark_aqua", "3" },
			{ "dark_red", "4" },
			{ "dark_purple", "5" },
			{ "gold", "6" },
			{ "gray", "7" },
			{ "dark_gray", "8" },
			{ "blue", "9" },
			{ "green", "a" },
			{ "aqua", "b" },
			{ "red", "c" },
			{ "light_purple", "d" },
			{ "yellow", "e" },
			{ "white", "f" }
		};

		private static readonly Random random = new Random();

		private static readonly string randomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_+-=[]{}|;:,.<>?/~";

		public static string ConvertToMinecraftFormat(JsonObject data)
		{
			string text = "";
			foreach (JsonNode item in data["extra"].AsArray())
			{
				text += ProcessElement((JsonObject)item, new List<string>());
			}
			return text.Replace("§§", "§");
		}

		private static string ProcessElement(JsonObject element, List<string> currentFormat)
		{
			string text = "";
			List<string> list = new List<string>(currentFormat);
			if (element.ContainsKey("bold") && JsonCompat.ToObject<bool>(element["bold"]))
			{
				list.Add("l");
			}
			if (element.ContainsKey("color"))
			{
				string key = element["color"].ToString();
				string item = "f";
				if (colorMap.ContainsKey(key))
				{
					item = colorMap[key];
				}
				list.Insert(0, item);
			}
			if (list.Count > 0)
			{
				text = text + "§" + string.Join("§", list);
			}
			if (element.ContainsKey("text"))
			{
				text += element["text"].ToString();
			}
			if (element.ContainsKey("extra"))
			{
				foreach (JsonNode item2 in element["extra"].AsArray())
				{
					text += ProcessElement((JsonObject)item2, new List<string>(list));
				}
			}
			return text;
		}

		public static void SetColorfulTextLab(string text, TextBlock lab, bool isDarkMode = true)
		{
			if (lab == null)
			{
				ModBase.Log("[Style] SetColorfulTextLab: lab is null");
				return;
			}
			lab.Inlines.Clear();
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			string text2 = (isDarkMode ? "#FFFFFF" : "#888888");
			bool flag6 = false;
			TimerRun timerRun = new TimerRun();
			lab.Inlines.Add(timerRun);
			List<TimerRun> list = new List<TimerRun>();
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (c.ToString() == "§")
				{
					flag6 = true;
					continue;
				}
				if (flag6)
				{
					string text3 = text2;
					if (!MotdRenderer.TryGetColorFromCode(c.ToString(), isDarkMode, out text2))
					{
						text2 = text3;
						switch (c)
						{
						case 'K':
						case 'k':
							flag5 = true;
							if (!string.IsNullOrEmpty(timerRun.Text))
							{
								timerRun = new TimerRun();
								lab.Inlines.Add(timerRun);
							}
							timerRun.AutoStart = true;
							list.Add(timerRun);
							break;
						case 'l':
							flag4 = true;
							break;
						case 'o':
							flag = true;
							break;
						case 'n':
							flag3 = true;
							break;
						case 'm':
							flag2 = true;
							break;
						case 'r':
							text2 = (isDarkMode ? "#FFFFFF" : "#888888");
							flag4 = false;
							flag = false;
							flag3 = false;
							flag2 = false;
							flag5 = false;
							break;
						}
					}
					if (!string.IsNullOrEmpty(timerRun.Text) && c.ToString() != "k" && c.ToString() != "K")
					{
						timerRun = new TimerRun();
						lab.Inlines.Add(timerRun);
					}
					timerRun.Foreground = new SolidColorBrush(new ModBase.MyColor(text2));
					timerRun.FontWeight = (flag4 ? FontWeights.Bold : FontWeights.Normal);
					timerRun.FontStyle = (flag ? FontStyles.Italic : FontStyles.Normal);
					timerRun.TextDecorations = (flag3 ? TextDecorations.Strikethrough : null);
					timerRun.TextDecorations = (flag2 ? TextDecorations.Underline : null);
				}
				else if (flag5)
				{
					timerRun.Text += randomChars[random.Next(randomChars.Length)];
				}
				else
				{
					timerRun.Text += c;
				}
				if (flag6)
				{
					flag6 = false;
				}
			}
			if (list.Count <= 0)
			{
				return;
			}
			foreach (TimerRun item in list)
			{
				item.UpdateInterval = TimeSpan.FromMilliseconds(20.0);
				item.TimerTick += (TimerRun sender) =>
				{
					if (!string.IsNullOrEmpty(sender.Text))
					{
						StringBuilder stringBuilder = new StringBuilder();
						int j = 0;
						for (int num = sender.Text.Length - 1; j <= num; j++)
						{
							stringBuilder.Append(randomChars[random.Next(randomChars.Length)]);
						}
						sender.Text = stringBuilder.ToString();
					}
				};
			}
		}
	}
}

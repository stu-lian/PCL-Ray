using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.Utils;

namespace PCL.Core.UI.Controls;

public partial class MotdRenderer : UserControl, IComponentConnector
{
	private static readonly Dictionary<string, Brush> _ColorMapWithBlackBackground = new Dictionary<string, Brush>
	{
		{
			"0",
			Brushes.Black
		},
		{
			"1",
			new SolidColorBrush(Color.FromRgb(0, 0, 170))
		},
		{
			"2",
			new SolidColorBrush(Color.FromRgb(0, 170, 0))
		},
		{
			"3",
			new SolidColorBrush(Color.FromRgb(0, 170, 170))
		},
		{
			"4",
			new SolidColorBrush(Color.FromRgb(170, 0, 0))
		},
		{
			"5",
			new SolidColorBrush(Color.FromRgb(170, 0, 170))
		},
		{
			"6",
			new SolidColorBrush(Color.FromRgb(byte.MaxValue, 170, 0))
		},
		{
			"7",
			Brushes.LightGray
		},
		{
			"8",
			Brushes.DarkGray
		},
		{
			"9",
			Brushes.Blue
		},
		{
			"a",
			Brushes.Lime
		},
		{
			"b",
			Brushes.Cyan
		},
		{
			"c",
			Brushes.Red
		},
		{
			"d",
			Brushes.Magenta
		},
		{
			"e",
			Brushes.Yellow
		},
		{
			"f",
			Brushes.White
		}
	};

	private static readonly Dictionary<string, Brush> _ColorMapWithWhiteBackground = new Dictionary<string, Brush>
	{
		{
			"0",
			new SolidColorBrush(Color.FromRgb(51, 51, 51))
		},
		{
			"1",
			new SolidColorBrush(Color.FromRgb(0, 48, 135))
		},
		{
			"2",
			new SolidColorBrush(Color.FromRgb(0, 128, 0))
		},
		{
			"3",
			new SolidColorBrush(Color.FromRgb(0, 122, 122))
		},
		{
			"4",
			new SolidColorBrush(Color.FromRgb(161, 0, 0))
		},
		{
			"5",
			new SolidColorBrush(Color.FromRgb(128, 0, 128))
		},
		{
			"6",
			new SolidColorBrush(Color.FromRgb(204, 112, 0))
		},
		{
			"7",
			new SolidColorBrush(Color.FromRgb(102, 102, 102))
		},
		{
			"8",
			new SolidColorBrush(Color.FromRgb(68, 68, 68))
		},
		{
			"9",
			new SolidColorBrush(Color.FromRgb(0, 68, 204))
		},
		{
			"a",
			new SolidColorBrush(Color.FromRgb(0, 153, 0))
		},
		{
			"b",
			new SolidColorBrush(Color.FromRgb(0, 161, 161))
		},
		{
			"c",
			new SolidColorBrush(Color.FromRgb(204, 0, 0))
		},
		{
			"d",
			new SolidColorBrush(Color.FromRgb(194, 0, 194))
		},
		{
			"e",
			new SolidColorBrush(Color.FromRgb(179, 160, 0))
		},
		{
			"f",
			new SolidColorBrush(Color.FromRgb(136, 136, 136))
		}
	};

	private readonly Dictionary<string, bool> _formatMap = new Dictionary<string, bool>
	{
		{ "l", true },
		{ "o", true },
		{ "n", true },
		{ "m", true },
		{ "k", true },
		{ "r", false }
	};

	private readonly List<(TextBlock TextBlock, string OriginalText)> _obfuscatedTextBlocks = new List<(TextBlock, string)>();

	private readonly Random _random = new Random();

	private const string RandomChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";

	private readonly Color _backgroundColor = Color.FromRgb(243, 246, 250);

	public MotdRenderer()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		InitializeComponent();
		DispatcherTimer val = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(20L)
		};
		val.Tick += _UpdateObfuscatedText;
		val.Start();
	}

	private void _UpdateObfuscatedText(object? sender, EventArgs e)
	{
		foreach (var obfuscatedTextBlock in _obfuscatedTextBlocks)
		{
			TextBlock item = obfuscatedTextBlock.TextBlock;
			string item2 = obfuscatedTextBlock.OriginalText;
			string text = string.Join("", from _ in Enumerable.Range(0, item2.Length)
				select "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()"[_random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()".Length)]);
			item.Text = text;
		}
	}

	private static double _GetRelativeLuminance(Color color)
	{
		double num = (double)(int)color.R / 255.0;
		double num2 = (double)(int)color.G / 255.0;
		double num3 = (double)(int)color.B / 255.0;
		double num4 = ((num <= 0.03928) ? (num / 12.92) : Math.Pow((num + 0.055) / 1.055, 2.4));
		double num5 = ((num2 <= 0.03928) ? (num2 / 12.92) : Math.Pow((num2 + 0.055) / 1.055, 2.4));
		double num6 = ((num3 <= 0.03928) ? (num3 / 12.92) : Math.Pow((num3 + 0.055) / 1.055, 2.4));
		return 0.2126 * num4 + 0.7152 * num5 + 0.0722 * num6;
	}

	private static double _GetContrastRatio(Color foreground, Color background)
	{
		double val = _GetRelativeLuminance(foreground);
		double val2 = _GetRelativeLuminance(background);
		return (Math.Max(val, val2) + 0.05) / (Math.Min(val, val2) + 0.05);
	}

	private Color _AdjustColorForContrast(Color inputColor)
	{
		if (_GetContrastRatio(inputColor, _backgroundColor) >= 4.5)
		{
			return inputColor;
		}
		double num = (double)(int)inputColor.R / 255.0;
		double num2 = (double)(int)inputColor.G / 255.0;
		double num3 = (double)(int)inputColor.B / 255.0;
		double num4 = Math.Max(Math.Max(num, num2), num3);
		double num5 = Math.Min(Math.Min(num, num2), num3);
		double num6 = (num4 + num5) / 2.0;
		double num8;
		double num7;
		if (Math.Abs(num4 - num5) < double.Epsilon)
		{
			num7 = 0.0;
			num8 = 0.0;
		}
		else
		{
			double num9 = num4 - num5;
			num8 = ((num6 > 0.5) ? (num9 / (2.0 - num4 - num5)) : (num9 / (num4 + num5)));
			double num10;
			if (Math.Abs(num4 - num) < double.Epsilon)
			{
				num10 = (num2 - num3) / num9 + ((num2 < num3) ? 6.0 : 0.0);
			}
			else
			{
				num10 = ((!(Math.Abs(num4 - num2) < double.Epsilon)) ? ((num - num2) / num9 + 4.0) : ((num3 - num) / num9 + 2.0));
			}
			num7 = num10;
			num7 /= 6.0;
		}
		double num11 = num6;
		Color color = inputColor;
		while (num11 > 0.1 && _GetContrastRatio(color, _backgroundColor) < 4.5)
		{
			num11 -= 0.05;
			double num12;
			double num13;
			double num14;
			if (num8 == 0.0)
			{
				num12 = num11;
				num13 = num11;
				num14 = num11;
			}
			else
			{
				double num15 = ((num11 < 0.5) ? (num11 * (1.0 + num8)) : (num11 + num8 - num11 * num8));
				double p = 2.0 * num11 - num15;
				num12 = _HueToRgb(p, num15, num7 + 1.0 / 3.0);
				num13 = _HueToRgb(p, num15, num7);
				num14 = _HueToRgb(p, num15, num7 - 1.0 / 3.0);
			}
			color = Color.FromRgb((byte)(num12 * 255.0), (byte)(num13 * 255.0), (byte)(num14 * 255.0));
		}
		if (!(_GetContrastRatio(color, _backgroundColor) < 4.5))
		{
			return color;
		}
		return Color.FromRgb(85, 85, 85);
	}

	private static double _HueToRgb(double p, double q, double t)
	{
		if (t < 0.0)
		{
			t++;
		}
		if (t > 1.0)
		{
			t--;
		}
		if (t < 1.0 / 6.0)
		{
			return p + (q - p) * 6.0 * t;
		}
		if (t < 0.5)
		{
			return q;
		}
		if (t < 2.0 / 3.0)
		{
			return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
		}
		return p;
	}

	public static bool TryGetColorFromCode(string code, bool isDarkMode, out string? color)
	{
		bool flag = (isDarkMode ? _ColorMapWithBlackBackground : _ColorMapWithWhiteBackground).TryGetValue(code.ToLower(), out Brush value);
		if (!flag)
		{
			color = null;
			return false;
		}
		Color color2 = ((SolidColorBrush)value).Color;
		color = $"#{color2.R:X2}{color2.G:X2}{color2.B:X2}";
		return flag;
	}

	public void RenderMotd(string motd, bool isDarkMode = true, int maxLines = int.MaxValue, double fontSize = 12.0, bool isCentered = true)
	{
		MotdCanvas.Children.Clear();
		_obfuscatedTextBlocks.Clear();
		if (motd == null)
		{
			motd = string.Empty;
		}
		Dictionary<string, Brush> dictionary = (isDarkMode ? _ColorMapWithBlackBackground : _ColorMapWithWhiteBackground);
		string motdFont = Config.Preference.MotdFont;
		FontFamily fontFamily = new FontFamily(string.IsNullOrWhiteSpace(motdFont) ? "./Resources/#PCL English, Segoe UI, Microsoft YaHei UI" : motdFont);
		double num = ((MotdCanvas.ActualWidth > 0.0) ? MotdCanvas.ActualWidth : 300.0);
		double num2 = ((MotdCanvas.ActualHeight > 0.0) ? MotdCanvas.ActualHeight : 34.0);
		double y = 10.0;
		motd = motd.Replace("\n", "\r\n");
		string[] array = motd.Split("\r\n");
		Brush color = dictionary["f"];
		bool isBold = false;
		bool isItalic = false;
		bool isUnderline = false;
		bool isStrikethrough = false;
		bool flag = false;
		int num3 = Math.Min(array.Length, maxLines);
		for (int i = 0; i < num3; i++)
		{
			string input = array[i].Trim();
			string[] array2 = RegexPatterns.MotdCode.Split(input);
			double num4 = 0.0;
			double num5 = 0.0;
			double num6 = 0.0;
			List<TextBlock> list = new List<TextBlock>();
			List<double> list2 = new List<double>();
			int num7 = -1;
			int num8 = -1;
			for (int j = 0; j < array2.Length; j++)
			{
				string text = array2[j];
				if (!string.IsNullOrEmpty(text) && (!text.StartsWith('§') || text.Length != 2) && !RegexPatterns.HexColor.IsMatch(text))
				{
					if (num7 == -1)
					{
						num7 = j;
					}
					num8 = j;
				}
			}
			for (int k = 0; k < array2.Length; k++)
			{
				string text2 = array2[k];
				string text3 = text2;
				if (k == num7)
				{
					text3 = text2.TrimStart();
				}
				else if (k == num8)
				{
					text3 = text2.TrimEnd();
				}
				if (string.IsNullOrEmpty(text3))
				{
					continue;
				}
				if (text3.StartsWith('§') && text3.Length == 2)
				{
					string text4 = text3.Substring(1).ToLower();
					if (dictionary.TryGetValue(text4, out var value))
					{
						color = value;
						isBold = false;
						isItalic = false;
						isUnderline = false;
						isStrikethrough = false;
						flag = false;
					}
					else if (_formatMap.ContainsKey(text4))
					{
						switch (text4)
						{
						case "l":
							isBold = true;
							break;
						case "o":
							isItalic = true;
							break;
						case "n":
							isUnderline = true;
							break;
						case "m":
							isStrikethrough = true;
							break;
						case "k":
							flag = true;
							break;
						case "r":
							color = dictionary["f"];
							isBold = false;
							isItalic = false;
							isUnderline = false;
							isStrikethrough = false;
							flag = false;
							break;
						}
					}
					continue;
				}
				if (RegexPatterns.HexColor.IsMatch(text3))
				{
					try
					{
						string text5 = text3.Substring(1);
						byte r = Convert.ToByte(text5.Substring(0, 2), 16);
						byte g = Convert.ToByte(text5.Substring(2, 2), 16);
						byte b = Convert.ToByte(text5.Substring(4, 2), 16);
						Color inputColor = Color.FromRgb(r, g, b);
						color = new SolidColorBrush(_AdjustColorForContrast(inputColor));
						isBold = false;
						isItalic = false;
						isUnderline = false;
						isStrikethrough = false;
						flag = false;
					}
					catch
					{
					}
					continue;
				}
				string text6 = text3;
				if (flag)
				{
					string text7 = text3;
					for (int l = 0; l < text7.Length; l++)
					{
						char c = text7[l];
						text6 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()"[_random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()".Length)].ToString();
						TextBlock textBlock = _RenderText(text6, fontFamily, fontSize, color, isBold, isItalic, isUnderline, isStrikethrough, num6, y, withClip: true, _MeasureTextWidth(c.ToString(), fontFamily, fontSize, isBold, isItalic));
						_obfuscatedTextBlocks.Add((textBlock, c.ToString()));
						list.Add(textBlock);
						list2.Add(num6);
						num6 += _MeasureTextWidth(c.ToString(), fontFamily, fontSize, isBold, isItalic);
					}
				}
				else
				{
					TextBlock textBlock = _RenderText(text6, fontFamily, fontSize, color, isBold, isItalic, isUnderline, isStrikethrough, num6, y);
					list.Add(textBlock);
					list2.Add(num6);
				}
				if (!flag)
				{
					num6 += _MeasureTextWidth(text3, fontFamily, fontSize, isBold, isItalic);
				}
				double num9 = _MeasureTextHeight(text3, fontFamily, fontSize, isBold, isItalic);
				num5 = ((num9 > num5) ? num9 : num5);
				num4 = num6;
			}
			if (isCentered)
			{
				double num10 = (num - num4) / 2.0;
				for (int m = 0; m < list.Count; m++)
				{
					Canvas.SetLeft(list[m], list2[m] + num10);
				}
			}
			double length;
			if (num3 == 1)
			{
				length = (num2 - num5) / 2.0;
			}
			else
			{
				double num11 = (double)num3 * num5;
				length = (num2 - num11) / 2.0 + (double)i * num5;
			}
			foreach (TextBlock item in list)
			{
				Canvas.SetTop(item, length);
			}
		}
	}

	private TextBlock _RenderText(string text, FontFamily fontFamily, double fontSize, Brush color, bool isBold, bool isItalic, bool isUnderline, bool isStrikethrough, double x, double y, bool withClip = false, double clipWidth = 15.0)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontFamily = fontFamily,
			FontSize = fontSize,
			Foreground = color,
			FontWeight = (isBold ? FontWeights.Bold : FontWeights.Normal),
			FontStyle = (isItalic ? FontStyles.Italic : FontStyles.Normal)
		};
		if (isUnderline | isStrikethrough)
		{
			textBlock.TextDecorations = new TextDecorationCollection();
			if (isUnderline)
			{
				textBlock.TextDecorations.Add(TextDecorations.Underline);
			}
			if (isStrikethrough)
			{
				textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
			}
		}
		if (withClip)
		{
			RectangleGeometry clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, clipWidth, _MeasureTextHeight(text, fontFamily, fontSize, isBold, isItalic))
			};
			textBlock.Clip = clip;
		}
		Canvas.SetLeft(textBlock, x);
		Canvas.SetTop(textBlock, y);
		if (Content is Canvas canvas)
		{
			canvas.Children.Add(textBlock);
		}
		return textBlock;
	}

	private static FormattedText _CreateFormattedText(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic)
	{
		return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(fontFamily, isItalic ? FontStyles.Italic : FontStyles.Normal, isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), fontSize, Brushes.White, 96.0);
	}

	private static double _MeasureTextWidth(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic)
	{
		return _CreateFormattedText(text, fontFamily, fontSize, isBold, isItalic).WidthIncludingTrailingWhitespace;
	}

	private static double _MeasureTextHeight(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic)
	{
		return _CreateFormattedText(text, fontFamily, fontSize, isBold, isItalic).Height;
	}

	public void RenderCanvas()
	{
		MotdCanvas.UpdateLayout();
		foreach (var obfuscatedTextBlock in _obfuscatedTextBlocks)
		{
			TextBlock item = obfuscatedTextBlock.TextBlock;
			string item2 = obfuscatedTextBlock.OriginalText;
			item.Text = string.Join("", from _ in Enumerable.Range(0, item2.Length)
				select "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()"[_random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()".Length)]);
		}
		new RenderTargetBitmap((int)MotdCanvas.Width, (int)MotdCanvas.Height, 96.0, 96.0, PixelFormats.Pbgra32).Render(MotdCanvas);
	}

	public void ClearCanvas()
	{
		MotdCanvas.Children.Clear();
		_obfuscatedTextBlocks.Clear();
	}
}

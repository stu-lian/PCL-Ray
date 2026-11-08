using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageToolsColorCode : MyPageRight, IComponentConnector
{
	private static readonly (string Code, string Label, Color Color)[] Palette =
	{
		("0", "黑色", Color.FromRgb(0, 0, 0)),
		("1", "深蓝", Color.FromRgb(0, 0, 170)),
		("2", "深绿", Color.FromRgb(0, 170, 0)),
		("3", "湖蓝", Color.FromRgb(0, 170, 170)),
		("4", "红色", Color.FromRgb(170, 0, 0)),
		("5", "紫色", Color.FromRgb(170, 0, 170)),
		("6", "金色", Color.FromRgb(255, 170, 0)),
		("7", "灰色", Color.FromRgb(170, 170, 170)),
		("8", "深灰", Color.FromRgb(85, 85, 85)),
		("9", "蓝色", Color.FromRgb(85, 85, 255)),
		("a", "绿色", Color.FromRgb(85, 255, 85)),
		("b", "青色", Color.FromRgb(85, 255, 255)),
		("c", "红色", Color.FromRgb(255, 85, 85)),
		("d", "粉色", Color.FromRgb(255, 85, 255)),
		("e", "黄色", Color.FromRgb(255, 255, 85)),
		("f", "白色", Color.FromRgb(255, 255, 255))
	};

	public PageToolsColorCode()
	{
		InitializeComponent();
		ApplyPalette();
		TextInput_TextChanged(null, null);
	}

	private void ApplyPalette()
	{
		ApplySample(Sample0, 0, "0", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample1, 1, "1", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample2, 2, "2", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample3, 3, "3", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample4, 4, "4", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample5, 5, "5", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample6, 6, "6", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample7, 7, "7", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample8, 8, "8", FontStyles.Normal, FontWeights.Normal);
		ApplySample(Sample9, 9, "9", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleA, 10, "a", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleB, 11, "b", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleC, 12, "c", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleD, 13, "d", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleE, 14, "e", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleF, 15, "f", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleK, -1, "k", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleL, -2, "l", FontStyles.Normal, FontWeights.Bold);
		ApplySample(SampleM, -3, "m", FontStyles.Normal, FontWeights.Normal);
		ApplySample(SampleN, -4, "n", FontStyles.Normal, FontWeights.Normal);
	}

	private void ApplySample(TextBlock target, int paletteIndex, string code, FontStyle style, FontWeight weight)
	{
		if (target == null)
		{
			return;
		}
		Color color = new Color();
		if (paletteIndex >= 0)
		{
			color = Palette[paletteIndex].Color;
		}
		else if (paletteIndex == -1)
		{
			color = Colors.Magenta;
		}
		else
		{
			color = Colors.Gray;
		}
		target.Foreground = new SolidColorBrush(color);
		target.FontStyle = style;
		target.FontWeight = weight;
		target.Cursor = System.Windows.Input.Cursors.Hand;
		target.ToolTip = Lang.Text("Tools.ColorCode.ClickToInsert");
		target.MouseLeftButtonUp += (object _, System.Windows.Input.MouseButtonEventArgs _) =>
		{
			InsertCode(code);
		};
	}

	private void InsertCode(string code)
	{
		TextInput.Text = (TextInput.Text ?? "") + "&" + code;
		TextInput.CaretIndex = TextInput.Text.Length;
		UpdatePreview();
	}

	private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdatePreview();
	}

	private void UpdatePreview()
	{
		LabPreview.Inlines.Clear();
		string input = TextInput.Text ?? "";
		if (string.IsNullOrEmpty(input))
		{
			LabPreview.Inlines.Add(new Run(Lang.Text("Tools.ColorCode.PreviewHint"))
			{
				Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
			});
			return;
		}
		Color currentColor = Color.FromRgb(255, 255, 255);
		FontWeight currentWeight = FontWeights.Normal;
		FontStyle currentStyle = FontStyles.Normal;
		bool strike = false;
		bool underline = false;
		string text = input;
		int index = 0;
		while (index < text.Length)
		{
			if (text[index] != '&' || index + 1 >= text.Length)
			{
				int end = text.IndexOf('&', index + 1);
				if (end < 0)
				{
					end = text.Length;
				}
				AppendPreviewRun(text.Substring(index, end - index), currentColor, currentWeight, currentStyle, strike, underline);
				index = end;
				continue;
			}
			char code = text[index + 1];
			ApplyMinecraftCode(code, ref currentColor, ref currentWeight, ref currentStyle, ref strike, ref underline);
			index += 2;
		}
	}

	private void AppendPreviewRun(string text, Color color, FontWeight weight, FontStyle style, bool strike, bool underline)
	{
		Run run = new Run(text)
		{
			Foreground = new SolidColorBrush(color),
			FontWeight = weight,
			FontStyle = style
		};
		if (strike)
		{
			run.TextDecorations.Add(TextDecorations.Strikethrough);
		}
		if (underline)
		{
			run.TextDecorations.Add(TextDecorations.Underline);
		}
		LabPreview.Inlines.Add(run);
	}

	private void ApplyMinecraftCode(char code, ref Color color, ref FontWeight weight, ref FontStyle style, ref bool strike, ref bool underline)
	{
		switch (code)
		{
		case '0': color = Color.FromRgb(0, 0, 0); break;
		case '1': color = Color.FromRgb(0, 0, 170); break;
		case '2': color = Color.FromRgb(0, 170, 0); break;
		case '3': color = Color.FromRgb(0, 170, 170); break;
		case '4': color = Color.FromRgb(170, 0, 0); break;
		case '5': color = Color.FromRgb(170, 0, 170); break;
		case '6': color = Color.FromRgb(255, 170, 0); break;
		case '7': color = Color.FromRgb(170, 170, 170); break;
		case '8': color = Color.FromRgb(85, 85, 85); break;
		case '9': color = Color.FromRgb(85, 85, 255); break;
		case 'a': color = Color.FromRgb(85, 255, 85); break;
		case 'b': color = Color.FromRgb(85, 255, 255); break;
		case 'c': color = Color.FromRgb(255, 85, 85); break;
		case 'd': color = Color.FromRgb(255, 85, 255); break;
		case 'e': color = Color.FromRgb(255, 255, 85); break;
		case 'f': color = Color.FromRgb(255, 255, 255); break;
		case 'k': break;
		case 'l': weight = FontWeights.Bold; break;
		case 'm': strike = true; break;
		case 'n': underline = true; break;
		case 'o': style = FontStyles.Italic; break;
		case 'r':
			color = Color.FromRgb(255, 255, 255);
			weight = FontWeights.Normal;
			style = FontStyles.Normal;
			strike = false;
			underline = false;
			break;
		}
	}

	private string ApplyCodes(string input)
	{
		return input;
	}

	private void BtnCopy_Click(object sender, MouseButtonEventArgs e)
	{
		string text = TextInput.Text?.Trim() ?? "";
		if (!string.IsNullOrEmpty(text))
		{
			ModBase.ClipboardSet(text);
		}
	}
}
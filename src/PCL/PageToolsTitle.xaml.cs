using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL;

public partial class PageToolsTitle : MyPageRight, IComponentConnector
{
	private bool _ready;

	public PageToolsTitle()
	{
		InitializeComponent();
		foreach ((string key, string chinese, string hex) in McToolBuilders.TitleColors)
		{
			ComboTitleColor.Items.Add(new ComboBoxItem { Content = chinese, Tag = key });
			ComboSubtitleColor.Items.Add(new ComboBoxItem { Content = chinese, Tag = key });
			ComboActionColor.Items.Add(new ComboBoxItem { Content = chinese, Tag = key });
		}
		ComboTitleColor.SelectedIndex = 0;
		ComboSubtitleColor.SelectedIndex = 0;
		ComboActionColor.SelectedIndex = 0;
		_ready = true;
		Refresh();
	}

	private string ColorOf(MyComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Tag as string ?? "white";

	private McToolBuilders.TitleSegment BuildSegment(MyTextBox text, MyComboBox color, MyCheckBox bold, MyCheckBox italic) => new McToolBuilders.TitleSegment
	{
		Text = text.Text ?? "",
		Color = ColorOf(color),
		Bold = bold.Checked == true,
		Italic = italic.Checked == true
	};

	private static int ParseInt(string? text, int fallback)
	{
		return int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(value, 0) : fallback;
	}

	private void Option_Changed(object sender, TextChangedEventArgs e) => RefreshIfReady();

	private void Option_Changed(object sender, SelectionChangedEventArgs e) => RefreshIfReady();

	private void Option_Change(object sender, bool user) => RefreshIfReady();

	private void RefreshIfReady()
	{
		if (_ready)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		McToolBuilders.TitleSegment title = BuildSegment(TextTitle, ComboTitleColor, CheckTitleBold, CheckTitleItalic);
		McToolBuilders.TitleSegment subtitle = BuildSegment(TextSubtitle, ComboSubtitleColor, CheckSubtitleBold, CheckSubtitleItalic);
		McToolBuilders.TitleSegment action = BuildSegment(TextActionBar, ComboActionColor, CheckActionBold, CheckActionItalic);
		List<string> commands = McToolBuilders.BuildTitleCommands(
			TextTarget.Text,
			title,
			subtitle,
			action,
			ParseInt(TextFadeIn.Text, 10),
			ParseInt(TextStay.Text, 70),
			ParseInt(TextFadeOut.Text, 20));
		TextCommands.Text = string.Join(Environment.NewLine, commands);
		UpdatePreview(PreviewTitle, title, 20);
		UpdatePreview(PreviewSubtitle, subtitle, 14);
		UpdatePreview(PreviewAction, action, 13);
	}

	private void UpdatePreview(TextBlock target, McToolBuilders.TitleSegment segment, double fontSize)
	{
		target.Text = segment.Text;
		target.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(McToolBuilders.TitleColorHex(segment.Color)));
		target.FontWeight = segment.Bold ? FontWeights.Bold : FontWeights.Normal;
		target.FontStyle = segment.Italic ? FontStyles.Italic : FontStyles.Normal;
		target.FontSize = fontSize;
		target.Visibility = string.IsNullOrWhiteSpace(segment.Text) ? Visibility.Collapsed : Visibility.Visible;
	}

	private void BtnCopy_Click(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(TextCommands.Text))
		{
			ModBase.ClipboardSet(TextCommands.Text);
			HintService.Hint("Title 指令已复制");
		}
	}
}

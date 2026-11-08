using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;

namespace PCL;

public partial class MyMsgAuthServer : Grid, IComponentConnector
{
	private readonly ModMain.MyMsgBoxConverter _converter;

	private readonly int _uuid = ModBase.GetUuid();

	public MyMsgAuthServer(ModMain.MyMsgBoxConverter converter)
	{
		InitializeComponent();
		_converter = converter;
		LabTitle.Text = converter.Title;
		Btn1.Text = converter.Button1;
		Btn2.Text = converter.Button2;
		ComboServer.Text = converter.AuthServerDefault;
		foreach (KeyValuePair<string, string> item in converter.AuthServerPresets ?? new Dictionary<string, string>())
		{
			ComboServer.Items.Add(new MyComboBoxItem
			{
				Content = item.Key,
				Tag = item.Value
			});
		}
		ComboServer.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			if (ComboServer.SelectedItem is MyComboBoxItem myComboBoxItem)
			{
				string value = myComboBoxItem.Tag?.ToString() ?? string.Empty;
				((DispatcherObject)this).Dispatcher.BeginInvoke((DispatcherPriority)5, (Delegate)(Action)(() =>
				{
					ComboServer.SelectedItem = null;
					ComboServer.Text = value;
				}));
			}
		};
		ShapeLine.StrokeThickness = ModBase.GetWPFSize(1.0);
		LabTitle.MouseLeftButtonDown += Drag;
		PanBorder.MouseLeftButtonDown += Drag;
		Loaded += Load;
	}

	private void Load(object sender, EventArgs e)
	{
		Opacity = 0.0;
		ModAnimation.AniStart(new ModAnimation.AniData[4]
		{
			ModAnimation.AaColor((FrameworkElement)(object)ModMain.frmMain.PanMsgBackground, Border.BackgroundProperty, new ModBase.MyColor(90.0, 0.0, 0.0, 0.0) - ((Border)(object)ModMain.frmMain.PanMsgBackground).Background, 200),
			ModAnimation.AaOpacity(this, 1.0, 120, 60),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformPos.Y += (double)i;
			}, 0.0 - TransformPos.Y, 300, 60, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformRotate.Angle += (double)i;
			}, 0.0 - TransformRotate.Angle, 300, 60, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
		}, "MyMsgBox " + _uuid);
	}

	private void Btn1_Click(object sender, MouseButtonEventArgs e)
	{
		string text = ComboServer.Text?.Trim();
		if (string.IsNullOrWhiteSpace(text) || !StringExtension.IsMatch(text, RegexPatterns.HttpUri))
		{
			HintService.Hint(Lang.Text("Launch.Account.Auth.InvalidServer"), HintType.Error);
			return;
		}
		_converter.IsExited = true;
		_converter.Result = text;
		_Close();
	}

	private void Btn2_Click(object sender, MouseButtonEventArgs e)
	{
		if (!_converter.IsExited)
		{
			_converter.IsExited = true;
			_converter.Result = null;
			_Close();
		}
	}

	private void _Close()
	{
		_converter.WaitFrame.Continue = false;
		ComponentDispatcher.PopModal();
		ModAnimation.AniStart(new ModAnimation.AniData[5]
		{
			ModAnimation.AaCode(() =>
			{
				if (!ModMain.WaitingMyMsgBox.Any())
				{
					ModAnimation.AniStart(ModAnimation.AaColor((FrameworkElement)(object)ModMain.frmMain.PanMsgBackground, Border.BackgroundProperty, new ModBase.MyColor(0.0, 0.0, 0.0, 0.0) - ((Border)(object)ModMain.frmMain.PanMsgBackground).Background, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
				}
			}, 30),
			ModAnimation.AaOpacity(this, 0.0 - Opacity, 80, 20),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformPos.Y += (double)i;
			}, 20.0 - TransformPos.Y, 150, 0, new ModAnimation.AniEaseOutFluent()),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformRotate.Angle += (double)i;
			}, 6.0 - TransformRotate.Angle, 150, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaCode(() =>
			{
				((Grid)(object)Parent).Children.Remove(this);
			}, 0, after: true)
		}, "MyMsgBox " + _uuid);
	}

	private void Drag(object sender, MouseButtonEventArgs e)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point position = e.GetPosition(ShapeLine);
				if (position.Y <= 2.0)
				{
					ModMain.frmMain.DragMove();
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "拖拽移动失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
		}
	}

}

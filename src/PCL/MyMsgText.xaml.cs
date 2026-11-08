using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class MyMsgText : Grid, IComponentConnector
{
	private readonly ModMain.MyMsgBoxConverter myConverter;

	private readonly int uuid = ModBase.GetUuid();

	public MyMsgText(ModMain.MyMsgBoxConverter converter)
	{
		try
		{
			InitializeComponent();
			AppendUniqueNameSuffix(Btn1);
			AppendUniqueNameSuffix(Btn2);
			AppendUniqueNameSuffix(Btn3);
			myConverter = converter;
			LabTitle.Text = converter.Title;
			LabCaption.Text = converter.Text;
			ConfigurePrimaryButton(converter.Button1, converter.IsWarn);
			ConfigureSecondaryButton(Btn2, converter.Button2);
			ConfigureSecondaryButton(Btn3, converter.Button3);
			ShapeLine.StrokeThickness = ModBase.GetWPFSize(1.0);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "普通弹窗初始化失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
		}
		Loaded += Load;
	}

	private void AppendUniqueNameSuffix(FrameworkElement element)
	{
		element.Name += ModBase.GetUuid();
	}

	private void ConfigurePrimaryButton(string text, bool isWarn)
	{
		Btn1.Text = text;
		if (isWarn)
		{
			Btn1.ColorType = MyButton.ColorState.Red;
			LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight");
		}
	}

	private static void ConfigureSecondaryButton(MyButton button, string text)
	{
		button.Text = text;
		button.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void Load(object sender, RoutedEventArgs e)
	{
		try
		{
			if (Btn2.IsVisible && Btn1.ColorType != MyButton.ColorState.Red)
			{
				Btn1.ColorType = MyButton.ColorState.Highlight;
			}
			Btn1.Focus();
			Opacity = 0.0;
			ModAnimation.AniStart(ModAnimation.AaColor((FrameworkElement)(object)ModMain.frmMain.PanMsgBackground, Border.BackgroundProperty, (myConverter.IsWarn ? new ModBase.MyColor(140.0, 80.0, 0.0, 0.0) : new ModBase.MyColor(90.0, 0.0, 0.0, 0.0)) - ((Border)(object)ModMain.frmMain.PanMsgBackground).Background, 200), "PanMsgBackground Background");
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaOpacity(this, 1.0, 120, 60),
				ModAnimation.AaDouble((object? i) =>
				{
					TransformPos.Y += (double)i;
				}, 0.0 - TransformPos.Y, 300, 60, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaDouble((object? i) =>
				{
					TransformRotate.Angle += (double)i;
				}, 0.0 - TransformRotate.Angle, 300, 60, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
			}, "MyMsgBox " + uuid);
			ModBase.Log("[Control] 普通弹窗：" + LabTitle.Text + "\r\n" + LabCaption.Text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "普通弹窗加载失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
		}
	}

	private void Close()
	{
		if (myConverter.ForceWait || !string.IsNullOrEmpty(myConverter.Button2))
		{
			myConverter.WaitFrame.Continue = false;
		}
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
		}, "MyMsgBox " + uuid);
	}

	public void Btn1_Click(object? sender = null, MouseButtonEventArgs? e = null)
	{
		if (!myConverter.IsExited)
		{
			if (myConverter.Button1Action != null)
			{
				myConverter.Button1Action();
				return;
			}
			myConverter.IsExited = true;
			myConverter.Result = 1;
			Close();
		}
	}

	public void Btn2_Click(object sender, MouseButtonEventArgs e)
	{
		if (!myConverter.IsExited)
		{
			if (myConverter.Button2Action != null)
			{
				myConverter.Button2Action();
				return;
			}
			myConverter.IsExited = true;
			myConverter.Result = 2;
			Close();
		}
	}

	public void Btn3_Click(object sender, MouseButtonEventArgs e)
	{
		if (!myConverter.IsExited)
		{
			if (myConverter.Button3Action != null)
			{
				myConverter.Button3Action();
				return;
			}
			myConverter.IsExited = true;
			myConverter.Result = 3;
			Close();
		}
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

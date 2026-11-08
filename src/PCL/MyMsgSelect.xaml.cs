using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class MyMsgSelect : Grid, IComponentConnector
{
	private readonly ModMain.MyMsgBoxConverter myConverter;

	private readonly int uuid = ModBase.GetUuid();

	private int selectedIndex = -1;

	public MyMsgSelect(ModMain.MyMsgBoxConverter converter)
	{
		try
		{
			InitializeComponent();
			AppendUniqueNameSuffix(Btn1);
			AppendUniqueNameSuffix(Btn2);
			myConverter = converter;
			LabTitle.Text = converter.Title;
			ConfigurePrimaryButton(converter.Button1, converter.IsWarn);
			ConfigureSecondaryButton(converter.Button2);
			ShapeLine.StrokeThickness = ModBase.GetWPFSize(1.0);
			InitializeSelectionList(converter.Content);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "选择弹窗初始化失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
		}
		Loaded += Load;
		Btn1.Click += Btn1_Click;
		Btn2.Click += Btn2_Click;
		LabTitle.MouseLeftButtonDown += Drag;
		PanBorder.MouseLeftButtonDown += Drag;
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

	private void ConfigureSecondaryButton(string text)
	{
		Btn2.Text = text;
		Btn2.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void InitializeSelectionList(object content)
	{
		Btn1.IsEnabled = false;
		foreach (FrameworkElement item in (IEnumerable)content)
		{
			if (MyVirtualizingElement.TryInit(item) is IMyRadio myRadio)
			{
				PanSelection.Children.Add((UIElement)myRadio);
				myRadio.Check += (object sender, ModBase.RouteEventArgs e) =>
				{
					OnChecked((IMyRadio)sender, e);
				};
				myRadio.Changed += (object _, ModBase.RouteEventArgs _) =>
				{
					_UpdateCheckedState();
				};
				if (myRadio is MyListItem myListItem)
				{
					myListItem.Type = (myConverter.MultiSelect ? MyListItem.CheckType.CheckBox : MyListItem.CheckType.RadioBox);
					myListItem.MinHeight = 24.0;
				}
				else if (myRadio is MyRadioBox myRadioBox)
				{
					myRadioBox.MinHeight = 24.0;
				}
			}
		}
	}

	private void Load(object sender, EventArgs e)
	{
		try
		{
			if (Btn2.IsVisible && Btn1.ColorType != MyButton.ColorState.Red)
			{
				Btn1.ColorType = MyButton.ColorState.Highlight;
			}
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
			ModBase.Log("[Control] 选择弹窗：" + LabTitle.Text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "选择弹窗加载失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
		}
	}

	private void Close()
	{
		myConverter.WaitFrame.Continue = false;
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

	public void Btn1_Click(object sender, MouseButtonEventArgs e)
	{
		if (myConverter.IsExited)
		{
			return;
		}
		if (myConverter.MultiSelect)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < PanSelection.Children.Count; i++)
			{
				if (PanSelection.Children[i] is MyListItem { Checked: not false })
				{
					list.Add(i);
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			myConverter.IsExited = true;
			myConverter.Result = list;
		}
		else
		{
			if (selectedIndex == -1)
			{
				return;
			}
			myConverter.IsExited = true;
			myConverter.Result = selectedIndex;
		}
		Close();
	}

	public void Btn2_Click(object sender, MouseButtonEventArgs e)
	{
		if (!myConverter.IsExited)
		{
			myConverter.IsExited = true;
			myConverter.Result = null;
			Close();
		}
	}

	private void OnChecked(IMyRadio sender, EventArgs e)
	{
		if (!myConverter.MultiSelect)
		{
			Btn1.IsEnabled = true;
			selectedIndex = PanSelection.Children.IndexOf((UIElement)sender);
		}
	}

	private void _UpdateCheckedState()
	{
		if (!myConverter.MultiSelect)
		{
			return;
		}
		int num = 0;
		foreach (object child in PanSelection.Children)
		{
			if (child is MyListItem { Checked: not false })
			{
				num++;
			}
		}
		Btn1.IsEnabled = num > 0;
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

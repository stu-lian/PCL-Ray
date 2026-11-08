using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCL.Core.UI.Controls;

namespace PCL;

public class MyScrollViewer : ScrollViewer
{
	private double realOffset;

	public MyScrollBar scrollBar;

	public double DeltaMult { get; set; } = 1.0;

	/// <summary>
	/// 嵌套滚动区域（如工具页里的小列表）优先处理滚轮：只要自己还能滚，就不把滚轮交给外层页面。
	/// </summary>
	public bool WheelPriority { get; set; }

	public MyScrollViewer()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected Obj, but got Unknown
		PreviewMouseWheel += MyScrollViewer_PreviewMouseWheel;
		ScrollChanged += MyScrollViewer_ScrollChanged;
		IsVisibleChanged += MyScrollViewer_IsVisibleChanged;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Load();
		};
		PreviewGotKeyboardFocus += MyScrollViewer_PreviewGotKeyboardFocus;
	}

	private void MyScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (e.Delta == 0 || ScrollableHeight <= 0.0)
		{
			return;
		}
		if (WheelPriority && HasNestedScrollUnderPointer(e.Delta))
		{
			// 鼠标停在内层可滚动列表（如头颅选择）上时，滚轮交给内层处理
			return;
		}
		object source = e.Source;
		if (Content is FrameworkElement { TemplatedParent: null })
		{
			if (source is ComboBox comboBox)
			{
				if (comboBox.IsDropDownOpen)
				{
					return;
				}
			}
			else if (source is TextBox textBox)
			{
				if (textBox.AcceptsReturn)
				{
					return;
				}
			}
			else if (source is ComboBoxItem || source is CheckBox)
			{
				return;
			}
		}
		e.Handled = true;
		PerformVerticalOffsetDelta(-e.Delta);
		Tooltip.Dismiss();
	}

	/// <summary>判断鼠标位置下方是否存在还能继续滚动的内层滚动区域。</summary>
	private bool HasNestedScrollUnderPointer(double delta)
	{
		if (!IsMouseOver)
		{
			return false;
		}
		DependencyObject? hit = InputHitTest(Mouse.GetPosition(this)) as DependencyObject;
		if (hit == null)
		{
			return false;
		}
		// 从命中元素向外找：只要遇到“不是自己、且还能朝该方向滚动”的 ScrollViewer，就让给它
		while (hit != null && !ReferenceEquals(hit, this))
		{
			if (hit is ScrollViewer inner && inner.ScrollableHeight > 0.5)
			{
				bool atTop = inner.VerticalOffset <= 0.5;
				bool atBottom = inner.VerticalOffset >= inner.ScrollableHeight - 0.5;
				if ((delta > 0 && !atTop) || (delta < 0 && !atBottom))
				{
					return true;
				}
			}
			hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
		}
		return false;
	}

	public void PerformVerticalOffsetDelta(double delta)
	{
		ModAnimation.AniStart(ModAnimation.AaDouble((object? animDelta) =>
		{
			realOffset = ModBase.MathClamp(realOffset + (double)animDelta, 0.0, ExtentHeight - ActualHeight);
			ScrollToVerticalOffset(realOffset);
		}, delta * DeltaMult, 300, 0, new ModAnimation.AniEaseOutFluent((ModAnimation.AniEasePower)6)));
	}

	private void MyScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		realOffset = VerticalOffset;
		if (ModMain.frmMain != null && (e.VerticalChange != 0.0 || e.ViewportHeightChange != 0.0))
		{
			ModMain.frmMain.BtnExtraBack.ShowRefresh();
		}
	}

	private void MyScrollViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		ModMain.frmMain.BtnExtraBack.ShowRefresh();
	}

	private void Load()
	{
		scrollBar = (MyScrollBar)(object)GetTemplateChild("PART_VerticalScrollBar");
	}

	private void MyScrollViewer_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (e.NewFocus is MySlider)
		{
			e.Handled = true;
		}
	}
}

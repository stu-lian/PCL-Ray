using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.App;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

public class MyMenuItem : MenuItem
{
	private const int AnimationTimeIn = 100;

	private const int AnimationTimeOut = 200;

	public static readonly DependencyProperty SvgIconProperty = DependencyProperty.Register("SvgIcon", typeof(string), typeof(MyMenuItem), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)OnSvgIconChanged));

	private SvgIcon? _svgIconControl;

	private string _colorName;

	public int Uuid = ModBase.GetUuid();

	public string SvgIcon
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(SvgIconProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SvgIconProperty, (object)value);
		}
	}

	public MyMenuItem()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected Obj, but got Unknown
		Loaded += MyMenuItem_Loaded;
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
	}

	private static void OnSvgIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is MyMenuItem { IsLoaded: not false } myMenuItem)
		{
			myMenuItem.UpdateTemplateIcon();
		}
	}

	private (string BackName, string ForeName, int Time) GetVisualState()
	{
		if (!IsEnabled)
		{
			return (BackName: "ColorBrushTransparent", ForeName: "ColorBrushGray5", Time: 200);
		}
		if (IsMouseOver)
		{
			return (BackName: "ColorBrush6", ForeName: "ColorBrush2", Time: 100);
		}
		return (BackName: "ColorBrushTransparent", ForeName: "ColorBrush1", Time: 200);
	}

	private void MyMenuItem_Loaded(object sender, RoutedEventArgs e)
	{
		UpdateTemplateIcon();
		((ContextMenu)(object)Parent).Opacity = (double)Config.Preference.Theme.WindowOpacity / 1000.0 + 0.4;
	}

	private void UpdateTemplateIcon()
	{
		Path path = (Path)(object)GetTemplateChild("Icon");
		if (path == null)
		{
			return;
		}
		if (SvgIconControlHelper.HasSvgIcon(SvgIcon))
		{
			path.Visibility = Visibility.Collapsed;
			EnsureSvgIconControl(path);
			_svgIconControl.Icon = SvgIcon;
			((UIElement)(object)_svgIconControl).Visibility = Visibility.Visible;
			return;
		}
		SvgIcon? svgIconControl = _svgIconControl;
		if (svgIconControl != null)
		{
			((UIElement)(object)svgIconControl).Visibility = Visibility.Collapsed;
		}
		if (Icon != null)
		{
			path.Visibility = Visibility.Visible;
			path.Data = (Geometry)new GeometryConverter().ConvertFromString(Icon.ToString());
		}
	}

	private void EnsureSvgIconControl(Path iconControl)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected Obj, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected Obj, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected Obj, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected Obj, but got Unknown
		//IL_0054: Expected Obj, but got Unknown
		if (_svgIconControl == null)
		{
			SvgIcon val = new SvgIcon();
			((FrameworkElement)val).VerticalAlignment = VerticalAlignment.Center;
			val.Stretch = Stretch.Uniform;
			((FrameworkElement)val).Margin = iconControl.Margin;
			((FrameworkElement)val).Height = iconControl.Height;
			((FrameworkElement)val).Width = iconControl.Width;
			((UIElement)val).IsHitTestVisible = false;
			((UIElement)val).Visibility = Visibility.Collapsed;
			_svgIconControl = val;
			((FrameworkElement)(object)_svgIconControl).SetBinding(PCL.Core.UI.Controls.SvgIcon.SvgIcon.IconBrushProperty, (BindingBase)new Binding("Foreground")
			{
				Source = this
			});
			if (VisualTreeHelper.GetParent((DependencyObject)(object)iconControl) is Grid grid)
			{
				Grid.SetColumn((UIElement)(object)_svgIconControl, Grid.GetColumn(iconControl));
				Grid.SetRow((UIElement)(object)_svgIconControl, Grid.GetRow(iconControl));
				grid.Children.Add((UIElement)(object)_svgIconControl);
			}
		}
	}

	private void RefreshColor()
	{
		var (text, text2, time) = GetVisualState();
		if (!((_colorName ?? "") == (text ?? "")))
		{
			_colorName = text;
			if (IsLoaded && ModAnimation.AniControlEnabled == 0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(this, Control.BackgroundProperty, text, time),
					ModAnimation.AaColor(this, Control.ForegroundProperty, text2, time)
				}, "MyMenuItem Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MyMenuItem Color " + Uuid);
				SetResourceReference(Control.BackgroundProperty, text);
				SetResourceReference(Control.ForegroundProperty, text2);
			}
		}
	}

	private void MyMenuItem_Click(object sender, RoutedEventArgs e)
	{
		ModMain.RaiseCustomEvent((DependencyObject)(object)this);
	}

	static MyMenuItem()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected Obj, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
	}
}

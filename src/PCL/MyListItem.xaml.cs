using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyListItem : Grid, IMyRadio, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	public delegate void LogoClickEventHandler(object sender, MouseButtonEventArgs e);

	public enum CheckType
	{
		None,
		Clickable,
		RadioBox,
		CheckBox
	}

	public bool isMouseOverAnimationEnabled = true;

	private string stateLast;

	private Border _RectBack;

	public FrameworkElement buttonStack;

	public FrameworkElement pathLogo;

	public Border rectCheck;

	public int Uuid = ModBase.GetUuid();

	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(MyListItem));

	public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(MyListItem), new PropertyMetadata((object)14.0));

	public static readonly DependencyProperty InfoProperty = DependencyProperty.Register("Info", typeof(string), typeof(MyListItem), new PropertyMetadata((object)"", (PropertyChangedCallback)OnInfoChanged));

	public static readonly DependencyProperty LogoProperty = DependencyProperty.Register("Logo", typeof(string), typeof(MyListItem), new PropertyMetadata((object)"", (PropertyChangedCallback)OnLogoChanged));

	public static readonly DependencyProperty SvgIconProperty = DependencyProperty.Register("SvgIcon", typeof(string), typeof(MyListItem), new PropertyMetadata((object)"", (PropertyChangedCallback)OnSvgIconChanged));

	private bool isLogoDown;

	private CheckType _Type;

	private bool _Checked;

	public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(MyListItem), new PropertyMetadata(ThemeManager.AppResources["ColorBrush1"]));

	private bool _canvasClipHandlerAdded;

	private bool isMouseDown;

	public object tag { get; set; }

	public Border RectBack
	{
		get
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			if (_RectBack == null)
			{
				Border border = new Border
				{
					Name = "RectBack",
					CornerRadius = new CornerRadius((IsScaleAnimationEnabled || Height > 40.0) ? 6 : 0),
					RenderTransform = (IsScaleAnimationEnabled ? new ScaleTransform(0.8, 0.8) : null),
					RenderTransformOrigin = new Point(0.5, 0.5),
					BorderThickness = new Thickness(ModBase.GetWPFSize(1.0)),
					SnapsToDevicePixels = true,
					IsHitTestVisible = false,
					Opacity = 0.0
				};
				border.SetResourceReference(Border.BackgroundProperty, "ColorBrush7");
				border.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6");
				Grid.SetColumnSpan(border, 999);
				Grid.SetRowSpan(border, 999);
				Children.Insert(0, border);
				_RectBack = border;
			}
			return _RectBack;
		}
	}

	public StackPanel PanTags
	{
		get
		{
			if (field != null)
			{
				return field;
			}
			StackPanel stackPanel = new StackPanel
			{
				IsHitTestVisible = false,
				Orientation = Orientation.Horizontal,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(3.5, 0.0, -3.0, 0.0)
			};
			Grid.SetColumn(stackPanel, 3);
			Grid.SetRow(stackPanel, 2);
			PanBack.Children.Add(stackPanel);
			field = stackPanel;
			return field;
		}
	}

	public object Tags
	{
		set
		{
			List<string> list = new List<string>();
			if (value is string text)
			{
				list = text.Split("|").ToList();
			}
			if (value is List<string>)
			{
				list = (List<string>)value;
			}
			PanTags.Children.Clear();
			PanTags.Visibility = ((!list.Any()) ? Visibility.Collapsed : Visibility.Visible);
			foreach (string item in list)
			{
				Border border = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(17, 0, 0, 0)),
					Padding = new Thickness(3.0, 1.0, 3.0, 1.0),
					CornerRadius = new CornerRadius(3.0),
					Margin = new Thickness(0.0, 0.0, 3.0, 0.0),
					SnapsToDevicePixels = true,
					UseLayoutRounding = false
				};
				TextBlock child = new TextBlock
				{
					Text = item,
					Foreground = new SolidColorBrush(Color.FromRgb(134, 134, 134)),
					FontSize = 11.0
				};
				border.Child = child;
				PanTags.Children.Add(border);
			}
		}
	}

	public TextBlock LabInfo
	{
		get
		{
			if (field == null)
			{
				TextBlock textBlock = new TextBlock
				{
					Name = "LabInfo",
					SnapsToDevicePixels = false,
					UseLayoutRounding = false,
					HorizontalAlignment = HorizontalAlignment.Left,
					IsHitTestVisible = false,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Visibility = Visibility.Collapsed,
					FontSize = 12.0,
					Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
					Opacity = 0.6
				};
				Grid.SetColumn(textBlock, 4);
				Grid.SetRow(textBlock, 2);
				PanBack.Children.Add(textBlock);
				field = textBlock;
			}
			return field;
		}
	}

	public bool IsScaleAnimationEnabled
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (_RectBack != null)
			{
				RectBack.CornerRadius = new CornerRadius(value ? 6 : 0);
			}
		}
	} = true;

	public int PaddingLeft
	{
		get
		{
			return (int)Math.Round(ColumnPaddingLeft.Width.Value);
		}
		set
		{
			ColumnPaddingLeft.Width = new GridLength(value);
		}
	}

	public int MinPaddingRight { get; set; } = 4;

	public IEnumerable<MyIconButton> Buttons
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (buttonStack != null)
			{
				Children.Remove(buttonStack);
				buttonStack = null;
			}
			switch (value.Count())
			{
			case 1:
			{
				foreach (MyIconButton item in value)
				{
					if (item.Height.Equals(double.NaN))
					{
						item.Height = 25.0;
					}
					if (item.Width.Equals(double.NaN))
					{
						item.Width = 25.0;
					}
					item.Opacity = 0.0;
					item.Margin = new Thickness(0.0, 0.0, 5.0, 0.0);
					item.SnapsToDevicePixels = false;
					item.HorizontalAlignment = HorizontalAlignment.Right;
					item.VerticalAlignment = VerticalAlignment.Center;
					item.UseLayoutRounding = false;
					Grid.SetColumnSpan(item, 10);
					Grid.SetRowSpan(item, 10);
					Children.Add(item);
					buttonStack = item;
				}
				return;
			}
			case 0:
				return;
			}
			buttonStack = new StackPanel
			{
				Opacity = 0.0,
				Margin = new Thickness(0.0, 0.0, 5.0, 0.0),
				SnapsToDevicePixels = false,
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				UseLayoutRounding = false
			};
			Grid.SetColumnSpan(buttonStack, 10);
			Grid.SetRowSpan(buttonStack, 10);
			foreach (MyIconButton item2 in value)
			{
				if (item2.Height.Equals(double.NaN))
				{
					item2.Height = 25.0;
				}
				if (item2.Width.Equals(double.NaN))
				{
					item2.Width = 25.0;
				}
				((StackPanel)buttonStack).Children.Add(item2);
			}
			Children.Add(buttonStack);
		}
	}

	public InlineCollection Inlines => LabTitle.Inlines;

	public string Title
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TitleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TitleProperty, (object)value.Replace("\r", "").Replace("\n", ""));
		}
	}

	public double FontSize
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(FontSizeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(FontSizeProperty, (object)value);
		}
	}

	public string Info
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(InfoProperty);
		}
		set
		{
			if (!(Info == value))
			{
				value = value?.Replace("\r", "").Replace("\n", "");
				((DependencyObject)this).SetValue(InfoProperty, (object)value);
			}
		}
	}

	public string Logo
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(LogoProperty);
		}
		set
		{
			if (!(Logo == value))
			{
				((DependencyObject)this).SetValue(LogoProperty, (object)value);
			}
		}
	}

	public string SvgIcon
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(SvgIconProperty);
		}
		set
		{
			if (!(SvgIcon == value))
			{
				((DependencyObject)this).SetValue(SvgIconProperty, (object)value);
			}
		}
	}

	private bool IsUsingSvgIcon => SvgIconControlHelper.HasSvgIcon(SvgIcon);

	public double LogoScale
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (pathLogo != null)
			{
				double num = (IsUsingSvgIcon ? 1.0 : LogoScale);
				pathLogo.RenderTransform = new ScaleTransform
				{
					ScaleX = num,
					ScaleY = num
				};
			}
		}
	} = 1.0;

	public bool LogoClickable { get; set; }

	public CheckType Type
	{
		get
		{
			return _Type;
		}
		set
		{
			if (_Type == value)
			{
				return;
			}
			_Type = value;
			ColumnCheck.Width = new GridLength((_Type != CheckType.None && _Type != CheckType.Clickable) ? 6 : ((Height < 40.0) ? 4 : 2));
			if (_Type == CheckType.None || _Type == CheckType.Clickable)
			{
				if (rectCheck != null)
				{
					Children.Remove(rectCheck);
					rectCheck = null;
				}
				SetChecked(value: false, user: false, anime: false);
			}
			else if (rectCheck == null)
			{
				rectCheck = new Border
				{
					Width = 5.0,
					Height = (Checked ? double.NaN : 0.0),
					CornerRadius = new CornerRadius(2.0, 2.0, 2.0, 2.0),
					VerticalAlignment = ((!Checked) ? VerticalAlignment.Center : VerticalAlignment.Stretch),
					HorizontalAlignment = HorizontalAlignment.Left,
					UseLayoutRounding = false,
					SnapsToDevicePixels = false,
					Margin = (Checked ? new Thickness(-1.0, 6.0, 0.0, 6.0) : new Thickness(-1.0, 0.0, 0.0, 0.0))
				};
				rectCheck.SetResourceReference(Border.BackgroundProperty, "ColorBrush3");
				Grid.SetRowSpan(rectCheck, 4);
				Children.Add(rectCheck);
			}
		}
	}

	public bool Checked
	{
		get
		{
			return _Checked;
		}
		set
		{
			SetChecked(value, user: false, value != _Checked);
		}
	}

	public Brush Foreground
	{
		get
		{
			return (Brush)((DependencyObject)this).GetValue(ForegroundProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ForegroundProperty, (object)value);
		}
	}

	public Action<MyListItem, EventArgs> ContentHandler { get; set; }

	public CornerRadius LogoCornerRadius
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (pathLogo is Canvas)
			{
				_canvasClipHandlerAdded = false;
			}
			ApplyLogoCornerRadius();
		}
	} = new CornerRadius(-1.0);

	public event IMyRadio.CheckEventHandler? Check;

	public event IMyRadio.ChangedEventHandler? Changed;

	public event ClickEventHandler? Click;

	public event LogoClickEventHandler? LogoClick;

	public void RefreshColor(object sender, EventArgs e)
	{
		if (ContentHandler != null)
		{
			ContentHandler(this, e);
			ContentHandler = null;
		}
		string text;
		int num;
		if (isMouseDown && (Type != CheckType.RadioBox || !Checked))
		{
			text = "MouseDown";
			num = 120;
		}
		else if (IsMouseOver && isMouseOverAnimationEnabled)
		{
			text = "MouseOver";
			num = 120;
		}
		else
		{
			text = "Idle";
			num = 180;
		}
		if ((stateLast ?? "") == (text ?? ""))
		{
			return;
		}
		stateLast = text;
		if (IsLoaded && ModAnimation.AniControlEnabled == 0)
		{
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
			if (IsMouseOver && isMouseOverAnimationEnabled)
			{
				if (buttonStack != null)
				{
					list.Add(ModAnimation.AaOpacity(buttonStack, 1.0 - buttonStack.Opacity, (int)Math.Round((double)num * 0.7), (int)Math.Round((double)num * 0.3)));
					list.Add(ModAnimation.AaDouble((object? i) =>
					{
						ColumnPaddingRight.Width = new GridLength(Math.Max(0.0, ColumnPaddingRight.Width.Value + (double)i));
					}, (double)Math.Max(MinPaddingRight, 5 + Buttons.Count() * 25) - ColumnPaddingRight.Width.Value, (int)Math.Round((double)num * 0.3), (int)Math.Round((double)num * 0.7)));
				}
				list.AddRange(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(RectBack, Border.BackgroundProperty, isMouseDown ? "ColorBrush6" : "ColorBrushBg1", num),
					ModAnimation.AaOpacity(RectBack, 1.0 - RectBack.Opacity, num, 0, new ModAnimation.AniEaseOutFluent())
				});
				if (IsScaleAnimationEnabled)
				{
					list.Add(ModAnimation.AaScaleTransform(RectBack, 1.0 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round((double)num * 1.6), 0, new ModAnimation.AniEaseOutFluent()));
					if (isMouseDown)
					{
						list.Add(ModAnimation.AaScaleTransform(this, 0.98 - ((ScaleTransform)RenderTransform).ScaleX, (int)Math.Round((double)num * 0.9), 0, new ModAnimation.AniEaseOutFluent()));
					}
					else
					{
						list.Add(ModAnimation.AaScaleTransform(this, 1.0 - ((ScaleTransform)RenderTransform).ScaleX, (int)Math.Round((double)num * 1.2), 0, new ModAnimation.AniEaseOutFluent()));
					}
				}
			}
			else
			{
				if (buttonStack != null)
				{
					list.Add(ModAnimation.AaOpacity(buttonStack, 0.0 - buttonStack.Opacity, (int)Math.Round((double)num * 0.4)));
					list.Add(ModAnimation.AaDouble((object? i) =>
					{
						ColumnPaddingRight.Width = new GridLength(Math.Max(0.0, ColumnPaddingRight.Width.Value + (double)i));
					}, (double)MinPaddingRight - ColumnPaddingRight.Width.Value, (int)Math.Round((double)num * 0.4)));
				}
				list.Add(ModAnimation.AaOpacity(RectBack, 0.0 - RectBack.Opacity, num));
				if (IsScaleAnimationEnabled)
				{
					list.AddRange(new ModAnimation.AniData[4]
					{
						ModAnimation.AaColor(RectBack, Border.BackgroundProperty, isMouseDown ? "ColorBrush6" : "ColorBrush7", num),
						ModAnimation.AaScaleTransform(this, 1.0 - ((ScaleTransform)RenderTransform).ScaleX, num * 3, 0, new ModAnimation.AniEaseOutFluent()),
						ModAnimation.AaScaleTransform(RectBack, 0.996 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, num, 0, new ModAnimation.AniEaseOutFluent()),
						ModAnimation.AaScaleTransform(RectBack, -0.246, 1, 0, null, after: true)
					});
				}
			}
			ModAnimation.AniStart(list, "ListItem Color " + Uuid);
			return;
		}
		if (IsMouseOver && isMouseOverAnimationEnabled)
		{
			if (buttonStack != null)
			{
				buttonStack.Opacity = 1.0;
				ColumnPaddingRight.Width = new GridLength(Math.Max(MinPaddingRight, 5 + Buttons.Count() * 25));
			}
			RectBack.Background = (Brush)ThemeManager.AppResources["ColorBrushBg1"];
			RectBack.Opacity = 1.0;
			RectBack.RenderTransform = new ScaleTransform(1.0, 1.0);
			RenderTransform = new ScaleTransform(1.0, 1.0);
		}
		else
		{
			if (buttonStack != null)
			{
				buttonStack.Opacity = 0.0;
				ColumnPaddingRight.Width = new GridLength(MinPaddingRight);
			}
			RenderTransform = new ScaleTransform(1.0, 1.0);
			if (_RectBack != null)
			{
				if (IsScaleAnimationEnabled)
				{
					RectBack.RenderTransform = new ScaleTransform(0.75, 0.75);
				}
				RectBack.Background = (Brush)ThemeManager.AppResources["ColorBrush7"];
				RectBack.Opacity = 0.0;
			}
		}
		ModAnimation.AniStop("ListItem Color " + Uuid);
	}

	private void MyListItem_Loaded(object sender, RoutedEventArgs e)
	{
		if (Checked)
		{
			SetResourceReference(ForegroundProperty, (Height < 40.0) ? "ColorBrush3" : "ColorBrush2");
		}
		else
		{
			SetResourceReference(ForegroundProperty, "ColorBrush1");
		}
		ColumnPaddingRight.Width = new GridLength(MinPaddingRight);
	}

	public override string ToString()
	{
		return Title;
	}

	public MyListItem()
	{
		InitializeComponent();
		SizeChanged += (object _, SizeChangedEventArgs _) =>
		{
			OnSizeChanged();
		};
		PreviewMouseLeftButtonUp += Button_MouseUp;
		PreviewMouseLeftButtonDown += Button_MouseDown;
		MouseLeave += Button_MouseLeave;
		PreviewMouseLeftButtonUp += Button_MouseLeave;
		MouseEnter += RefreshColor;
		MouseLeave += RefreshColor;
		MouseLeftButtonDown += RefreshColor;
		MouseLeftButtonUp += RefreshColor;
		Loaded += MyListItem_Loaded;
	}

	private static void OnInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		MyListItem myListItem = (MyListItem)(object)d;
		string text = e.NewValue as string;
		myListItem.LabInfo.Text = text;
		myListItem.LabInfo.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static void OnLogoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((MyListItem)(object)d).UpdateLogo(e.NewValue as string);
	}

	private static void OnSvgIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		MyListItem myListItem = (MyListItem)(object)d;
		myListItem.UpdateLogo(myListItem.Logo);
	}

	private void UpdateLogo(string logo)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected Obj, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected Obj, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected Obj, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected Obj, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected Obj, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected Obj, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected Obj, but got Unknown
		//IL_00be: Expected Obj, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected Obj, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		if (pathLogo != null)
		{
			Children.Remove(pathLogo);
		}
		pathLogo = null;
		if (IsUsingSvgIcon)
		{
			SvgIcon val = new SvgIcon();
			((FrameworkElement)val).Tag = this;
			((UIElement)val).IsHitTestVisible = LogoClickable;
			val.Icon = SvgIcon;
			val.Stretch = Stretch.Uniform;
			((UIElement)val).RenderTransformOrigin = new Point(0.5, 0.5);
			((UIElement)val).RenderTransform = new ScaleTransform
			{
				ScaleX = 1.0,
				ScaleY = 1.0
			};
			((UIElement)val).SnapsToDevicePixels = false;
			((FrameworkElement)val).UseLayoutRounding = false;
			((FrameworkElement)val).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val).VerticalAlignment = VerticalAlignment.Stretch;
			pathLogo = (FrameworkElement)val;
			((FrameworkElement)(SvgIcon)pathLogo).SetBinding(PCL.Core.UI.Controls.SvgIcon.SvgIcon.IconBrushProperty, (BindingBase)new Binding("Foreground")
			{
				Source = this
			});
		}
		else if (!string.IsNullOrEmpty(logo))
		{
			if (logo.StartsWithF("http", ignoreCase: true))
			{
				pathLogo = new MyImage
				{
					Tag = this,
					IsHitTestVisible = LogoClickable,
					Source = logo,
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new ScaleTransform
					{
						ScaleX = LogoScale,
						ScaleY = LogoScale
					},
					SnapsToDevicePixels = true,
					UseLayoutRounding = false
				};
				RenderOptions.SetBitmapScalingMode((DependencyObject)(object)pathLogo, BitmapScalingMode.LowQuality);
			}
			else if (logo.EndsWithF(".png", ignoreCase: true) || logo.EndsWithF(".jpg", ignoreCase: true) || logo.EndsWithF(".webp", ignoreCase: true))
			{
				pathLogo = new Canvas
				{
					Tag = this,
					IsHitTestVisible = LogoClickable,
					Background = (ImageBrush)new MyBitmap(logo),
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new ScaleTransform
					{
						ScaleX = LogoScale,
						ScaleY = LogoScale
					},
					SnapsToDevicePixels = true,
					UseLayoutRounding = false,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				if (logo.Contains(ModBase.pathTemp + "Cache\\Skin\\Head") || logo.Contains(ModBase.pathTemp + "Cache\\Cape"))
				{
					RenderOptions.SetBitmapScalingMode((DependencyObject)(object)pathLogo, BitmapScalingMode.NearestNeighbor);
				}
				else
				{
					RenderOptions.SetBitmapScalingMode((DependencyObject)(object)pathLogo, BitmapScalingMode.LowQuality);
				}
			}
			else
			{
				pathLogo = new Path
				{
					Tag = this,
					IsHitTestVisible = LogoClickable,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Stretch = Stretch.Uniform,
					Data = (Geometry)new GeometryConverter().ConvertFromString(logo),
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new ScaleTransform
					{
						ScaleX = LogoScale,
						ScaleY = LogoScale
					},
					SnapsToDevicePixels = false,
					UseLayoutRounding = false
				};
				pathLogo.SetBinding(Shape.FillProperty, new Binding("Foreground")
				{
					Source = this
				});
			}
		}
		if (pathLogo != null)
		{
			Grid.SetColumn(pathLogo, 2);
			Grid.SetRowSpan(pathLogo, 4);
			OnSizeChanged();
			Children.Add(pathLogo);
			if (LogoClickable)
			{
				pathLogo.MouseLeave += (object sender, MouseEventArgs e) =>
				{
					isLogoDown = false;
				};
				pathLogo.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
				{
					isLogoDown = true;
				};
				pathLogo.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
				{
					if (isLogoDown)
					{
						isLogoDown = false;
						LogoClick?.Invoke(((FrameworkElement)sender).Tag, e);
					}
				};
			}
		}
		bool flag = IsUsingSvgIcon || !string.IsNullOrEmpty(logo);
		ColumnLogo.Width = new GridLength((flag ? 34 : 0) + ((!(Height < 40.0)) ? 4 : 0));
	}

	private void OnSizeChanged()
	{
		string logo = Logo;
		bool flag = IsUsingSvgIcon || !string.IsNullOrEmpty(logo);
		ColumnDefinition columnCheck = ColumnCheck;
		CheckType type = _Type;
		bool flag2 = (uint)type <= 1u;
		columnCheck.Width = new GridLength((!flag2) ? 6 : ((Height < 40.0) ? 4 : 2));
		ColumnLogo.Width = new GridLength((flag ? 34 : 0) + ((!(Height < 40.0)) ? 4 : 0));
		if (pathLogo != null)
		{
			if (!IsUsingSvgIcon && (logo.EndsWithF(".png", ignoreCase: true) || logo.EndsWithF(".jpg", ignoreCase: true) || logo.EndsWithF(".webp", ignoreCase: true)))
			{
				pathLogo.Margin = new Thickness(4.0, 5.0, 3.0, 5.0);
			}
			else
			{
				pathLogo.Margin = new Thickness((Height < 40.0) ? 6 : 8, 8.0, (Height < 40.0) ? 4 : 6, 8.0);
			}
		}
		LabTitle.Margin = new Thickness(4.0, 0.0, 0.0, (!(Height < 40.0)) ? 2 : 0);
	}

	public void SetChecked(bool value, bool user, bool anime)
	{
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ModBase.RouteEventArgs e = new ModBase.RouteEventArgs(user);
			bool flag = _Checked;
			if (Type == CheckType.RadioBox)
			{
				if (IsInitialized && value != _Checked)
				{
					_Checked = value;
					Changed?.Invoke(this, e);
					if (e.handled)
					{
						_Checked = flag;
						return;
					}
				}
				_Checked = value;
			}
			else
			{
				if (value == _Checked)
				{
					return;
				}
				_Checked = value;
				if (IsInitialized)
				{
					Changed?.Invoke(this, e);
					if (e.handled)
					{
						_Checked = flag;
						return;
					}
				}
			}
			if (value)
			{
				ModBase.RouteEventArgs e2 = new ModBase.RouteEventArgs(user);
				Check?.Invoke(this, e2);
				if (e2.handled)
				{
					return;
				}
			}
			if (Type == CheckType.RadioBox)
			{
				if (Parent == null)
				{
					return;
				}
				List<MyListItem> list = new List<MyListItem>();
				int num = 0;
				foreach (FrameworkElement child in ((Panel)(object)Parent).Children)
				{
					if (MyVirtualizingElement.TryInit(child) is MyListItem { Type: CheckType.RadioBox } myListItem)
					{
						list.Add(myListItem);
						if (myListItem.Checked)
						{
							num++;
						}
					}
				}
				int num2 = num;
				if (num2 == 0)
				{
					list[0].Checked = true;
				}
				else if (num2 > 1)
				{
					if (Checked)
					{
						foreach (MyListItem item in list)
						{
							if (item.Checked && !((object)item).Equals((object?)this))
							{
								item.Checked = false;
							}
						}
					}
					else
					{
						bool flag2 = false;
						foreach (MyListItem item2 in list)
						{
							if (item2.Checked)
							{
								if (flag2)
								{
									item2.Checked = false;
								}
								else
								{
									flag2 = true;
								}
							}
						}
					}
				}
			}
			double height = 20.0;
			if ((IsLoaded && ModAnimation.AniControlEnabled == 0) & anime)
			{
				List<ModAnimation.AniData> list2 = new List<ModAnimation.AniData>();
				if (Checked)
				{
					if (rectCheck != null)
					{
						rectCheck.Height = height;
						rectCheck.VerticalAlignment = VerticalAlignment.Center;
						rectCheck.Margin = new Thickness(-1.0, 0.0, 0.0, 0.0);
						rectCheck.Opacity = 1.0;
						ScaleTransform scale = new ScaleTransform(1.0, 0.0);
						rectCheck.RenderTransformOrigin = new Point(0.5, 0.5);
						rectCheck.RenderTransform = scale;
						list2.Add(ModAnimation.AaDouble((object? i) =>
						{
							scale.ScaleY = Math.Max(0.0, scale.ScaleY + (double)i);
						}, 1.0 - scale.ScaleY, 300, 0, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)));
					}
					list2.Add(ModAnimation.AaColor(this, ForegroundProperty, (Height < 40.0) ? "ColorBrush3" : "ColorBrush2", 200));
				}
				else
				{
					if (rectCheck != null)
					{
						if (!(rectCheck.RenderTransform is ScaleTransform))
						{
							rectCheck.RenderTransformOrigin = new Point(0.5, 0.5);
							rectCheck.RenderTransform = new ScaleTransform(1.0, 1.0);
						}
						ScaleTransform scale2 = (ScaleTransform)rectCheck.RenderTransform;
						list2.Add(ModAnimation.AaDouble((object? i) =>
						{
							scale2.ScaleY = Math.Max(0.0, scale2.ScaleY + (double)i);
						}, 0.0 - scale2.ScaleY, 120, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)));
						list2.Add(ModAnimation.AaOpacity(rectCheck, 0.0 - rectCheck.Opacity, 70, 40));
					}
					list2.Add(ModAnimation.AaColor(this, ForegroundProperty, "ColorBrush1", 120));
				}
				ModAnimation.AniStart(list2, "MyListItem Checked " + Uuid);
				return;
			}
			ModAnimation.AniStop("MyListItem Checked " + Uuid);
			if (Checked)
			{
				if (rectCheck != null)
				{
					rectCheck.Height = height;
					rectCheck.Margin = new Thickness(-1.0, 0.0, 0.0, 0.0);
					rectCheck.Opacity = 1.0;
					rectCheck.VerticalAlignment = VerticalAlignment.Center;
					rectCheck.RenderTransform = null;
				}
				SetResourceReference(ForegroundProperty, (Height < 40.0) ? "ColorBrush3" : "ColorBrush2");
			}
			else
			{
				if (rectCheck != null)
				{
					rectCheck.Height = 0.0;
					rectCheck.Margin = new Thickness(-1.0, 0.0, 0.0, 0.0);
					rectCheck.Opacity = 0.0;
					rectCheck.VerticalAlignment = VerticalAlignment.Center;
					rectCheck.RenderTransform = null;
				}
				SetResourceReference(ForegroundProperty, "ColorBrush1");
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "设置 Checked 失败");
		}
	}

	private bool IsLogoCornerRadiusEnabled()
	{
		CornerRadius logoCornerRadius = LogoCornerRadius;
		if (logoCornerRadius.TopLeft >= 0.0 && logoCornerRadius.TopRight >= 0.0 && logoCornerRadius.BottomLeft >= 0.0)
		{
			return logoCornerRadius.BottomRight >= 0.0;
		}
		return false;
	}

	private void UpdateCanvasClip(Canvas c)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (c != null && c.ActualWidth > 0.0 && c.ActualHeight > 0.0)
		{
			CornerRadius logoCornerRadius = LogoCornerRadius;
			double num = Math.Max(Math.Max(logoCornerRadius.TopLeft, logoCornerRadius.TopRight), Math.Max(logoCornerRadius.BottomLeft, logoCornerRadius.BottomRight));
			c.Clip = new RectangleGeometry(new Rect(0.0, 0.0, c.ActualWidth, c.ActualHeight), num, num);
		}
	}

	private void ApplyLogoCornerRadius()
	{
		if (pathLogo == null || !IsLogoCornerRadiusEnabled())
		{
			return;
		}
		FrameworkElement frameworkElement = pathLogo;
		if (!(frameworkElement is MyImage myImage))
		{
			if (frameworkElement is Canvas canvas)
			{
				if (!_canvasClipHandlerAdded && canvas != null && canvas.ActualWidth == 0.0 && canvas.ActualHeight == 0.0)
				{
					UpdateCanvasClip(canvas);
					canvas.SizeChanged += OnCanvasLogoSizeChanged;
					_canvasClipHandlerAdded = true;
				}
				else
				{
					Canvas c = canvas;
					UpdateCanvasClip(c);
				}
			}
		}
		else
		{
			myImage.CornerRadius = LogoCornerRadius;
		}
	}

	private void OnCanvasLogoSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		if (sender is Canvas { ActualWidth: >0.0, ActualHeight: >0.0 } canvas)
		{
			UpdateCanvasClip(canvas);
			canvas.SizeChanged -= OnCanvasLogoSizeChanged;
			_canvasClipHandlerAdded = false;
		}
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected Obj, but got Unknown
		if (!isMouseDown)
		{
			return;
		}
		Click?.Invoke(sender, e);
		if (e.Handled)
		{
			return;
		}
		if (CustomEventService.GetEventType((DependencyObject)sender) != EventType.None)
		{
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			e.Handled = true;
		}
		if (e.Handled)
		{
			return;
		}
		switch (Type)
		{
		case CheckType.Clickable:
			ModBase.Log("[Control] 按下单击列表项：" + Title);
			break;
		case CheckType.RadioBox:
			ModBase.Log("[Control] 按下单选列表项：" + Title);
			if (!Checked)
			{
				SetChecked(value: true, user: true, anime: true);
			}
			break;
		case CheckType.CheckBox:
			ModBase.Log("[Control] 按下复选列表项（" + !Checked + "）：" + Title);
			SetChecked(!Checked, user: true, anime: true);
			break;
		}
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (IsMouseDirectlyOver && Type != CheckType.None)
		{
			isMouseDown = true;
			if (buttonStack != null)
			{
				buttonStack.IsHitTestVisible = false;
			}
		}
	}

	private void Button_MouseLeave(object sender, object e)
	{
		isMouseDown = false;
		if (buttonStack != null)
		{
			buttonStack.IsHitTestVisible = true;
		}
	}

	static MyListItem()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected Obj, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected Obj, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected Obj, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected Obj, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected Obj, but got Unknown
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected Obj, but got Unknown
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected Obj, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected Obj, but got Unknown
	}
}

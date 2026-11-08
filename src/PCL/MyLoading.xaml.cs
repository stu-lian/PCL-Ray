using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class MyLoading : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	public delegate void IsErrorChangedEventHandler(object sender, bool isError);

	public delegate void StateChangedEventHandler(object sender, MyLoadingState newState, MyLoadingState oldState);

	public enum MyLoadingState
	{
		Unloaded = -1,
		Run,
		Stop,
		Error
	}

	private readonly int uuid = ModBase.GetUuid();

	public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(MyLoading));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyLoading), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MyLoading)(object)d).RefreshText();
	})));

	public static readonly DependencyProperty TextErrorProperty = DependencyProperty.Register("TextError", typeof(string), typeof(MyLoading), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MyLoading)(object)d).RefreshText();
	})));

	private bool isLooping;

	private bool errorAnimationWaiting;

	private bool isMouseDown;

	public bool AutoRun { get; set; } = true;

	public SolidColorBrush Foreground
	{
		get
		{
			return (SolidColorBrush)((DependencyObject)this).GetValue(ForegroundProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ForegroundProperty, (object)value);
		}
	}

	private bool _ShowProgress { get; set; }

	public bool ShowProgress
	{
		get
		{
			return _ShowProgress;
		}
		set
		{
			if (_ShowProgress != value)
			{
				_ShowProgress = value;
				RefreshText();
			}
		}
	}

	public string Text
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TextProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TextProperty, (object)value);
		}
	}

	public string TextError
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TextErrorProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TextErrorProperty, (object)value);
		}
	}

	public bool TextErrorInherit { get; set; } = true;

	private ILoadingTrigger _State
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		get
		{
			return field;
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		set
		{
			if (field != null)
			{
				field.ProgressChanged -= (double _, double _) =>
				{
					RefreshText();
				};
				field.LoadingStateChanged -= (MyLoadingState _, MyLoadingState _) =>
				{
					RefreshState();
				};
			}
			field = value;
			if (field != null)
			{
				field.ProgressChanged += (double _, double _) =>
				{
					RefreshText();
				};
				field.LoadingStateChanged += (MyLoadingState _, MyLoadingState _) =>
				{
					RefreshState();
				};
			}
		}
	}

	public ILoadingTrigger State
	{
		get
		{
			InitState();
			return _State;
		}
		set
		{
			_State = value;
			RefreshState();
		}
	}

	private MyLoadingState _OuterState { get; set; } = MyLoadingState.Unloaded;

	private MyLoadingState OuterState
	{
		get
		{
			return _OuterState;
		}
		set
		{
			if (_OuterState != value)
			{
				MyLoadingState outerState = _OuterState;
				_OuterState = value;
				StateChanged?.Invoke(this, value, outerState);
				if (outerState == MyLoadingState.Error != (value == MyLoadingState.Error))
				{
					IsErrorChanged?.Invoke(this, value == MyLoadingState.Error);
				}
			}
		}
	}

	private MyLoadingState _InnerState { get; set; } = MyLoadingState.Unloaded;

	private MyLoadingState InnerState
	{
		get
		{
			return _InnerState;
		}
		set
		{
			if (_InnerState != value)
			{
				MyLoadingState innerState = _InnerState;
				_InnerState = value;
				AniLoop();
				if (innerState == MyLoadingState.Error != (value == MyLoadingState.Error))
				{
					ErrorAnimation(this, value == MyLoadingState.Error);
				}
			}
		}
	}

	public bool HasAnimation { get; set; } = true;

	public event IsErrorChangedEventHandler? IsErrorChanged;

	public event StateChangedEventHandler? StateChanged;

	public event ClickEventHandler? Click;

	public MyLoading()
	{
		InitializeComponent();
		((DependencyObject)this).SetCurrentValue(TextProperty, (object)Lang.Text("Application.Control.Loading.Default"));
		((DependencyObject)this).SetCurrentValue(TextErrorProperty, (object)Lang.Text("Application.Control.Loading.Failed"));
		SetResourceReference(ForegroundProperty, "ColorBrush3");
		IsErrorChanged += (object _, bool _) =>
		{
			RefreshText();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshText();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			InitState();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshState();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			RefreshState();
		};
		MouseLeftButtonUp += Button_MouseUp;
		MouseLeftButtonDown += Button_MouseDown;
		MouseLeave += Button_MouseLeave;
		MouseLeftButtonUp += Button_MouseLeave;
	}

	private void RefreshText()
	{
		ModBase.RunInUi(() =>
		{
			if (InnerState == MyLoadingState.Error)
			{
				if (TextErrorInherit && State.IsLoader)
				{
					Exception ex = State.Error;
					if (ex == null)
					{
						LabText.Text = Lang.Text("Application.Control.Loading.UnknownError");
					}
					else
					{
						while (ex.InnerException != null)
						{
							ex = ex.InnerException;
						}
						LabText.Text = ModBase.StrTrim(ex.Message).ToString();
						if (new string[8] { "远程主机强迫关闭了", "远程方已关闭传输流", "未能解析此远程名称", "由于目标计算机积极拒绝", "操作已超时", "操作超时", "服务器超时", "连接超时" }.Any((string s) => LabText.Text.Contains(s)))
						{
							LabText.Text = Lang.Text("Application.Control.Loading.NetworkUnstable");
						}
					}
				}
				else
				{
					LabText.Text = TextError;
				}
			}
			else if (ShowProgress && State.IsLoader)
			{
				LabText.Text = Text + " - " + Lang.Number<double>(State.Progress, "P0");
			}
			else
			{
				LabText.Text = Text;
			}
		});
	}

	private void InitState()
	{
		if (_State == null)
		{
			_State = new MyLoadingStateSimulator();
			if (AutoRun)
			{
				_State.LoadingState = MyLoadingState.Run;
			}
		}
	}

	private void RefreshState()
	{
		if (_State.LoadingState == MyLoadingState.Run && !IsLoaded)
		{
			InnerState = MyLoadingState.Stop;
		}
		InnerState = _State.LoadingState;
		OuterState = _State.LoadingState;
		AniLoop();
	}

	private void AniLoop()
	{
		if (HasAnimation && !isLooping && InnerState == MyLoadingState.Run && !(ModAnimation.aniSpeed > 10.0) && IsLoaded)
		{
			isLooping = true;
			errorAnimationWaiting = true;
			ModAnimation.AniStart(new ModAnimation.AniData[11]
			{
				ModAnimation.AaRotateTransform(PathPickaxe, -20.0 - ((RotateTransform)PathPickaxe.RenderTransform).Angle, 350, 250, new ModAnimation.AniEaseInBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaRotateTransform(PathPickaxe, 50.0, 900, 0, new ModAnimation.AniEaseOutFluent(), after: true),
				ModAnimation.AaRotateTransform(PathPickaxe, 25.0, 900, 0, new ModAnimation.AniEaseOutElastic(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaCode(() =>
				{
					PathLeft.Opacity = 1.0;
					PathLeft.Margin = new Thickness(7.0, 41.0, 0.0, 0.0);
					PathRight.Opacity = 1.0;
					PathRight.Margin = new Thickness(14.0, 41.0, 0.0, 0.0);
					errorAnimationWaiting = false;
				}),
				ModAnimation.AaOpacity(PathLeft, -1.0, 100, 50),
				ModAnimation.AaX(PathLeft, -5.0, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaY(PathLeft, -6.0, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaOpacity(PathRight, -1.0, 100, 50),
				ModAnimation.AaX(PathRight, 5.0, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaY(PathRight, -6.0, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaCode(() =>
				{
					isLooping = false;
					AniLoop();
				}, 0, after: true)
			}, "MyLoader Loop " + uuid + "/" + ModBase.GetUuid());
		}
	}

	private void ErrorAnimation(object sender, bool isError)
	{
		if (isError)
		{
			int num = (errorAnimationWaiting ? 400 : 0);
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaColor(PanBack, ForegroundProperty, "ColorBrushRedLight", 300),
				ModAnimation.AaOpacity(PathError, 1.0 - PathError.Opacity, 100, 300 + num),
				ModAnimation.AaScaleTransform(PathError, 1.0 - ((ScaleTransform)PathError.RenderTransform).ScaleX, 400, 300 + num, new ModAnimation.AniEaseOutBack())
			}, "MyLoader Error " + uuid);
		}
		else
		{
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaOpacity(PathError, 0.0 - PathError.Opacity, 100),
				ModAnimation.AaScaleTransform(PathError, 0.5 - ((ScaleTransform)PathError.RenderTransform).ScaleX, 200),
				ModAnimation.AaColor(PanBack, ForegroundProperty, "ColorBrush3", 300)
			}, "MyLoader Error " + uuid);
		}
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		Click?.Invoke(sender, e);
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
	}

	private void Button_MouseLeave(object sender, object e)
	{
		isMouseDown = false;
	}

	static MyLoading()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected Obj, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected Obj, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected Obj, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected Obj, but got Unknown
	}
}

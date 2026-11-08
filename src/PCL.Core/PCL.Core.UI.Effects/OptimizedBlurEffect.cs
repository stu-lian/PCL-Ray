using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace PCL.Core.UI.Effects;

public sealed class OptimizedBlurEffect : Freezable
{
	private readonly object _renderLock = new object();

	private readonly SamplingBlurProcessor _processor;

	private WriteableBitmap? _cachedResult;

	private Size _lastRenderSize;

	private double _lastRadius;

	private double _lastSamplingRate;

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(OptimizedBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(16.0, OnEffectPropertyChanged), (ValidateValueCallback)_ValidateRadius);

	public static readonly DependencyProperty SamplingRateProperty = DependencyProperty.Register("SamplingRate", typeof(double), typeof(OptimizedBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(0.7, OnEffectPropertyChanged), (ValidateValueCallback)_ValidateSamplingRate);

	public static readonly DependencyProperty RenderingBiasProperty = DependencyProperty.Register("RenderingBias", typeof(RenderingBias), typeof(OptimizedBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(RenderingBias.Performance, OnEffectPropertyChanged));

	public static readonly DependencyProperty KernelTypeProperty = DependencyProperty.Register("KernelType", typeof(KernelType), typeof(OptimizedBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(KernelType.Gaussian, OnEffectPropertyChanged));

	public double Radius
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(RadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(RadiusProperty, (object)Math.Max(0.0, Math.Min(300.0, value)));
		}
	}

	public double SamplingRate
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(SamplingRateProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SamplingRateProperty, (object)Math.Max(0.1, Math.Min(1.0, value)));
		}
	}

	public RenderingBias RenderingBias
	{
		get
		{
			return (RenderingBias)((DependencyObject)this).GetValue(RenderingBiasProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(RenderingBiasProperty, (object)value);
		}
	}

	public KernelType KernelType
	{
		get
		{
			return (KernelType)((DependencyObject)this).GetValue(KernelTypeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(KernelTypeProperty, (object)value);
		}
	}

	public OptimizedBlurEffect()
	{
		_processor = new SamplingBlurProcessor();
		Radius = 16.0;
		SamplingRate = 0.7;
		RenderingBias = RenderingBias.Performance;
		KernelType = KernelType.Gaussian;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool _ValidateRadius(object value)
	{
		if (value is double num && num >= 0.0)
		{
			return num <= 300.0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool _ValidateSamplingRate(object value)
	{
		if (value is double num && num >= 0.1)
		{
			return num <= 1.0;
		}
		return false;
	}

	private static void OnEffectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is OptimizedBlurEffect optimizedBlurEffect)
		{
			optimizedBlurEffect._InvalidateCachedResult();
		}
	}

	private void _InvalidateCachedResult()
	{
		lock (_renderLock)
		{
			_cachedResult = null;
		}
	}

	protected override Freezable CreateInstanceCore()
	{
		return (Freezable)(object)new OptimizedBlurEffect();
	}

	protected override void CloneCore(Freezable sourceFreezable)
	{
		if (sourceFreezable is OptimizedBlurEffect optimizedBlurEffect)
		{
			Radius = optimizedBlurEffect.Radius;
			SamplingRate = optimizedBlurEffect.SamplingRate;
			RenderingBias = optimizedBlurEffect.RenderingBias;
			KernelType = optimizedBlurEffect.KernelType;
		}
		else if (sourceFreezable is BlurEffect blurEffect)
		{
			Radius = blurEffect.Radius;
			RenderingBias = blurEffect.RenderingBias;
			KernelType = blurEffect.KernelType;
			SamplingRate = 1.0;
		}
		this.CloneCore(sourceFreezable);
	}

	protected override void CloneCurrentValueCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		this.CloneCurrentValueCore(sourceFreezable);
	}

	protected override void GetAsFrozenCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		this.GetAsFrozenCore(sourceFreezable);
	}

	protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		this.GetCurrentValueAsFrozenCore(sourceFreezable);
	}

	public WriteableBitmap? ApplyBlur(BitmapSource? source)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (source == null || Radius < 0.5)
		{
			return null;
		}
		lock (_renderLock)
		{
			Size val = new Size((double)source.PixelWidth, (double)source.PixelHeight);
			if (_cachedResult == null || !Size.Equals(_lastRenderSize, val) || Math.Abs(_lastRadius - Radius) > 0.1 || Math.Abs(_lastSamplingRate - SamplingRate) > 0.05)
			{
				_cachedResult = _processor.ApplySamplingBlur(source, Radius, SamplingRate, RenderingBias, KernelType);
				_lastRenderSize = val;
				_lastRadius = Radius;
				_lastSamplingRate = SamplingRate;
			}
			return _cachedResult;
		}
	}

	internal SamplingBlurProcessor GetProcessor()
	{
		return _processor;
	}

	public WriteableBitmap? RenderBlurredBitmap(Visual? visual, Size size)
	{
		if (visual == null || size.Width <= 0.0 || size.Height <= 0.0)
		{
			return null;
		}
		try
		{
			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96.0, 96.0, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(visual);
			return ApplyBlur(renderTargetBitmap);
		}
		catch
		{
			return null;
		}
	}

	private BlurEffect _GetFallbackEffect()
	{
		return new BlurEffect
		{
			Radius = Radius,
			KernelType = KernelType,
			RenderingBias = RenderingBias
		};
	}

	public Effect GetEffectInstance()
	{
		_ = SamplingRate;
		_ = 0.98;
		return _GetFallbackEffect();
	}

	public void Dispose()
	{
		_processor.Dispose();
		_cachedResult = null;
	}

	~OptimizedBlurEffect()
	{
		Dispose();
	}

	static OptimizedBlurEffect()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected Obj, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected Obj, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected Obj, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected Obj, but got Unknown
	}
}

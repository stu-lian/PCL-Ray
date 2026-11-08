using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Effects;

namespace PCL.Core.UI.Effects;

public sealed class EnhancedBlurEffect : Freezable
{
	private readonly BlurEffect _nativeBlur;

	private readonly SamplingBlurProcessor _processor;

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(EnhancedBlurEffect), new PropertyMetadata((object)16.0, (PropertyChangedCallback)OnEffectPropertyChanged), (ValidateValueCallback)_ValidateRadius);

	public static readonly DependencyProperty SamplingRateProperty = DependencyProperty.Register("SamplingRate", typeof(double), typeof(EnhancedBlurEffect), new PropertyMetadata((object)0.7, (PropertyChangedCallback)OnEffectPropertyChanged), (ValidateValueCallback)_ValidateSamplingRate);

	public static readonly DependencyProperty RenderingBiasProperty = DependencyProperty.Register("RenderingBias", typeof(RenderingBias), typeof(EnhancedBlurEffect), new PropertyMetadata((object)RenderingBias.Performance, (PropertyChangedCallback)OnEffectPropertyChanged));

	public static readonly DependencyProperty KernelTypeProperty = DependencyProperty.Register("KernelType", typeof(KernelType), typeof(EnhancedBlurEffect), new PropertyMetadata((object)KernelType.Gaussian, (PropertyChangedCallback)OnEffectPropertyChanged));

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

	public EnhancedBlurEffect()
	{
		_nativeBlur = new BlurEffect();
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
		if (d is EnhancedBlurEffect enhancedBlurEffect)
		{
			enhancedBlurEffect._UpdateNativeBlur();
			enhancedBlurEffect._processor.InvalidateCache();
		}
	}

	private void _UpdateNativeBlur()
	{
		_nativeBlur.Radius = Radius;
		_nativeBlur.RenderingBias = RenderingBias;
		_nativeBlur.KernelType = KernelType;
	}

	protected override Freezable CreateInstanceCore()
	{
		return (Freezable)(object)new EnhancedBlurEffect();
	}

	protected override void CloneCore(Freezable sourceFreezable)
	{
		if (sourceFreezable is EnhancedBlurEffect enhancedBlurEffect)
		{
			Radius = enhancedBlurEffect.Radius;
			SamplingRate = enhancedBlurEffect.SamplingRate;
			RenderingBias = enhancedBlurEffect.RenderingBias;
			KernelType = enhancedBlurEffect.KernelType;
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

	internal Effect GetOptimizedEffect()
	{
		_ = SamplingRate;
		_ = 0.95;
		_UpdateNativeBlur();
		return _nativeBlur;
	}

	static EnhancedBlurEffect()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected Obj, but got Unknown
		//IL_0049: Expected Obj, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected Obj, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected Obj, but got Unknown
		//IL_0097: Expected Obj, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected Obj, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected Obj, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected Obj, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Expected Obj, but got Unknown
	}
}

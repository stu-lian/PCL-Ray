using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PCL.Core.UI.Effects;

public sealed class AdaptiveBlurEffect : ShaderEffect
{
	private const string PixelShaderUri = "pack://application:,,,/PCL.Core;component/UI/Assets/Shaders/AdaptiveBlur.ps";

	private static readonly MemoryPool<byte> _MemoryPool;

	private static readonly object _ShaderLock;

	private static PixelShader? _cachedShader;

	private static readonly Vector2[] _GaussianSampleOffsets;

	private static readonly float[] _GaussianWeights;

	public static readonly DependencyProperty InputProperty;

	public static readonly DependencyProperty RadiusProperty;

	public static readonly DependencyProperty SamplingRateProperty;

	public static readonly DependencyProperty QualityBiasProperty;

	public static readonly DependencyProperty TextureSizeProperty;

	public static readonly DependencyProperty RenderingBiasProperty;

	public static readonly DependencyProperty KernelTypeProperty;

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

	public Brush Input
	{
		get
		{
			return (Brush)((DependencyObject)this).GetValue(InputProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(InputProperty, (object)value);
		}
	}

	static AdaptiveBlurEffect()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected Obj, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected Obj, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Expected Obj, but got Unknown
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Expected Obj, but got Unknown
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected Obj, but got Unknown
		_MemoryPool = MemoryPool<byte>.Shared;
		_ShaderLock = new object();
		_GaussianSampleOffsets = _GenerateOptimalSamplePattern();
		_GaussianWeights = _GenerateGaussianWeights();
		InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(AdaptiveBlurEffect), 0);
		RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(AdaptiveBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(16.0, ShaderEffect.PixelShaderConstantCallback(0)), (ValidateValueCallback)_ValidateRadius);
		SamplingRateProperty = DependencyProperty.Register("SamplingRate", typeof(double), typeof(AdaptiveBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(1.0, ShaderEffect.PixelShaderConstantCallback(1)), (ValidateValueCallback)_ValidateSamplingRate);
		QualityBiasProperty = DependencyProperty.Register("QualityBias", typeof(double), typeof(AdaptiveBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata(0.0, ShaderEffect.PixelShaderConstantCallback(2)));
		TextureSizeProperty = DependencyProperty.Register("TextureSize", typeof(Point), typeof(AdaptiveBlurEffect), (PropertyMetadata)(object)new UIPropertyMetadata((object)new Point(1920.0, 1080.0), ShaderEffect.PixelShaderConstantCallback(3)));
		RenderingBiasProperty = DependencyProperty.Register("RenderingBias", typeof(RenderingBias), typeof(AdaptiveBlurEffect), new PropertyMetadata((object)RenderingBias.Performance, (PropertyChangedCallback)OnRenderingBiasChanged));
		KernelTypeProperty = DependencyProperty.Register("KernelType", typeof(KernelType), typeof(AdaptiveBlurEffect), new PropertyMetadata((object)KernelType.Gaussian));
		_EnsureShaderInitialized();
	}

	public AdaptiveBlurEffect()
	{
		PixelShader = _cachedShader;
		UpdateShaderValue(InputProperty);
		UpdateShaderValue(RadiusProperty);
		UpdateShaderValue(SamplingRateProperty);
		UpdateShaderValue(QualityBiasProperty);
		UpdateShaderValue(TextureSizeProperty);
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

	private static void OnRenderingBiasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AdaptiveBlurEffect adaptiveBlurEffect)
		{
			object newValue = e.NewValue;
			double num = ((newValue is RenderingBias && (RenderingBias)newValue == RenderingBias.Quality) ? 1.0 : 0.0);
			((DependencyObject)adaptiveBlurEffect).SetValue(QualityBiasProperty, (object)num);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void _EnsureShaderInitialized()
	{
		if (_cachedShader != null)
		{
			return;
		}
		lock (_ShaderLock)
		{
			if (_cachedShader == null)
			{
				try
				{
					_cachedShader = new PixelShader
					{
						UriSource = new Uri("pack://application:,,,/PCL.Core;component/UI/Assets/Shaders/AdaptiveBlur.ps", UriKind.Absolute)
					};
					return;
				}
				catch
				{
					_cachedShader = new PixelShader();
					return;
				}
			}
		}
	}

	protected override Freezable CreateInstanceCore()
	{
		return (Freezable)(object)new AdaptiveBlurEffect();
	}

	protected override void CloneCore(Freezable sourceFreezable)
	{
		if (sourceFreezable is AdaptiveBlurEffect adaptiveBlurEffect)
		{
			Radius = adaptiveBlurEffect.Radius;
			SamplingRate = adaptiveBlurEffect.SamplingRate;
			RenderingBias = adaptiveBlurEffect.RenderingBias;
			KernelType = adaptiveBlurEffect.KernelType;
		}
		base.CloneCore(sourceFreezable);
	}

	protected override void CloneCurrentValueCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		base.CloneCurrentValueCore(sourceFreezable);
	}

	protected override void GetAsFrozenCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		base.GetAsFrozenCore(sourceFreezable);
	}

	protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
	{
		this.CloneCore(sourceFreezable);
		base.GetCurrentValueAsFrozenCore(sourceFreezable);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static Vector2[] _GenerateOptimalSamplePattern()
	{
		Vector2[] array = new Vector2[32];
		int num = 0;
		Random random = new Random(42);
		int num2 = 0;
		while (num < 32 && num2 < 1000)
		{
			Vector2 vector = new Vector2((float)(random.NextDouble() * 2.0 - 1.0), (float)(random.NextDouble() * 2.0 - 1.0));
			if (vector.LengthSquared() > 1f)
			{
				num2++;
				continue;
			}
			bool flag = true;
			for (int i = 0; i < num; i++)
			{
				if (Vector2.DistanceSquared(vector, array[i]) < 0.64000005f)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				array[num++] = vector;
			}
			num2++;
		}
		return array.AsSpan(0, num).ToArray();
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static float[] _GenerateGaussianWeights()
	{
		float[] array = new float[33];
		float num = 5.5f;
		float num2 = 2f * num * num;
		float num3 = 1f / MathF.Sqrt((float)Math.PI * num2);
		float num4 = 0f;
		for (int i = 0; i < 33; i++)
		{
			int num5 = i - 16;
			num4 += (array[i] = num3 * MathF.Exp((float)(-(num5 * num5)) / num2));
		}
		if (num4 > 0f)
		{
			float num6 = 1f / num4;
			for (int j = 0; j < 33; j++)
			{
				array[j] *= num6;
			}
		}
		return array;
	}
}

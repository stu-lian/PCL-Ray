using System;
using System.Windows.Media.Effects;

namespace PCL.Core.UI.Effects;

public static class BlurPerformancePresets
{
	public static EnhancedBlurEffect BestQuality(double radius = 16.0)
	{
		return new EnhancedBlurEffect
		{
			Radius = radius,
			SamplingRate = 1.0,
			RenderingBias = RenderingBias.Quality,
			KernelType = KernelType.Gaussian
		};
	}

	public static EnhancedBlurEffect Balanced(double radius = 16.0)
	{
		return new EnhancedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.7,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Gaussian
		};
	}

	public static EnhancedBlurEffect HighPerformance(double radius = 16.0)
	{
		return new EnhancedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.3,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Box
		};
	}

	public static EnhancedBlurEffect UltraFast(double radius = 16.0)
	{
		return new EnhancedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.1,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Box
		};
	}

	public static EnhancedBlurEffect Adaptive(double radius = 16.0)
	{
		double samplingRate = Math.Max(0.2, Math.Min(1.0, 30.0 / radius));
		return new EnhancedBlurEffect
		{
			Radius = radius,
			SamplingRate = samplingRate,
			RenderingBias = ((!(radius > 20.0)) ? RenderingBias.Quality : RenderingBias.Performance),
			KernelType = KernelType.Gaussian
		};
	}
}

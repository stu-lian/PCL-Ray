using System;
using System.ComponentModel;
using System.Globalization;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Converters;

public class EasingConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(string)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		string text = value as string;
		return text switch
		{
			"BackEaseIn" => new BackEaseIn(), 
			"BackEaseOut" => new BackEaseOut(), 
			"BackEaseInOut" => new BackEaseInOut(), 
			"BounceEaseIn" => new BounceEaseIn(), 
			"BounceEaseOut" => new BounceEaseOut(), 
			"BounceEaseInOut" => new BounceEaseInOut(), 
			"CircularEaseIn" => new CircularEaseIn(), 
			"CircularEaseOut" => new CircularEaseOut(), 
			"CircularEaseInOut" => new CircularEaseInOut(), 
			"CubicEaseIn" => new CubicEaseIn(), 
			"CubicEaseOut" => new CubicEaseOut(), 
			"CubicEaseInOut" => new CubicEaseInOut(), 
			"ElasticEaseIn" => new ElasticEaseIn(), 
			"ElasticEaseOut" => new ElasticEaseOut(), 
			"ElasticEaseInOut" => new ElasticEaseInOut(), 
			"ExponentialEaseIn" => new ExponentialEaseIn(), 
			"ExponentialEaseOut" => new ExponentialEaseOut(), 
			"ExponentialEaseInOut" => new ExponentialEaseInOut(), 
			"LinearEasing" => new LinearEasing(), 
			"QuadEaseIn" => new QuadEaseIn(), 
			"QuadEaseOut" => new QuadEaseOut(), 
			"QuadEaseInOut" => new QuadEaseInOut(), 
			"QuarticEaseIn" => new QuarticEaseIn(), 
			"QuarticEaseOut" => new QuarticEaseOut(), 
			"QuarticEaseInOut" => new QuarticEaseInOut(), 
			"QuinticEaseIn" => new QuinticEaseIn(), 
			"QuinticEaseOut" => new QuinticEaseOut(), 
			"QuinticEaseInOut" => new QuinticEaseInOut(), 
			"SineEaseIn" => new SineEaseIn(), 
			"SineEaseOut" => new SineEaseOut(), 
			"SineEaseInOut" => new SineEaseInOut(), 
			null => base.ConvertFrom(context, culture, value), 
			_ => throw new NotSupportedException("不支持的缓动: " + text), 
		};
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is IEasing easing)
		{
			if (!(easing is BackEaseIn))
			{
				if (!(easing is BackEaseOut))
				{
					if (!(easing is BackEaseInOut))
					{
						if (!(easing is BounceEaseIn))
						{
							if (!(easing is BounceEaseOut))
							{
								if (!(easing is BounceEaseInOut))
								{
									if (!(easing is CircularEaseIn))
									{
										if (!(easing is CircularEaseOut))
										{
											if (!(easing is CircularEaseInOut))
											{
												if (!(easing is CubicEaseIn))
												{
													if (!(easing is CubicEaseOut))
													{
														if (!(easing is CubicEaseInOut))
														{
															if (!(easing is ElasticEaseIn))
															{
																if (!(easing is ElasticEaseOut))
																{
																	if (!(easing is ElasticEaseInOut))
																	{
																		if (!(easing is ExponentialEaseIn))
																		{
																			if (!(easing is ExponentialEaseOut))
																			{
																				if (!(easing is ExponentialEaseInOut))
																				{
																					if (!(easing is LinearEasing))
																					{
																						if (!(easing is QuadEaseIn))
																						{
																							if (!(easing is QuadEaseOut))
																							{
																								if (!(easing is QuadEaseInOut))
																								{
																									if (!(easing is QuarticEaseIn))
																									{
																										if (!(easing is QuarticEaseOut))
																										{
																											if (!(easing is QuarticEaseInOut))
																											{
																												if (!(easing is QuinticEaseIn))
																												{
																													if (!(easing is QuinticEaseOut))
																													{
																														if (!(easing is QuinticEaseInOut))
																														{
																															if (!(easing is SineEaseIn))
																															{
																																if (!(easing is SineEaseOut))
																																{
																																	if (easing is SineEaseInOut)
																																	{
																																		return "SineEaseInOut";
																																	}
																																	throw new NotSupportedException("不支持的缓动: " + easing.GetType().Name);
																																}
																																return "SineEaseOut";
																															}
																															return "SineEaseIn";
																														}
																														return "QuinticEaseInOut";
																													}
																													return "QuinticEaseOut";
																												}
																												return "QuinticEaseIn";
																											}
																											return "QuarticEaseInOut";
																										}
																										return "QuarticEaseOut";
																									}
																									return "QuarticEaseIn";
																								}
																								return "QuadEaseInOut";
																							}
																							return "QuadEaseOut";
																						}
																						return "QuadEaseIn";
																					}
																					return "LinearEasing";
																				}
																				return "ExponentialEaseInOut";
																			}
																			return "ExponentialEaseOut";
																		}
																		return "ExponentialEaseIn";
																	}
																	return "ElasticEaseInOut";
																}
																return "ElasticEaseOut";
															}
															return "ElasticEaseIn";
														}
														return "CubicEaseInOut";
													}
													return "CubicEaseOut";
												}
												return "CubicEaseIn";
											}
											return "CircularEaseInOut";
										}
										return "CircularEaseOut";
									}
									return "CircularEaseIn";
								}
								return "BounceEaseInOut";
							}
							return "BounceEaseOut";
						}
						return "BounceEaseIn";
					}
					return "BackEaseInOut";
				}
				return "BackEaseOut";
			}
			return "BackEaseIn";
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}

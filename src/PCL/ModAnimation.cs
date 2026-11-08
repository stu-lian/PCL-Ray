using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

public static class ModAnimation
{
	public class AniGroupEntry
	{
		public List<AniData> data;

		public long startTick;

		public int Uuid = ModBase.GetUuid();
	}

	public struct AniData
	{
		public AniType typeMain;

		public AniTypeSub typeSub;

		public int timeTotal;

		public int timeFinished;

		public double timePercent;

		public bool isAfter;

		public AniEase ease;

		public object obj;

		public object value;

		public object valueLast;

		public override string ToString()
		{
			return ModBase.GetStringFromEnum(typeMain) + " | " + timeFinished + "/" + timeTotal + "(" + Math.Round(timePercent * 100.0) + "%)" + ((obj == null) ? "" : (" | " + obj?.ToString() + "(" + obj.GetType().Name + ")"));
		}
	}

	public enum AniType
	{
		Number,
		Color,
		Scale,
		TextAppear,
		Code,
		ScaleTransform,
		RotateTransform
	}

	public enum AniTypeSub
	{
		X,
		Y,
		Width,
		Height,
		Opacity,
		Value,
		Radius,
		BorderThickness,
		StrokeThickness,
		TranslateX,
		TranslateY,
		Double,
		DoubleParam,
		GridLengthWidth
	}

	public enum AniEasePower
	{
		Weak = 2,
		Middle,
		Strong,
		ExtraStrong
	}

	public abstract class AniEase
	{
		public abstract double GetValue(double t);

		public virtual double GetDelta(double t1, double t0)
		{
			return GetValue(t1) - GetValue(t0);
		}
	}

	public class AniEaseInout : AniEase
	{
		private readonly AniEase easeIn;

		private readonly double easeInPercent;

		private readonly AniEase easeOut;

		public AniEaseInout(AniEase easeIn, AniEase easeOut, double easeInPercent = 0.5)
		{
			this.easeIn = easeIn;
			this.easeOut = easeOut;
			this.easeInPercent = easeInPercent;
		}

		public override double GetValue(double t)
		{
			if (t < easeInPercent)
			{
				return easeInPercent * easeIn.GetValue(t / easeInPercent);
			}
			return (1.0 - easeInPercent) * easeOut.GetValue((t - easeInPercent) / (1.0 - easeInPercent)) + easeInPercent;
		}
	}

	public class AniEaseLinear : AniEase
	{
		public override double GetValue(double t)
		{
			return ModBase.MathClamp(t, 0.0, 1.0);
		}

		public override double GetDelta(double t1, double t0)
		{
			return ModBase.MathClamp(t1, 0.0, 1.0) - ModBase.MathClamp(t0, 0.0, 1.0);
		}
	}

	public class AniEaseInFluent : AniEase
	{
		private readonly AniEasePower p;

		public AniEaseInFluent(AniEasePower power = AniEasePower.Middle)
		{
			p = power;
		}

		public override double GetValue(double t)
		{
			return Math.Pow(ModBase.MathClamp(t, 0.0, 1.0), (double)p);
		}
	}

	public class AniEaseOutFluent : AniEase
	{
		private readonly AniEasePower p;

		public AniEaseOutFluent(AniEasePower power = AniEasePower.Middle)
		{
			p = power;
		}

		public override double GetValue(double t)
		{
			return 1.0 - Math.Pow(ModBase.MathClamp(1.0 - t, 0.0, 1.0), (double)p);
		}
	}

	public class AniEaseInoutFluent : AniEase
	{
		private readonly AniEaseInout ease;

		public AniEaseInoutFluent(AniEasePower power = AniEasePower.Middle, double middle = 0.5)
		{
			ease = new AniEaseInout(new AniEaseInFluent(power), new AniEaseOutFluent(power), middle);
		}

		public override double GetValue(double t)
		{
			return ease.GetValue(t);
		}
	}

	public class AniEaseOutFluentWithInitial : AniEase
	{
		private readonly double alpha;

		public AniEaseOutFluentWithInitial(double initialPixelPerSecond, double totalSecond, double totalDistance)
		{
			double num = initialPixelPerSecond * totalSecond / totalDistance;
			alpha = num - 1.0;
			if (alpha < 0.0)
			{
				alpha = 0.0;
			}
		}

		public override double GetValue(double percent)
		{
			double num = ModBase.MathClamp(percent, 0.0, 1.0);
			if (alpha == 0.0)
			{
				return num;
			}
			return (alpha + 1.0) * num / (1.0 + alpha * num);
		}
	}

	public class AniEaseInBack : AniEase
	{
		private readonly double p;

		public AniEaseInBack(AniEasePower power = AniEasePower.Middle)
		{
			p = 3.0 - (double)power * 0.5;
		}

		public override double GetValue(double t)
		{
			t = ModBase.MathClamp(t, 0.0, 1.0);
			return Math.Pow(t, p) * Math.Cos(4.71238898038469 * (1.0 - t));
		}
	}

	public class AniEaseOutBack : AniEase
	{
		private readonly double p;

		public AniEaseOutBack(AniEasePower power = AniEasePower.Middle)
		{
			p = 3.0 - (double)power * 0.5;
		}

		public override double GetValue(double t)
		{
			t = ModBase.MathClamp(t, 0.0, 1.0);
			return 1.0 - Math.Pow(1.0 - t, p) * Math.Cos(4.71238898038469 * t);
		}
	}

	public class AniEaseInCar : AniEase
	{
		private readonly AniEaseInout ease;

		public AniEaseInCar(double middle = 0.7, AniEasePower power = AniEasePower.Middle)
		{
			ease = new AniEaseInout(new AniEaseInBack(power), new AniEaseOutFluent(power), middle);
		}

		public override double GetValue(double t)
		{
			return ease.GetValue(t);
		}
	}

	public class AniEaseOutCar : AniEase
	{
		private readonly AniEaseInout ease;

		public AniEaseOutCar(double middle = 0.3, AniEasePower power = AniEasePower.Middle)
		{
			ease = new AniEaseInout(new AniEaseInFluent(power), new AniEaseOutBack(power), middle);
		}

		public override double GetValue(double t)
		{
			return ease.GetValue(t);
		}
	}

	public class AniEaseInElastic : AniEase
	{
		private readonly int p;

		public AniEaseInElastic(AniEasePower power = AniEasePower.Middle)
		{
			p = (int)(power + 4);
		}

		public override double GetValue(double t)
		{
			t = ModBase.MathClamp(t, 0.0, 1.0);
			return Math.Pow(t, (double)(p - 1) * 0.25) * Math.Cos(((double)p - 3.5) * Math.PI * Math.Pow(1.0 - t, 1.5));
		}
	}

	public class AniEaseOutElastic : AniEase
	{
		private readonly int p;

		public AniEaseOutElastic(AniEasePower power = AniEasePower.Middle)
		{
			p = (int)(power + 4);
		}

		public override double GetValue(double t)
		{
			t = 1.0 - ModBase.MathClamp(t, 0.0, 1.0);
			return 1.0 - Math.Pow(t, (double)(p - 1) * 0.25) * Math.Cos(((double)p - 3.5) * Math.PI * Math.Pow(1.0 - t, 1.5));
		}
	}

	private static int aniCount;

	private static int aniFPSCounter;

	private static long aniFPSTimer;

	public static int aniFPS;

	public static double aniSpeed = 1.0;

	public static ConcurrentDictionary<string, AniGroupEntry> aniGroups = new ConcurrentDictionary<string, AniGroupEntry>();

	private static long aniLastTick;

	public static bool aniRunning;

	private static readonly object aniControlEnabledLock = new object();

	public static int AniControlEnabled
	{
		get
		{
			return field;
		}
		set
		{
			lock (aniControlEnabledLock)
			{
				field = value;
			}
		}
	}

	public static void AniDispose(MyCard control, bool removeFromChildren, ParameterizedThreadStart callBack = null)
	{
		if (control.IsHitTestVisible)
		{
			control.IsHitTestVisible = false;
			AniStart(new AniData[4]
			{
				AaScaleTransform(control, -0.08, 200, 0, new AniEaseInFluent()),
				AaOpacity(control, -1.0, 200, 0, new AniEaseOutFluent()),
				AaHeight(control, 0.0 - control.ActualHeight, 150, 100, new AniEaseOutFluent()),
				AaCode(() =>
				{
					if (removeFromChildren)
					{
						if (control.Parent == null)
						{
							return;
						}
						((Panel)(object)control.Parent).Children.Remove(control);
					}
					else
					{
						control.Visibility = Visibility.Collapsed;
					}
					if (callBack != null)
					{
						callBack(control);
					}
				}, 0, after: true)
			}, "MyCard Dispose " + control.uuid);
			return;
		}
		if (removeFromChildren)
		{
			if (control.Parent == null)
			{
				return;
			}
			((Panel)(object)control.Parent).Children.Remove(control);
		}
		else
		{
			control.Visibility = Visibility.Collapsed;
		}
		if (callBack != null)
		{
			callBack(control);
		}
	}

	public static void AniDispose(MyHint control, bool removeFromChildren, ParameterizedThreadStart callBack = null)
	{
		if (!control.IsHitTestVisible)
		{
			return;
		}
		control.IsHitTestVisible = false;
		AniStart(new AniData[4]
		{
			AaScaleTransform(control, -0.08, 200, 0, new AniEaseInFluent()),
			AaOpacity(control, -1.0, 200, 0, new AniEaseOutFluent()),
			AaHeight(control, 0.0 - control.ActualHeight, 150, 100, new AniEaseOutFluent()),
			AaCode(() =>
			{
				if (removeFromChildren)
				{
					((Panel)(object)control.Parent).Children.Remove(control);
				}
				else
				{
					control.Visibility = Visibility.Collapsed;
				}
				if (callBack != null)
				{
					callBack(control);
				}
			}, 0, after: true)
		}, "MyCard Dispose " + control.Uuid);
	}

	public static void AniDispose(MyIconButton control, bool removeFromChildren, ParameterizedThreadStart callBack = null)
	{
		if (!control.IsHitTestVisible)
		{
			return;
		}
		control.IsHitTestVisible = false;
		AniStart(new AniData[2]
		{
			AaScaleTransform(control, -1.5, 200, 0, new AniEaseInFluent()),
			AaCode(() =>
			{
				if (removeFromChildren)
				{
					((Panel)(object)control.Parent).Children.Remove(control);
				}
				else
				{
					control.Visibility = Visibility.Collapsed;
				}
				if (callBack != null)
				{
					callBack(control);
				}
			}, 0, after: true)
		}, "MyIconButton Dispose " + control.Uuid);
	}

	public static void AniStart()
	{
		aniLastTick = TimeUtils.GetTimeTick();
		aniFPSTimer = aniLastTick;
		aniRunning = true;
		double minFrameGap = 1000.0 / (double)(Config.System.AnimationFpsLimit + 1) / 2.0;
		ModBase.RunInNewThread(() =>
		{
			try
			{
				ModBase.Log("[Animation] 动画线程开始");
				while (true)
				{
					long deltaTime = (long)Math.Round(ModBase.MathClamp(TimeUtils.GetTimeTick() - aniLastTick, 0.0, 100000.0));
					if ((double)deltaTime < minFrameGap)
					{
						Thread.Sleep(1);
					}
					else
					{
						aniLastTick = TimeUtils.GetTimeTick();
						if (ModBase.modeDebug)
						{
							if (ModBase.MathClamp(aniLastTick - aniFPSTimer, 0.0, 100000.0) >= 500.0)
							{
								aniFPS = aniFPSCounter;
								aniFPSCounter = 0;
								aniFPSTimer = aniLastTick;
							}
							aniFPSCounter += 2;
						}
						ModBase.RunInUiWait(() =>
						{
							aniCount = 0;
							AniTimer((int)Math.Round((double)deltaTime * aniSpeed));
							if (RandomUtils.NextInt(0, 64 * (ModBase.modeDebug ? 5 : 30)) == 0 && ((aniFPS < 62 && aniFPS > 0) || aniCount > 4 || ModNet.NetManager.FileRemain != 0))
							{
								ModBase.Log("[Report] FPS " + aniFPS + ", 动画 " + aniCount + ", 下载中 " + ModNet.NetManager.FileRemain + "（" + ModBase.GetString(ModNet.NetManager.Speed) + "/s）");
							}
						});
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "动画帧执行失败", ModBase.LogLevel.Critical, null, Lang.Text("Application.Animation.Error.OperationFailed"));
			}
		}, "Animation", ThreadPriority.AboveNormal);
	}

	public static void AniTimer(int deltaTick)
	{
		try
		{
			if ((double)deltaTick / aniSpeed > 100.0)
			{
				ModBase.Log("[Animation] 两个动画帧间隔 " + deltaTick + " ms", ModBase.LogLevel.Developer);
			}
			int num = -1;
			while (num + 1 < aniGroups.Count)
			{
				num++;
				AniGroupEntry aniGroupEntry = aniGroups.Values.ElementAtOrDefault(num);
				if (aniGroupEntry.startTick > aniLastTick)
				{
					continue;
				}
				bool flag = true;
				int num2 = 0;
				while (num2 < aniGroupEntry.data.Count)
				{
					AniData aniData = aniGroupEntry.data[num2];
					if (!aniData.isAfter)
					{
						flag = false;
						aniData.timeFinished += deltaTick;
						if (aniData.timeFinished > 0)
						{
							aniData = AniRun(aniData);
							aniCount++;
						}
						if (aniData.timeFinished >= aniData.timeTotal)
						{
							if (aniData.typeMain == AniType.Color && !string.Equals(((dynamic)aniData.obj)[2] as string, "", StringComparison.Ordinal))
							{
								((dynamic)aniData.obj)[0].SetResourceReference(((dynamic)aniData.obj)[1], ((dynamic)aniData.obj)[2]);
							}
							aniGroupEntry.data.RemoveAt(num2);
						}
						else
						{
							aniGroupEntry.data[num2] = aniData;
							num2++;
						}
					}
					else
					{
						if (!flag)
						{
							break;
						}
						flag = false;
						aniData.isAfter = false;
						aniGroupEntry.data[num2] = aniData;
					}
				}
				if (aniGroupEntry.data.Any())
				{
					continue;
				}
				int i = 0;
				for (int num3 = aniGroups.Count - 1; i <= num3; i++)
				{
					if (aniGroups.ElementAt(i).Value.Uuid == aniGroupEntry.Uuid)
					{
						aniGroups.Remove(aniGroups.ElementAt(i).Key, out var _);
						break;
					}
				}
				num--;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "动画刻执行失败", ModBase.LogLevel.Hint, null, Lang.Text("Application.Animation.Error.OperationFailed"));
		}
	}

	private static AniData AniRun(AniData ani)
	{
		//IL_2ac7: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bc4: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			switch (ani.typeMain)
			{
			case AniType.Number:
			{
				double num4 = ModBase.MathPercent(0.0, (double)ani.value, ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, ani.timePercent));
				if (num4 == 0.0)
				{
					break;
				}
				switch (ani.typeSub)
				{
				case AniTypeSub.X:
					ModBase.DeltaLeft((FrameworkElement)ani.obj, num4);
					break;
				case AniTypeSub.Y:
					ModBase.DeltaTop((FrameworkElement)ani.obj, num4);
					break;
				case AniTypeSub.Opacity:
					((dynamic)ani.obj).Opacity = ModBase.MathClamp(Convert.ToDouble(((dynamic)ani.obj).Opacity) + num4, 0.0, 1.0);
					break;
				case AniTypeSub.Width:
				{
					FrameworkElement frameworkElement5 = (FrameworkElement)ani.obj;
					frameworkElement5.Width = Math.Max((double.IsNaN(frameworkElement5.Width) ? frameworkElement5.ActualWidth : frameworkElement5.Width) + num4, 0.0);
					break;
				}
				case AniTypeSub.Height:
				{
					FrameworkElement frameworkElement4 = (FrameworkElement)ani.obj;
					frameworkElement4.Height = Math.Max((double.IsNaN(frameworkElement4.Height) ? frameworkElement4.ActualHeight : frameworkElement4.Height) + num4, 0.0);
					break;
				}
				case AniTypeSub.Value:
					((dynamic)ani.obj).Value += num4;
					break;
				case AniTypeSub.Radius:
					((dynamic)ani.obj).Radius += num4;
					break;
				case AniTypeSub.StrokeThickness:
					((dynamic)ani.obj).StrokeThickness = Math.Max(Convert.ToDouble(((dynamic)ani.obj).StrokeThickness) + num4, 0);
					break;
				case AniTypeSub.BorderThickness:
					((dynamic)ani.obj).BorderThickness = new Thickness(((Thickness)((dynamic)ani.obj).BorderThickness).Bottom + num4);
					break;
				case AniTypeSub.TranslateX:
					if ((object)((dynamic)ani.obj).RenderTransform == null || !(((dynamic)ani.obj).RenderTransform is TranslateTransform))
					{
						((dynamic)ani.obj).RenderTransform = new TranslateTransform(0.0, 0.0);
					}
					((TranslateTransform)((dynamic)ani.obj).RenderTransform).X += num4;
					break;
				case AniTypeSub.TranslateY:
					if ((object)((dynamic)ani.obj).RenderTransform == null || !(((dynamic)ani.obj).RenderTransform is TranslateTransform))
					{
						((dynamic)ani.obj).RenderTransform = new TranslateTransform(0.0, 0.0);
					}
					((TranslateTransform)((dynamic)ani.obj).RenderTransform).Y += num4;
					break;
				case AniTypeSub.Double:
					((dynamic)ani.obj)[0].SetValue(((dynamic)ani.obj)[1], Convert.ToDouble(((dynamic)ani.obj)[0].GetValue(((dynamic)ani.obj)[1])) + num4);
					break;
				case AniTypeSub.DoubleParam:
					((ParameterizedThreadStart)ani.obj)(num4);
					break;
				case AniTypeSub.GridLengthWidth:
					((dynamic)ani.obj).Width = new GridLength(Convert.ToDouble(Math.Max(Convert.ToDouble(((dynamic)ani.obj).Width.Value) + num4, 0)), GridUnitType.Star);
					break;
				}
				break;
			}
			case AniType.Color:
			{
				ModBase.MyColor myColor = ModBase.MathPercent(new ModBase.MyColor(0.0, 0.0, 0.0, 0.0), (ModBase.MyColor)ani.value, ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, ani.timePercent)) + (ModBase.MyColor)ani.valueLast;
				FrameworkElement frameworkElement2 = (FrameworkElement)((dynamic)ani.obj)[0];
				DependencyProperty val = (DependencyProperty)((dynamic)ani.obj)[1];
				ModBase.MyColor myColor2 = new ModBase.MyColor(((DependencyObject)frameworkElement2).GetValue(val)) + myColor;
				((DependencyObject)frameworkElement2).SetValue(val, (val.PropertyType.Name == "Color") ? ((object)(Color)myColor2) : ((SolidColorBrush)myColor2));
				ani.valueLast = myColor2 - new ModBase.MyColor(((DependencyObject)frameworkElement2).GetValue(val));
				break;
			}
			case AniType.Scale:
			{
				FrameworkElement frameworkElement6 = (FrameworkElement)ani.obj;
				double delta = ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, ani.timePercent);
				frameworkElement6.Margin = new Thickness(frameworkElement6.Margin.Left + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Left), delta), frameworkElement6.Margin.Top + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Top), delta), frameworkElement6.Margin.Right + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Left), delta), frameworkElement6.Margin.Bottom + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Top), delta));
				frameworkElement6.Width = Math.Max(frameworkElement6.Width + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Width), delta), 0.0);
				frameworkElement6.Height = Math.Max(frameworkElement6.Height + ModBase.MathPercent(0.0, Convert.ToDouble(((dynamic)ani.value).Height), delta), 0.0);
				break;
			}
			case AniType.TextAppear:
			{
				bool flag = (bool)((dynamic)ani.value)[1];
				dynamic val2 = ((dynamic)ani.value)[0].ToString().Length;
				int num2 = (int)Math.Round((double)(flag ? val2 : ((object)0)) + Math.Round(val2 * ((!flag) ? 1 : (-1)) * ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, 0.0)));
				dynamic val3 = ((dynamic)ani.value)[0].ToString();
				dynamic val4 = val3.Substring(0, Math.Min(num2, val3.Length));
				if (num2 < val3.Length)
				{
					dynamic val5 = val3.Substring(num2, 1);
					if (!((Convert.ToInt32(Convert.ToChar(val5)) >= Convert.ToInt32(Convert.ToChar(128))) ? true : false))
					{
						val4 += RandomUtils.PickRandom<char>((ICollection<char>)"0123456789./*-+\\[]{};':/?,!@#$%^&*()_+-=qwwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray());
					}
					else
					{
						val4 += Encoding.GetEncoding("GB18030").GetString(new byte[2]
						{
							(byte)RandomUtils.NextInt(176, 247),
							(byte)RandomUtils.NextInt(161, 249)
						});
					}
				}
				if (ani.obj is TextBlock)
				{
					((dynamic)ani.obj).Text = val4;
				}
				else
				{
					((dynamic)ani.obj).Context = val4;
				}
				break;
			}
			case AniType.Code:
				((ThreadStart)ani.value)();
				break;
			case AniType.ScaleTransform:
			{
				FrameworkElement frameworkElement3 = (FrameworkElement)ani.obj;
				if (!(frameworkElement3.RenderTransform is ScaleTransform))
				{
					frameworkElement3.RenderTransformOrigin = new Point(0.5, 0.5);
					frameworkElement3.RenderTransform = new ScaleTransform(1.0, 1.0);
				}
				double num3 = ModBase.MathPercent(0.0, (double)ani.value, ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, ani.timePercent));
				((ScaleTransform)frameworkElement3.RenderTransform).ScaleX = Math.Max(((ScaleTransform)frameworkElement3.RenderTransform).ScaleX + num3, 0.0);
				((ScaleTransform)frameworkElement3.RenderTransform).ScaleY = Math.Max(((ScaleTransform)frameworkElement3.RenderTransform).ScaleY + num3, 0.0);
				break;
			}
			case AniType.RotateTransform:
			{
				FrameworkElement frameworkElement = (FrameworkElement)ani.obj;
				if (!(frameworkElement.RenderTransform is RotateTransform))
				{
					frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
					frameworkElement.RenderTransform = new RotateTransform(0.0);
				}
				double num = ModBase.MathPercent(0.0, (double)ani.value, ani.ease.GetDelta((double)ani.timeFinished / (double)ani.timeTotal, ani.timePercent));
				((RotateTransform)frameworkElement.RenderTransform).Angle = ((RotateTransform)frameworkElement.RenderTransform).Angle + num;
				break;
			}
			}
			ani.timePercent = (double)ani.timeFinished / (double)ani.timeTotal;
		}
		catch (Exception ex)
		{
			AniData aniData = ani;
			ModBase.Log(ex, "执行动画失败：" + aniData.ToString(), ModBase.LogLevel.Hint, null, Lang.Text("Application.Animation.Error.OperationFailed"));
		}
		return ani;
	}

	public static AniData AaX(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.X,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaY(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Y,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaWidth(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Width,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaHeight(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Height,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaOpacity(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Opacity,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaValue(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Value,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaRadius(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Radius,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaBorderThickness(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.BorderThickness,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaStrokeThickness(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.StrokeThickness,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaGridLengthWidth(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.GridLengthWidth,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaDouble(object obj, DependencyProperty prop, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.Double,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = new object[3] { obj, prop, "" },
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaDouble(ParameterizedThreadStart lambda, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.DoubleParam,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = lambda,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaColor(FrameworkElement obj, DependencyProperty prop, ModBase.MyColor value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Color,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = new object[3] { obj, prop, "" },
			value = value,
			isAfter = after,
			timeFinished = -delay,
			valueLast = new ModBase.MyColor(0.0, 0.0, 0.0, 0.0)
		};
	}

	public static AniData AaColor(FrameworkElement obj, DependencyProperty prop, string res, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Color,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = new object[3] { obj, prop, res },
			value = new ModBase.MyColor(System.Windows.Application.Current.FindResource(res)) - new ModBase.MyColor(((DependencyObject)obj).GetValue(prop)),
			isAfter = after,
			timeFinished = -delay,
			valueLast = new ModBase.MyColor(0.0, 0.0, 0.0, 0.0)
		};
	}

	public static AniData AaScale(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false, bool absolute = false)
	{
		ModBase.MyRect value2 = ((!absolute) ? new ModBase.MyRect(Convert.ToDouble(-0.5 * ((dynamic)obj).ActualWidth * value), Convert.ToDouble(-0.5 * ((dynamic)obj).ActualHeight * value), Convert.ToDouble(((dynamic)obj).ActualWidth * value), Convert.ToDouble(((dynamic)obj).ActualHeight * value)) : new ModBase.MyRect(-0.5 * value, -0.5 * value, value, value));
		return new AniData
		{
			typeMain = AniType.Scale,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value2,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaTextAppear(object obj, bool hide = false, bool timePerText = true, int time = 70, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.TextAppear,
			ease = (ease ?? new AniEaseLinear()),
			timeTotal = (timePerText ? (time * ((obj is TextBlock) ? ((dynamic)obj).Text : ((dynamic)obj).Context.ToString()).ToString().Length) : ((object)time)),
			obj = obj,
			value = new object[2]
			{
				(obj is TextBlock) ? ((dynamic)obj).Text : ((dynamic)obj).Context.ToString(),
				hide
			},
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaCode(ThreadStart code, int delay = 0, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Code,
			timeTotal = 1,
			value = code,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaScaleTransform(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.ScaleTransform,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaRotateTransform(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.RotateTransform,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaTranslateX(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.TranslateX,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static AniData AaTranslateY(object obj, double value, int time = 400, int delay = 0, AniEase ease = null, bool after = false)
	{
		return new AniData
		{
			typeMain = AniType.Number,
			typeSub = AniTypeSub.TranslateY,
			timeTotal = time,
			ease = (ease ?? new AniEaseLinear()),
			obj = obj,
			value = value,
			isAfter = after,
			timeFinished = -delay
		};
	}

	public static List<AniData> AaStack(StackPanel stack, int time = 100, int delay = 25)
	{
		List<AniData> list = null;
		list = new List<AniData>();
		int num = 0;
		foreach (dynamic child in stack.Children)
		{
			child.Opacity = 0;
			list.Add(AaOpacity((object)child, 1.0, time, num));
			num += delay;
		}
		return list;
	}

	public static void AniStart(IList aniGroup, string name = "", bool refreshTime = false)
	{
		if (refreshTime)
		{
			aniLastTick = TimeUtils.GetTimeTick();
		}
		AniGroupEntry aniGroupEntry = new AniGroupEntry
		{
			data = ModBase.GetFullList<AniData>(aniGroup),
			startTick = TimeUtils.GetTimeTick()
		};
		if (string.IsNullOrEmpty(name))
		{
			name = aniGroupEntry.Uuid.ToString();
		}
		else
		{
			AniStop(name);
		}
		aniGroups.TryAdd(name, aniGroupEntry);
	}

	public static void AniStart(AniData aniGroup, string name = "", bool refreshTime = false)
	{
		AniStart(new List<AniData> { aniGroup }, name, refreshTime);
	}

	public static void AniStop(string name)
	{
		aniGroups.Remove(name, out var _);
	}

	public static bool AniIsRun(string name)
	{
		return aniGroups.ContainsKey(name);
	}
}

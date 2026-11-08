using System;
using System.Windows;
using System.Windows.Controls;

namespace PCL.Core.UI.Controls;

public class WaterfallPanel : Panel
{
	private int _computedColumnCount;

	public static readonly DependencyProperty LayoutModeProperty = DependencyProperty.Register("LayoutMode", typeof(WaterfallLayoutMode), typeof(WaterfallPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)WaterfallLayoutMode.FixedColumns, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.Register("ColumnCount", typeof(int), typeof(WaterfallPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)2, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register("ColumnSpacing", typeof(double), typeof(WaterfallPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register("RowSpacing", typeof(double), typeof(WaterfallPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty MaxItemWidthProperty = DependencyProperty.Register("MaxItemWidth", typeof(double), typeof(WaterfallPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public WaterfallLayoutMode LayoutMode
	{
		get
		{
			return (WaterfallLayoutMode)((DependencyObject)this).GetValue(LayoutModeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(LayoutModeProperty, (object)value);
		}
	}

	public int ColumnCount
	{
		get
		{
			return (int)((DependencyObject)this).GetValue(ColumnCountProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ColumnCountProperty, (object)value);
		}
	}

	public double ColumnSpacing
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(ColumnSpacingProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ColumnSpacingProperty, (object)value);
		}
	}

	public double RowSpacing
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(RowSpacingProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(RowSpacingProperty, (object)value);
		}
	}

	public double MaxItemWidth
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(MaxItemWidthProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(MaxItemWidthProperty, (object)value);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		int num;
		if (LayoutMode == WaterfallLayoutMode.FixedColumns)
		{
			num = ColumnCount;
		}
		else if (double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width))
		{
			num = ColumnCount;
		}
		else
		{
			double num2 = ((MaxItemWidth > 0.0) ? MaxItemWidth : 100.0) + ColumnSpacing;
			num = Math.Max(1, (int)Math.Floor((availableSize.Width + ColumnSpacing) / num2));
		}
		_computedColumnCount = num;
		double val = (availableSize.Width - (double)(num - 1) * ColumnSpacing) / (double)num;
		val = Math.Max(0.0, val);
		double[] array = new double[num];
		foreach (UIElement internalChild in InternalChildren)
		{
			internalChild.Measure(new Size(val, double.PositiveInfinity));
			Size desiredSize = internalChild.DesiredSize;
			int num3 = _GetMinHeightColumnIndex(array);
			_ = array[num3];
			array[num3] += desiredSize.Height + RowSpacing;
		}
		double num4 = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			num4 = ((!(array[i] > 0.0)) ? Math.Max(num4, array[i]) : Math.Max(num4, array[i] - RowSpacing));
		}
		return new Size(availableSize.Width, num4);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		if (_computedColumnCount <= 0)
		{
			return finalSize;
		}
		double val = (finalSize.Width - (double)(_computedColumnCount - 1) * ColumnSpacing) / (double)_computedColumnCount;
		val = Math.Max(0.0, val);
		double[] array = new double[_computedColumnCount];
		foreach (UIElement internalChild in InternalChildren)
		{
			int num = _GetMinHeightColumnIndex(array);
			double num2 = (double)num * (val + ColumnSpacing);
			double num3 = array[num];
			Point val2 = new Point(num2, num3);
			double num4 = val;
			Size desiredSize = internalChild.DesiredSize;
			internalChild.Arrange(new Rect(val2, new Size(num4, desiredSize.Height)));
			ref double reference = ref array[num];
			double num5 = reference;
			desiredSize = internalChild.DesiredSize;
			reference = num5 + (desiredSize.Height + RowSpacing);
		}
		return finalSize;
	}

	private int _GetMinHeightColumnIndex(double[] colHeights)
	{
		int result = 0;
		double num = colHeights[0];
		for (int i = 1; i < colHeights.Length; i++)
		{
			if (colHeights[i] < num)
			{
				num = colHeights[i];
				result = i;
			}
		}
		return result;
	}
}

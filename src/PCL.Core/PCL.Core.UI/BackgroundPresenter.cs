using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace PCL.Core.UI;

public class BackgroundPresenter : FrameworkElement
{
	private delegate void GetContentBoundsDelegate(VisualBrush visualBrush, out Rect bounds);

	private static readonly FieldInfo _DrawingContentOfUIElement = typeof(UIElement).GetField("_drawingContent", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _ContentOfDrawingVisual = typeof(DrawingVisual).GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _OffsetOfVisual = typeof(Visual).GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly Func<UIElement, DrawingContext> _RenderOpenMethod = (Func<UIElement, DrawingContext>)typeof(UIElement).GetMethod("RenderOpen", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<UIElement, DrawingContext>));

	private static readonly Action<UIElement, DrawingContext> _OnRenderMethod = (Action<UIElement, DrawingContext>)typeof(UIElement).GetMethod("OnRender", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Action<UIElement, DrawingContext>));

	private static readonly GetContentBoundsDelegate _MethodGetContentBounds = (GetContentBoundsDelegate)typeof(VisualBrush).GetMethod("GetContentBounds", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(GetContentBoundsDelegate));

	private readonly Stack<UIElement> _parentStack = new Stack<UIElement>();

	public static readonly DependencyProperty MaxDepthProperty = DependencyProperty.Register("MaxDepth", typeof(int), typeof(BackgroundPresenter), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)16));

	public int MaxDepth
	{
		get
		{
			return (int)((DependencyObject)this).GetValue(MaxDepthProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(MaxDepthProperty, (object)value);
		}
	}

	public static void ForceRender(UIElement target)
	{
		using DrawingContext arg = _RenderOpenMethod(target);
		_OnRenderMethod(target, arg);
	}

	internal static void DrawVisual(DrawingContext drawingContext, Visual visual, Point relatedXY)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		VisualBrush visualBrush = new VisualBrush(visual);
		Vector val = (Vector)_OffsetOfVisual.GetValue(visual);
		_MethodGetContentBounds(visualBrush, out var bounds);
		relatedXY -= val;
		if (!bounds.IsEmpty)
		{
			drawingContext.DrawRectangle(visualBrush, null, new Rect(relatedXY.X + bounds.X, relatedXY.Y + bounds.Y, bounds.Width, bounds.Height));
		}
	}

	protected override Geometry GetLayoutClip(Size layoutSlotSize)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight));
	}

	protected override void OnVisualParentChanged(DependencyObject oldParentObject)
	{
		if (oldParentObject is UIElement uIElement)
		{
			uIElement.LayoutUpdated -= ParentLayoutUpdated;
		}
		if (Parent is UIElement uIElement2)
		{
			uIElement2.LayoutUpdated += ParentLayoutUpdated;
		}
	}

	private void ParentLayoutUpdated(object? sender, EventArgs e)
	{
		ForceRender(this);
	}

	internal static void DrawBackground(DrawingContext drawingContext, UIElement self, Stack<UIElement> parentStackStorage, int maxDepth, bool throwExceptionIfParentArranging)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		bool isInDesignMode = DesignerProperties.GetIsInDesignMode((DependencyObject)(object)self);
		UIElement uIElement = VisualTreeHelper.GetParent((DependencyObject)(object)self) as UIElement;
		Size renderSize;
		while (uIElement != null && parentStackStorage.Count < maxDepth)
		{
			if (!uIElement.IsVisible)
			{
				parentStackStorage.Clear();
				return;
			}
			if (isInDesignMode && ((object)uIElement).GetType().ToString().Contains("VisualStudio"))
			{
				break;
			}
			renderSize = uIElement.RenderSize;
			if (renderSize.Width != 0.0)
			{
				renderSize = uIElement.RenderSize;
				if (renderSize.Height != 0.0)
				{
					parentStackStorage.Push(uIElement);
					uIElement = VisualTreeHelper.GetParent((DependencyObject)(object)uIElement) as UIElement;
					continue;
				}
			}
			parentStackStorage.Clear();
			if (throwExceptionIfParentArranging)
			{
				throw new InvalidOperationException("Arranging");
			}
			self.InvalidateArrange();
			return;
		}
		renderSize = self.RenderSize;
		double width = renderSize.Width;
		renderSize = self.RenderSize;
		Rect val = new Rect(0.0, 0.0, width, renderSize.Height);
		while (parentStackStorage.Count > 0)
		{
			UIElement uIElement2 = parentStackStorage.Pop();
			UIElement uIElement3 = self;
			if (parentStackStorage.Count > 0)
			{
				uIElement3 = parentStackStorage.Peek();
			}
			Point relatedXY = uIElement2.TranslatePoint(default, self);
			object value = _DrawingContentOfUIElement.GetValue(uIElement2);
			if (value != null)
			{
				DrawingVisual drawingVisual = new DrawingVisual();
				_ContentOfDrawingVisual.SetValue(drawingVisual, value);
				DrawVisual(drawingContext, drawingVisual, relatedXY);
			}
			int childrenCount = VisualTreeHelper.GetChildrenCount((DependencyObject)(object)uIElement2);
			for (int i = 0; i < childrenCount; i++)
			{
				if (VisualTreeHelper.GetChild((DependencyObject)(object)uIElement2, i) is UIElement uIElement4)
				{
					if (uIElement4 == uIElement3)
					{
						break;
					}
					Point val2 = uIElement4.TranslatePoint(default, self);
					Rect val3 = new Rect(val2, uIElement4.RenderSize);
					if (val.IntersectsWith(val3) && uIElement4.IsVisible)
					{
						DrawVisual(drawingContext, uIElement4, val2);
					}
				}
			}
		}
	}

	public static void DrawBackground(DrawingContext drawingContext, UIElement self)
	{
		Stack<UIElement> parentStackStorage = new Stack<UIElement>();
		DrawBackground(drawingContext, self, parentStackStorage, int.MaxValue, throwExceptionIfParentArranging: true);
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		DrawBackground(drawingContext, this, _parentStack, MaxDepth, throwExceptionIfParentArranging: false);
	}
}

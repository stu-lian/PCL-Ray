using System;
using System.Windows;
using System.Windows.Controls;

namespace PCL;

public class MyVirtualizingElement<T> : FrameworkElement where T : FrameworkElement
{
	private readonly Func<T> _initializer;

	public MyVirtualizingElement(Func<T> initializer)
	{
		_initializer = initializer;
		this.EnableLazyLoad(() =>
		{
			Init();
		});
	}

	public T Init()
	{
		T val = _initializer();
		if (Parent != null)
		{
			if (!(Parent is Panel))
			{
				throw new Exception("MyVirtualizingElement 的父级必须是一个 Panel");
			}
			Panel panel = (Panel)(object)Parent;
			int index = panel.Children.IndexOf(this);
			panel.Children.RemoveAt(index);
			panel.Children.Insert(index, val);
		}
		return val;
	}

	public static implicit operator T(MyVirtualizingElement<T> virtualized)
	{
		return virtualized.Init();
	}
}
public class MyVirtualizingElement : FrameworkElement
{
	private readonly Func<FrameworkElement> _initializer;

	public MyVirtualizingElement(Func<FrameworkElement> initializer)
	{
		_initializer = initializer;
		this.EnableLazyLoad(() =>
		{
			Init();
		});
	}

	public FrameworkElement Init()
	{
		FrameworkElement frameworkElement = _initializer();
		if (Parent != null)
		{
			if (!(Parent is Panel))
			{
				throw new Exception("MyVirtualizingElement 的父级必须是一个 Panel");
			}
			Panel panel = (Panel)(object)Parent;
			int index = panel.Children.IndexOf(this);
			panel.Children.RemoveAt(index);
			panel.Children.Insert(index, frameworkElement);
		}
		return frameworkElement;
	}

	public static FrameworkElement TryInit(FrameworkElement element)
	{
		if (typeof(MyVirtualizingElement<>).IsInstanceOfGenericType(element))
		{
			return (FrameworkElement)((object)element).GetType().GetMethod("Init", Type.EmptyTypes).Invoke(element, null);
		}
		if (!(element is MyVirtualizingElement))
		{
			return element;
		}
		return ((MyVirtualizingElement)element).Init();
	}
}

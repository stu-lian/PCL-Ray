using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace PCL.Core.UI;

public class IconManager : INotifyPropertyChanged
{
	private readonly Dictionary<string, IconModel> _iconIndex = new Dictionary<string, IconModel>();

	public IconModel? SelectedIcon
	{
		get;
		set
		{
			field = value;
			OnPropertyChanged("SelectedIcon");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public bool SetSelectedIconByName(string name)
	{
		if (_iconIndex.TryGetValue(name, out IconModel value))
		{
			SelectedIcon = value;
			return true;
		}
		return false;
	}

	public bool AddIconFromXaml(string name, string xamlString)
	{
		if (string.IsNullOrWhiteSpace(name) || _iconIndex.ContainsKey(name))
		{
			return false;
		}
		if (TryLoadIconFromXaml(xamlString, out UIElement icon))
		{
			IconModel value = new IconModel(name, icon);
			_iconIndex[name] = value;
			return true;
		}
		return false;
	}

	public void RemoveIconByName(string name)
	{
		_iconIndex.Remove(name);
	}

	public static bool TryLoadIconFromXaml(string xamlString, out UIElement? icon)
	{
		icon = null;
		if (string.IsNullOrWhiteSpace(xamlString))
		{
			return false;
		}
		if (!((DispatcherObject)Application.Current).Dispatcher.CheckAccess())
		{
			return false;
		}
		try
		{
			icon = (UIElement)XamlReader.Parse(xamlString);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool LoadIconFromXaml(string xamlString, out UIElement? icon)
	{
		icon = null;
		if (string.IsNullOrWhiteSpace(xamlString))
		{
			throw new ArgumentNullException("xamlString", "XAML 字符串不能为空或空白。");
		}
		if (!((DispatcherObject)Application.Current).Dispatcher.CheckAccess())
		{
			throw new InvalidOperationException("XAML 解析需要在 UI 线程执行。");
		}
		icon = (UIElement)XamlReader.Parse(xamlString);
		return true;
	}

	protected void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

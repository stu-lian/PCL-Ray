using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.UI.Theme;

namespace PCL;

public static class ThemeManager
{
	private static bool _contextMenuHandlerRegistered;

	public static ModBase.MyColor colorGray1 = new ModBase.MyColor(AppResources["ColorObjectGray1"]);

	public static ModBase.MyColor colorGray4 = new ModBase.MyColor(AppResources["ColorObjectGray4"]);

	public static ModBase.MyColor colorGray5 = new ModBase.MyColor(AppResources["ColorObjectGray5"]);

	public static ModBase.MyColor colorSemiTransparent = new ModBase.MyColor(AppResources["ColorBrushSemiTransparent"]);

	public static bool IsDarkMode => ThemeService.IsDarkMode;

	public static ResourceDictionary AppResources => System.Windows.Application.Current.Resources;

	public static void ThemeRefresh(int newTheme = -1)
	{
		colorGray1 = new ModBase.MyColor(AppResources["ColorObjectGray1"]);
		colorGray4 = new ModBase.MyColor(AppResources["ColorObjectGray4"]);
		colorGray5 = new ModBase.MyColor(AppResources["ColorObjectGray5"]);
		colorSemiTransparent = new ModBase.MyColor(AppResources["ColorBrushSemiTransparent"]);
		ThemeRefreshMain();
	}

	public static void ThemeRefreshMain()
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmMain.IsLoaded)
			{
				RefreshBackground();
				RefreshAllContextMenuThemes();
			}
		});
	}

	private static void RefreshBackground()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (Config.Preference.Background.BackgroundColorful)
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				EndPoint = new Point(0.1, 1.0),
				StartPoint = new Point(0.9, 0.0)
			};
			int item = ThemeService.GetCurrentThemeArgs().Item1;
			int num = item - 15;
			int num2 = item + 15;
			ToneProfile currentTone = ThemeService.CurrentTone;
			double num3 = (IsDarkMode ? 0.2 : 1.0);
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = -0.1,
				Color = (Color)LabColor.FromLch(0.84 * num3, currentTone.C5, (double)num, 1.0)
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = 0.4,
				Color = (Color)LabColor.FromLch(0.96 * num3, currentTone.C7, (double)item, 1.0)
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = 1.1,
				Color = (Color)LabColor.FromLch(0.84 * num3, currentTone.C5, (double)num2, 1.0)
			});
			ModMain.frmMain.PanForm.Background = linearGradientBrush;
		}
		else
		{
			ModMain.frmMain.PanForm.Background = (Brush)System.Windows.Application.Current.Resources["ColorBrushBackground"];
		}
		((Freezable)ModMain.frmMain.PanForm.Background).Freeze();
	}

	private static void RefreshAllContextMenuThemes()
	{
		try
		{
			if (!_contextMenuHandlerRegistered)
			{
				EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent, new RoutedEventHandler(OnContextMenuOpened));
				_contextMenuHandlerRegistered = true;
			}
			foreach (Window window in System.Windows.Application.Current.Windows)
			{
				RefreshContextMenusInElement((DependencyObject)(object)window);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新ContextMenu主题时出错");
		}
	}

	private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
	{
		try
		{
			if (sender is ContextMenu contextMenu)
			{
				((DependencyObject)contextMenu).ClearValue(FrameworkElement.StyleProperty);
				contextMenu.UpdateDefaultStyle();
			}
		}
		catch
		{
		}
	}

	private static void RefreshContextMenusInElement(DependencyObject element)
	{
		if (element == null)
		{
			return;
		}
		try
		{
			if (element is FrameworkElement { ContextMenu: not null } frameworkElement)
			{
				((DependencyObject)frameworkElement.ContextMenu).ClearValue(FrameworkElement.StyleProperty);
				frameworkElement.ContextMenu.UpdateDefaultStyle();
			}
			int childrenCount = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < childrenCount; i++)
			{
				RefreshContextMenusInElement(VisualTreeHelper.GetChild(element, i));
			}
		}
		catch
		{
		}
	}
}

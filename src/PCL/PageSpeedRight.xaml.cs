using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

namespace PCL;

public partial class PageSpeedRight : MyPageRight, IComponentConnector
{
	public PageSpeedRight()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
	}

	private void Init()
	{
		PanBack.ScrollToHome();
	}

}

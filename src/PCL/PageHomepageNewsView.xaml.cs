using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Markup;
using PCL.Core.ViewModel.Homepage;

namespace PCL;

public partial class PageHomepageNewsView : MyPageRight, IComponentConnector
{
	public PageHomepageNewsView()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected Obj, but got Unknown
		InitializeComponent();
		DataContext = (object)new NewsViewModel();
	}

}

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace PCL;

public partial class MinecraftServerQuery : Grid, IComponentConnector
{
	public MinecraftServerQuery()
	{
		InitializeComponent();
		BtnServerQuery.Click += BtnServerQuery_Click;
	}

	private void BtnServerQuery_Click(object sender, MouseButtonEventArgs e)
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(() => ServerQueryAsync()), Array.Empty<object>());
	}

	private async Task ServerQueryAsync()
	{
		await PanMcServer.UpdateServerInfoAsync(LabServerIp.Text);
		ServerInfo.Visibility = Visibility.Visible;
	}

}

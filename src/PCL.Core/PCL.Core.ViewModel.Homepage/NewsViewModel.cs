using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using PCL.Core.App;
using PCL.Core.IO.Net.Http;
using PCL.Core.Logging;
using PCL.Core.Model.Tools.News;

namespace PCL.Core.ViewModel.Homepage;

public class NewsViewModel : ObservableObject
{
	private const string BaseApiUrl = "https://net-secondary.web.minecraft-services.net/api/v1.0/zh-cn/search";

	private const int PageSize = 24;

	private static readonly string[] AllowedNewsLinkHosts = new string[3] { "minecraft.net", "minecraft-services.net", "microsoft.com" };

	private int _currentPage = 1;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? __LoadDataCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string?>? __OpenReadCommand;

	public ObservableCollection<NewsItem> NewsItems { get; } = new ObservableCollection<NewsItem>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isLoading, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsLoading);
				_isLoading = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLoading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_errorMessage, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ErrorMessage);
				_errorMessage = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ErrorMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand _LoadDataCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected Obj, but got Unknown
			//IL_0023: Expected Obj, but got Unknown
			AsyncRelayCommand obj = __LoadDataCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)_LoadDataAsync);
				AsyncRelayCommand val2 = val;
				__LoadDataCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> _OpenReadCommand => (IRelayCommand<string?>)(object)(__OpenReadCommand ?? (__OpenReadCommand = new RelayCommand<string>((Action<string>)_OpenRead)));

	public NewsViewModel()
	{
		((ICommand)_LoadDataCommand).Execute((object?)null);
	}

	[RelayCommand]
	private async Task _LoadDataAsync()
	{
		if (IsLoading)
		{
			return;
		}
		IsLoading = true;
		try
		{
			using HttpResponseMessage resp = await HttpRequest.Create($"{"https://net-secondary.web.minecraft-services.net/api/v1.0/zh-cn/search"}?pageSize={24}&sortType=Recent&category=News&newsOnly=true&page={_currentPage}").SendAsync();
			resp.EnsureSuccessStatusCode();
			ApiResponse apiResponse = await resp.AsJsonAsync<ApiResponse>();
			if (apiResponse?.Result?.Results == null)
			{
				return;
			}
			foreach (NewsItem result in apiResponse.Result.Results)
			{
				result.Description = WebUtility.HtmlDecode(result.Description);
				NewsItems.Add(result);
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = "加载错误: " + ex.Message;
			LogWrapper.Error(ex, "Minecrft 信息流主页加载失败");
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private void _OpenRead(string? url)
	{
		if (!IsSafeNewsLink(url))
		{
			LogWrapper.Warn("Homepage", "已拦截不安全的 Minecraft 信息流链接：" + (url ?? "<null>"));
		}
		else
		{
			Basics.OpenPath(url);
		}
	}

	internal static bool IsSafeNewsLink(string? url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
		{
			return false;
		}
		if (!result.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !result.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string idnHost = result.IdnHost;
		string[] allowedNewsLinkHosts = AllowedNewsLinkHosts;
		foreach (string text in allowedNewsLinkHosts)
		{
			if (idnHost.Equals(text, StringComparison.OrdinalIgnoreCase) || idnHost.EndsWith("." + text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}

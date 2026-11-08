using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadClient : MyPageRight, IComponentConnector
{
	public PageDownloadClient()
	{
		Initialized += (object? _, EventArgs _) =>
		{
			LoaderInit();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
		InitializeComponent();
	}

	private void LoaderInit()
	{
		PageLoaderInit(Load, PanLoad, PanBack, null, ModDownload.dlClientListLoader, (ModLoader.LoaderBase _) =>
		{
			Load_OnFinish();
		});
	}

	private void Init()
	{
		PanBack.ScrollToHome();
	}

	private void Load_OnFinish()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			McVersionCategory[] array = new McVersionCategory[4] { McVersionCategory.Release, McVersionCategory.Snapshot, McVersionCategory.BeforeRelease, McVersionCategory.AprilFools };
			
			McVersionCategory[] array2 = array;
			Dictionary<McVersionCategory, List<JsonObject>> dictionary = array2.ToDictionary((McVersionCategory category) =>
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				return category;
			}, (McVersionCategory _) => new List<JsonObject>());
			foreach (JsonObject item in (JsonArray)ModDownload.dlClientListLoader.output.Value["versions"])
			{
				McVersionCategory key = McVersionClassifier.ClassifyVersion(item);
				dictionary[key].Add(item);
			}
			McVersionCategory[] array3 = array2;
			foreach (McVersionCategory key2 in array3)
			{
				dictionary[key2] = ((IEnumerable<JsonObject>)dictionary[key2]).OrderByDescending((Func<JsonObject, DateTime>)McVersionClassifier.GetReleaseTime).ToList();
			}
			PanMain.Children.Clear();
			MyCard myCard = new MyCard
			{
				Title = Lang.Text("Download.Version.Latest.Title"),
				Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
			};
			List<JsonObject> list = new List<JsonObject>();
			JsonObject jsonObject2 = (JsonObject)dictionary[(McVersionCategory)0][0].DeepClone();
			jsonObject2["lore"] = Lang.Text("Download.Version.Latest.Release", new object[1] { Lang.Date(McVersionClassifier.GetReleaseTime(jsonObject2), "g") });
			list.Add(jsonObject2);
			if (McVersionClassifier.GetReleaseTime(dictionary[(McVersionCategory)0][0]) < McVersionClassifier.GetReleaseTime(dictionary[(McVersionCategory)1][0]))
			{
				JsonObject jsonObject3 = (JsonObject)dictionary[(McVersionCategory)1][0].DeepClone();
				jsonObject3["lore"] = Lang.Text("Download.Version.Latest.Development", new object[1] { Lang.Date(McVersionClassifier.GetReleaseTime(jsonObject3), "g") });
				list.Add(jsonObject3);
			}
			StackPanel stack = new StackPanel
			{
				Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
				VerticalAlignment = VerticalAlignment.Top,
				RenderTransform = new TranslateTransform(0.0, 0.0),
				Tag = list
			};
			MyCard.StackInstall(ref stack, PutMethod);
			myCard.Children.Add(stack);
			PanMain.Children.Add(myCard);
			foreach (KeyValuePair<McVersionCategory, List<JsonObject>> item2 in dictionary)
			{
				if (item2.Value.Any())
				{
					MyCard myCard2 = new MyCard
					{
						Title = McVersionClassifier.GetCategoryDisplayName(item2.Key) + " (" + item2.Value.Count + ")",
						Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
					};
					StackPanel stackPanel = new StackPanel
					{
						Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
						VerticalAlignment = VerticalAlignment.Top,
						RenderTransform = new TranslateTransform(0.0, 0.0),
						Tag = item2.Value
					};
					myCard2.Children.Add(stackPanel);
					myCard2.SwapControl = stackPanel;
					myCard2.InstallMethod = PutMethod;
					myCard2.IsSwapped = true;
					PanMain.Children.Add(myCard2);
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 MC 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
		static void PutMethod(StackPanel stackPanel2)
		{
			foreach (object item3 in (IEnumerable)stackPanel2.Tag)
			{
				stackPanel2.Children.Add(ModDownloadLib.McDownloadListItem((JsonObject)item3, ModDownloadLib.McDownloadMenuSave, isSaveOnly: true));
			}
		}
	}

}

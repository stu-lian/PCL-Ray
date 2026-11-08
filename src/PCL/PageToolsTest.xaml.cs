using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO;
using PCL.Core.IO.Net;
using PCL.Core.UI;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;
using PCL.Core.Utils.Validate;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageToolsTest : MyPageRight, IComponentConnector
{
	private Bitmap currentSkinBitmap;

	private Bitmap generatedHeadBitmap;

	private int headSize = 64;

	private string skinPath = "";

	public PageToolsTest()
	{
		InitializeComponent();
		BtnSelectSkin.Click += BtnSelectSkin_Click;
		CmbHeadSize.SelectionChanged += CmbHeadSize_SelectionChanged;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			MeLoaded();
		};
	}

	private void MeLoaded()
	{
		BtnDownloadStart.IsEnabled = false;
		TextDownloadFolder.Text = States.Tool.DownloadFolder;
		TextDownloadFolder.Validate();
		if (!string.IsNullOrEmpty(TextDownloadFolder.ValidateResult) || string.IsNullOrEmpty(TextDownloadFolder.Text))
		{
			TextDownloadFolder.Text = ModBase.exePath + "PCL\\MyDownload\\";
		}
		TextDownloadFolder.Validate();
		TextDownloadName.Validate();
		TextDownloadUrl.Validate();
		StartButtonRefresh();
	}

	private void StartButtonRefresh()
	{
		BtnDownloadStart.IsEnabled = string.IsNullOrEmpty(TextDownloadFolder.ValidateResult) && string.IsNullOrEmpty(TextDownloadUrl.ValidateResult) && string.IsNullOrEmpty(TextDownloadName.ValidateResult);
		BtnDownloadOpen.IsEnabled = string.IsNullOrEmpty(TextDownloadFolder.ValidateResult);
		BtnAchievementPreview.IsEnabled = string.IsNullOrEmpty(AchievementBlockTextBox.ValidateResult) && string.IsNullOrEmpty(AchievementTitleTextBox.ValidateResult) && string.IsNullOrEmpty(AchievementString1TextBox.ValidateResult);
		BtnAchievementSave.IsEnabled = string.IsNullOrEmpty(AchievementBlockTextBox.ValidateResult) && string.IsNullOrEmpty(AchievementTitleTextBox.ValidateResult) && string.IsNullOrEmpty(AchievementString1TextBox.ValidateResult);
	}

	private void SaveCacheDownloadFolder(object sender, RoutedEventArgs e)
	{
		States.Tool.DownloadFolder = TextDownloadFolder.Text;
		TextDownloadName.Validate();
	}

	private static void DownloadState(ModLoader.LoaderCombo<int> loader)
	{
		try
		{
			switch (loader.State)
			{
			case ModBase.LoadState.Finished:
				HintService.Hint(Lang.Text("Tools.Test.CustomDownload.Finished", new object[1] { loader.name }), HintType.Success);
				Console.Beep();
				break;
			case ModBase.LoadState.Failed:
				ModBase.Log(loader.Error, loader.name + "失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Tools.Test.Error.OperationFailed"));
				Console.Beep();
				break;
			case ModBase.LoadState.Aborted:
				HintService.Hint(Lang.Text("Tools.Test.CustomDownload.Aborted", new object[1] { loader.name }));
				break;
			}
		}
		catch (Exception)
		{
		}
	}

	public static void StartCustomDownload(string url, string fileName, string folder = null, string userAgent = "")
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected Obj, but got Unknown
		try
		{
			if (string.IsNullOrWhiteSpace(folder))
			{
				folder = SystemDialogs.SelectSaveFile(Lang.Text("Tools.Test.CustomDownload.SelectLocation"), fileName, (string)null, (string)null);
				if (!folder.Contains("\\"))
				{
					return;
				}
				if (folder.EndsWith(fileName))
				{
					string text = folder;
					int length = fileName.Length;
					folder = text.Substring(0, text.Length - length);
				}
			}
			folder = folder.Replace("/", "\\").TrimEnd(new char[1] { '\\' }) + "\\";
			try
			{
				Directory.CreateDirectory(folder);
				ModBase.CheckPermissionWithException(folder);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "访问文件夹失败（" + folder + "）", ModBase.LogLevel.Hint, null, Lang.Text("Tools.Test.Error.OperationFailed"));
				return;
			}
			ModBase.Log("[Download] 自定义下载文件名：" + fileName);
			ModBase.Log("[Download] 自定义下载文件目标：" + folder);
			int uuid = ModBase.GetUuid();
			ModLoader.LoaderBase loaderBase = ((!((AbstractValidator<string>)new HttpValidator()).Validate(url).IsValid) ? ((ModLoader.LoaderBase)new LoaderDownloadUnc(Lang.Text("Tools.Test.CustomDownload.LoaderName", new object[1] { fileName }), new Tuple<string, string>(url, folder + fileName))) : ((ModLoader.LoaderBase)new LoaderDownload(Lang.Text("Tools.Test.CustomDownload.LoaderName", new object[1] { fileName }), new List<DownloadFile>
			{
				new DownloadFile(new string[1] { url }, folder + fileName, null, useBrowserUserAgent: true, userAgent)
			})));
			ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(Lang.Text("Tools.Test.CustomDownload.LoaderTitle", new object[1] { uuid }), new ModLoader.LoaderBase[1] { loaderBase });
			loaderCombo.OnStateChanged = (ModLoader.LoaderBase a) =>
			{
				DownloadState((ModLoader.LoaderCombo<int>)a);
			};
			loaderCombo.Start();
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "开始自定义下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Tools.Test.Error.OperationFailed"));
		}
	}

	public static void Jrrp()
	{
		int num = new Random(GenerateDailySeed()).Next(0, 101);
		string rating = GetRating(num);
		string title = Lang.Text("Tools.Test.Luck.MsgboxTitle", new object[1] { Lang.Date(DateTime.Now, "d") });
		if (num >= 60)
		{
			ModMain.MyMsgBox(Lang.Text("Tools.Test.Luck.MessageGood", new object[2] { num, rating }), title);
		}
		else
		{
			ModMain.MyMsgBox(Lang.Text("Tools.Test.Luck.MessageBad", new object[2] { num, rating }), title, null, "", "", num <= 30);
		}
	}

	public static void RubbishClear()
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmToolsTest != null && ModMain.frmToolsTest.BtnClear != null)
			{
				ModMain.frmToolsTest.BtnClear.IsEnabled = false;
			}
		});
		ModBase.RunInNewThread(() =>
		{
			try
			{
				if (!ModWatcher.hasRunningMinecraft && ModLaunch.mcLaunchLoader.State != ModBase.LoadState.Loading)
				{
					if (ModNet.HasDownloadingTask())
					{
						HintService.Hint(Lang.Text("Tools.Test.Clean.WaitForDownload"));
					}
					else
					{
						if (!ModFolder.mcFolderList.Any())
						{
							ModFolder.mcFolderListLoader.Start();
						}
						if (States.Hint.CleanJunkFile <= 2)
						{
							if (ModMain.MyMsgBox(Lang.Text("Tools.Test.Clean.ConfirmMessage"), Lang.Text("Tools.Test.Clean.ConfirmTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 2)
							{
								return;
							}
							States.HintStatesGroup hint = States.Hint;
							hint.CleanJunkFile += 1;
						}
						int num = 0;
						List<DirectoryInfo> list = new List<DirectoryInfo>();
						if (!ModFolder.mcFolderList.Any())
						{
							ModFolder.mcFolderListLoader.WaitForExit();
						}
						foreach (ModFolder.McFolder mcFolder in ModFolder.mcFolderList)
						{
							list.Add(new DirectoryInfo(mcFolder.Location));
							DirectoryInfo directoryInfo = new DirectoryInfo(mcFolder.Location + "versions");
							if (directoryInfo.Exists)
							{
								foreach (DirectoryInfo item in directoryInfo.EnumerateDirectories())
								{
									list.Add(item);
								}
							}
						}
						foreach (DirectoryInfo item2 in list)
						{
							num += ModBase.DeleteDirectory(item2.FullName + (item2.FullName.EndsWith("\\") ? "" : "\\") + "crash-reports\\", ignoreIssue: true);
							num += ModBase.DeleteDirectory(item2.FullName + (item2.FullName.EndsWith("\\") ? "" : "\\") + "logs\\", ignoreIssue: true);
							foreach (FileInfo item3 in item2.EnumerateFiles("*"))
							{
								if (item3.Name.StartsWith("hs_err_pid") || item3.Name.EndsWith(".log") || item3.Name == "WailaErrorOutput.txt")
								{
									item3.Delete();
									num++;
								}
							}
							foreach (DirectoryInfo item4 in item2.EnumerateDirectories())
							{
								if ((item4.Name ?? "") == item4.Name + "-natives" || item4.Name == "natives-windows-x86_64")
								{
									num += ModBase.DeleteDirectory(item4.FullName, ignoreIssue: true);
								}
							}
						}
						num += ModBase.DeleteDirectory(ModBase.pathTemp, ignoreIssue: true);
						num += ModBase.DeleteDirectory(Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL"), ignoreIssue: true);
						if (num != 0)
						{
							ModMain.MyMsgBox(Lang.Text("Tools.Test.Clean.ClearedMessage", new object[1] { num }), Lang.Text("Tools.Test.Clean.Cleared"), Lang.Text("Common.Action.Confirm"), "", "", isWarn: false, highLight: true, forceWait: true);
							Process.Start(new ProcessStartInfo(Basics.ExecutablePath));
							FormMain.EndProgramForce();
						}
						else
						{
							HintService.Hint(Lang.Text("Tools.Test.Clean.NoFiles"));
						}
					}
				}
				else
				{
					HintService.Hint(Lang.Text("Tools.Test.Clean.CloseGameFirst"));
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "清理垃圾失败", ModBase.LogLevel.Hint, null, Lang.Text("Tools.Test.Error.OperationFailed"));
			}
			finally
			{
				ModBase.RunInUiWait(() =>
				{
					if (ModMain.frmToolsTest != null && ModMain.frmToolsTest.BtnClear != null)
					{
						ModMain.frmToolsTest.BtnClear.IsEnabled = true;
					}
				});
			}
		}, "Rubbish Clear");
	}

	public static string GetRandomCave()
	{
		return Lang.Text("Tools.Test.CeNotice");
	}

	public static string GetRandomHint()
	{
		return Lang.Text("Tools.Test.CeNotice");
	}

	public static string GetRandomPresetHint()
	{
		return Lang.Text("Tools.Test.CeNotice");
	}

	private void TextDownloadUrl_TextChanged(object sender, TextChangedEventArgs e)
	{
		try
		{
			if (string.IsNullOrEmpty(TextDownloadName.Text) && !string.IsNullOrEmpty(TextDownloadUrl.Text))
			{
				TextDownloadName.Text = ModBase.GetFileNameFromPath(WebUtility.UrlDecode(TextDownloadUrl.Text));
				StartButtonRefresh();
			}
		}
		catch
		{
		}
	}

	private void MyTextButton_Click(object sender, EventArgs e)
	{
		string text = SystemDialogs.SelectFolder((string)null, (string)null);
		if (!string.IsNullOrEmpty(text))
		{
			TextDownloadFolder.Text = text;
		}
	}

	private void BtnDownloadOpen_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			string text = TextDownloadFolder.Text;
			Directory.CreateDirectory(text);
			Basics.OpenPath(text, (string)null);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "打开下载文件夹失败");
		}
	}

	private void BtnDownloadStart_Click(object sender, MouseButtonEventArgs e)
	{
		StartCustomDownload(TextDownloadUrl.Text, TextDownloadName.Text, TextDownloadFolder.Text);
		TextDownloadUrl.Text = "";
		TextDownloadUrl.Validate();
		TextDownloadUrl.ForceShowAsSuccess();
		TextDownloadName.Text = "";
		TextDownloadName.Validate();
		TextDownloadName.ForceShowAsSuccess();
		StartButtonRefresh();
	}

	private void TextDownloadUrl_ValidateChanged(object sender, RoutedEventArgs e)
	{
		StartButtonRefresh();
	}

	private void TextDownloadFolder_ValidateChanged(object sender, EventArgs e)
	{
		StartButtonRefresh();
	}

	private void TextDownloadName_ValidateChanged(object sender, EventArgs e)
	{
		StartButtonRefresh();
	}

	private void BtnClear_Click(object sender, MouseButtonEventArgs e)
	{
		RubbishClear();
	}

	private void BtnSkinSave_Click(object sender, MouseButtonEventArgs e)
	{
		string id = TextSkinID.Text;
		HintService.Hint(Lang.Text("Tools.Test.Skin.Fetching"));
		ModBase.RunInNewThread(() =>
		{
			try
			{
				if (id.Length < 3)
				{
					HintService.Hint(Lang.Text("Tools.Test.Skin.InvalidId"));
				}
				else
				{
					string result = (string)ProfileUi.McLoginMojangUuid(id, throwOnNotFound: true);
					result = ModSkin.McSkinGetAddress(result, "Mojang");
					result = ModSkin.McSkinDownload(result);
					ModBase.RunInUi(() =>
					{
						string toPath = SystemDialogs.SelectSaveFile(Lang.Text("Tools.Test.Skin.Save"), id + ".png", Lang.Text("Tools.Test.Skin.FileFilter"), (string)null);
						ModBase.CopyFile(result, toPath);
						HintService.Hint(Lang.Text("Tools.Test.Skin.Saved", new object[1] { id }), HintType.Success);
					});
				}
			}
			catch (Exception ex)
			{
				if (ex.ToString().Contains("429"))
				{
					HintService.Hint(Lang.Text("Tools.Test.Skin.TooFrequent"), HintType.Error);
					ModBase.Log("获取正版皮肤失败（" + id + "）：获取皮肤太过频繁，请 5 分钟后再试！");
				}
				else
				{
					ModBase.Log(ex, "获取正版皮肤失败（" + id + "）");
				}
			}
		});
	}

	private void BtnLuck_Click(object sender, MouseButtonEventArgs e)
	{
		Jrrp();
	}

	public static int GenerateDailySeed()
	{
		return DJB2Hash(DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + Identify.LauncherId);
	}

	private static int DJB2Hash(string str)
	{
		long num = 5381L;
		long num2 = 33L;
		foreach (long num3 in str)
		{
			num = (num * num2 + num3) % 4294967296L;
		}
		return (int)(num & 0x7FFFFFFF);
	}

	public static string GetRating(int luckValue)
	{
		string text;
		if (luckValue >= 60)
		{
			if (luckValue >= 95)
			{
				text = ((luckValue != 100) ? "Tools.Test.Luck.Rating95" : "Tools.Test.Luck.Rating100");
			}
			else
			{
				text = ((luckValue < 90) ? "Tools.Test.Luck.Rating60" : "Tools.Test.Luck.Rating90");
			}
		}
		else if (luckValue >= 30)
		{
			text = ((luckValue < 40) ? "Tools.Test.Luck.Rating30" : "Tools.Test.Luck.Rating40");
		}
		else
		{
			text = ((luckValue < 10) ? "Tools.Test.Luck.Rating0" : "Tools.Test.Luck.Rating10");
		}
		return Lang.Text(text);
	}

	private void BtnCreateShortcut_Click(object sender, MouseButtonEventArgs e)
	{
		string text = Lang.Text("Tools.Test.Shortcut.FileName", new object[1] { ".lnk" });
		string text2 = Lang.Text("Tools.Test.Shortcut.Desktop");
		string text3 = Lang.Text("Tools.Test.Shortcut.StartMenu");
		string specialPath = Paths.GetSpecialPath(Environment.SpecialFolder.Desktop, text);
		string specialPath2 = Paths.GetSpecialPath(Environment.SpecialFolder.StartMenu, "Programs\\" + text);
		int num = ModMain.MyMsgBox(Lang.Text("Tools.Test.Shortcut.ConfirmMessage", new object[4] { text2, specialPath, text3, specialPath2 }), Lang.Text("Tools.Test.Shortcut.SelectLocation"), Lang.Text("Common.Action.Cancel"), text2, text3);
		string text4;
		switch (num)
		{
		case 1:
			return;
		default:
			text4 = specialPath2;
			break;
		case 2:
			text4 = specialPath;
			break;
		}
		string text5 = text4;
		string text6 = ((num == 2) ? text2 : text3);
		Files.CreateShortcut(text5, Basics.ExecutablePath, (string)null, (string)null, (string)null, (string)null);
		HintService.Hint(Lang.Text("Tools.Test.Shortcut.Created", new object[1] { text6 }), HintType.Success);
	}

	private void BtnLaunchCount_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.MyMsgBox(Lang.Text("Tools.Test.LaunchCount.Message", new object[1] { States.System.LaunchCount }), Lang.Text("Tools.Test.LaunchCount.Title"));
	}

	private async void BtnAchievementPreview_Click(object sender, MouseButtonEventArgs e)
	{
		string achievementUrl = GetAchievementUrl();
		ModBase.Log("[Net] 获取网络结果" + achievementUrl);
		await LoadImageAsync(achievementUrl);
	}

	private async Task LoadImageAsync(string imageUrl)
	{
		HttpClient client = NetworkService.GetClient("default");
		try
		{
			HttpResponseMessage response = await client.GetAsync(imageUrl);
			if (response.IsSuccessStatusCode)
			{
				using (Stream streamSource = await response.Content.ReadAsStreamAsync())
				{
					BitmapImage bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					((Freezable)bitmapImage).Freeze();
					((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
					{
						AchievementImage.Source = bitmapImage;
						AchievementImage.Visibility = Visibility.Visible;
					}));
					return;
				}
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
				{
					ModBase.Log("获取成就图片失败（404）");
					HintService.Hint(Lang.Text("Tools.Test.Achievement.FetchFailed"), HintType.Error);
				}));
			}
			else
			{
				((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
				{
					ModBase.Log("获取成就图片失败（" + (int)response.StatusCode + "）");
				}));
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				ModBase.Log(ex3, "获取成就图片失败");
			}));
		}
	}

	private async void BtnAchievementSave_Click(object sender, MouseButtonEventArgs e)
	{
		string achievementUrl = GetAchievementUrl();
		await DownloadImageToLocalAsync(achievementUrl);
	}

	private async Task DownloadImageToLocalAsync(string imageUrl)
	{
		string savePath = ModBase.pathTemp + "Download\\" + ModBase.GetHash(imageUrl) + ".png";
		HttpClient client = NetworkService.GetClient("default");
		try
		{
			HttpResponseMessage httpResponseMessage = await client.GetAsync(imageUrl);
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				File.WriteAllBytes(savePath, await httpResponseMessage.Content.ReadAsByteArrayAsync());
				string text = SystemDialogs.SelectSaveFile(Lang.Text("Tools.Test.Achievement.Save"), AchievementTitleTextBox.Text + ".png", Lang.Text("Tools.Test.Achievement.FileFilter"), (string)null);
				if (string.IsNullOrEmpty(text))
				{
					ModBase.Log("用户取消了保存操作");
					File.Delete(savePath);
				}
				else
				{
					ModBase.CopyFile(savePath, text);
					File.Delete(savePath);
					HintService.Hint(Lang.Text("Tools.Test.Achievement.Saved"), HintType.Success);
				}
			}
			else if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
			{
				ModBase.Log("获取成就图片失败（404）");
				HintService.Hint(Lang.Text("Tools.Test.Achievement.FetchFailed"), HintType.Error);
			}
			else
			{
				ModBase.Log("获取成就图片失败（" + (int)httpResponseMessage.StatusCode + "）");
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取成就图片失败");
		}
	}

	private string GetAchievementUrl()
	{
		string value = AchievementBlockTextBox.Text.Trim();
		string value2 = AchievementTitleTextBox.Text.Replace(" ", "..");
		string value3 = AchievementString1TextBox.Text.Replace(" ", "..");
		string text = AchievementString2TextBox.Text.Replace(" ", "..");
		string text2 = $"https://minecraft-api.com/api/achivements/{value}/{value2}/{value3}";
		if (!string.IsNullOrEmpty(text))
		{
			text2 = text2 + "/" + text;
		}
		return text2;
	}

	private void BtnCrash_Click(object sender, MouseButtonEventArgs e)
	{
		throw new Exception(Lang.Text("Tools.Test.Crash.ManualCrash"));
	}

	private int GetHeadSize()
	{
		return CmbHeadSize.SelectedIndex switch
		{
			0 => 64, 
			1 => 96, 
			2 => 128, 
			_ => 64, 
		};
	}

	private void BtnSelectSkin_Click(object sender, RoutedEventArgs e)
	{
		string value = SystemDialogs.SelectFile(Lang.Text("Tools.Test.Avatar.FileFilter"), Lang.Text("Tools.Test.Avatar.SelectSkinFile"), (string)null);
		if (!string.IsNullOrEmpty(value))
		{
			LoadAndGenerateHead(value);
		}
	}

	private void LoadAndGenerateHead(string skinPath)
	{
		try
		{
			using (FileStream stream = new FileStream(skinPath, FileMode.Open, FileAccess.Read))
			{
				currentSkinBitmap = new Bitmap(stream);
			}
			this.skinPath = skinPath;
			if (currentSkinBitmap.Width != currentSkinBitmap.Height)
			{
				HintService.Hint(Lang.Text("Tools.Test.Avatar.InvalidSize"), HintType.Error);
				SkinPreviewBorder.Visibility = Visibility.Collapsed;
				return;
			}
			generatedHeadBitmap = GenerateHeadFromSkin(currentSkinBitmap);
			ImgFace.Source = BitmapToBitmapImage(generatedHeadBitmap);
			ImgHair.Source = null;
			SkinPreviewBorder.Visibility = Visibility.Visible;
			HintService.Hint(Lang.Text("Tools.Test.Avatar.Generated"), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "生成头像失败");
			HintService.Hint(Lang.Text("Tools.Test.Avatar.GenerateFailed", new object[1] { ex.Message }), HintType.Error);
			SkinPreviewBorder.Visibility = Visibility.Collapsed;
		}
	}

	private Bitmap GenerateHeadFromSkin(Bitmap skinBitmap)
	{
		int scale = skinBitmap.Width / 64;
		headSize = GetHeadSize();
		Bitmap bitmap = new Bitmap(headSize, headSize);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			DrawFaceLayer(graphics, skinBitmap, scale);
			if (skinBitmap.Width >= 64)
			{
				DrawHairLayer(bitmap, skinBitmap, scale);
			}
		}
		return bitmap;
	}

	private void DrawFaceLayer(Graphics g, Bitmap skinBitmap, int scale)
	{
		Rectangle srcRect = new Rectangle(8 * scale, 8 * scale, 8 * scale, 8 * scale);
		int num = headSize - headSize / 8;
		Bitmap image = new Bitmap(num, num);
		using (Graphics graphics = Graphics.FromImage(image))
		{
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			graphics.DrawImage(skinBitmap, new Rectangle(0, 0, num, num), srcRect, GraphicsUnit.Pixel);
		}
		int num2 = headSize / 16;
		g.DrawImage(image, num2, num2, num, num);
	}

	private void DrawHairLayer(Bitmap headBitmap, Bitmap skinBitmap, int scale)
	{
		Rectangle srcRect = new Rectangle(40 * scale, 8 * scale, 8 * scale, 8 * scale);
		Bitmap bitmap = new Bitmap(headSize, headSize);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			graphics.DrawImage(skinBitmap, new Rectangle(0, 0, headSize, headSize), srcRect, GraphicsUnit.Pixel);
		}
		int i = 0;
		for (int num = headSize - 1; i <= num; i++)
		{
			int j = 0;
			for (int num2 = headSize - 1; j <= num2; j++)
			{
				Color pixel = bitmap.GetPixel(i, j);
				if (pixel.A > 0)
				{
					headBitmap.SetPixel(i, j, pixel);
				}
			}
		}
	}

	private void BtnSaveHead_Click(object sender, MouseButtonEventArgs e)
	{
		if (generatedHeadBitmap == null)
		{
			HintService.Hint(Lang.Text("Tools.Test.Avatar.SelectFirst"), HintType.Error);
			return;
		}
		string text = SystemDialogs.SelectSaveFile(Lang.Text("Tools.Test.Avatar.Save"), "Head.png", (string)null, (string)null);
		if (!string.IsNullOrEmpty(text))
		{
			generatedHeadBitmap.Save(text, ImageFormat.Png);
			HintService.Hint(Lang.Text("Tools.Test.Avatar.Saved"), HintType.Success);
		}
	}

	private void CmbHeadSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (currentSkinBitmap != null && skinPath != null)
		{
			LoadAndGenerateHead(skinPath);
		}
	}

	private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
	{
		using MemoryStream memoryStream = new MemoryStream();
		bitmap.Save(memoryStream, ImageFormat.Png);
		memoryStream.Position = 0L;
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = memoryStream;
		bitmapImage.EndInit();
		((Freezable)bitmapImage).Freeze();
		return bitmapImage;
	}

	private void TextDownloadFolder_OnValidatedTextChanged(object sender, RoutedEventArgs e)
	{
		SaveCacheDownloadFolder(sender, e);
		TextDownloadName_ValidateChanged(sender, e);
	}

}

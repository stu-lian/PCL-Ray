using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.UI;
using PCL.Network;

namespace PCL;

public partial class MySkin : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	private bool isChanging;

	private bool isSkinMouseDown;

	public ModLoader.LoaderTask<ModBase.EqualableList<string>, string> loader;

	public string Address
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			ToolTip = (string.IsNullOrEmpty(field) ? Lang.Text("Common.State.Loading") : Lang.Text("Launch.Skin.Change.ToolTip"));
		}
	}

	public bool HasCape
	{
		get
		{
			return BtnSkinCape.Visibility == Visibility.Collapsed;
		}
		set
		{
			BtnSkinCape.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public event ClickEventHandler? Click;

	public MySkin()
	{
		InitializeComponent();
		MouseEnter += PanSkin_MouseEnter;
		MouseLeave += PanSkin_MouseLeave;
		MouseLeftButtonDown += PanSkin_MouseLeftButtonDown;
		MouseLeftButtonUp += PanSkin_MouseLeftButtonUp;
		BtnSkinSave.Click += BtnSkinSave_Click;
		BtnSkinSave.Checked += BtnSkinSave_Checked;
		BtnSkinRefresh.Click += RefreshClick;
		BtnSkinCape.Click += BtnSkinCape_Click;
	}

	private void PanSkin_MouseEnter(object sender, MouseEventArgs e)
	{
		ModAnimation.AniStart(ModAnimation.AaOpacity(ShadowSkin, 0.8 - ShadowSkin.Opacity, 200, 100), "Skin Shadow");
	}

	private void PanSkin_MouseLeave(object sender, MouseEventArgs e)
	{
		ModAnimation.AniStart(ModAnimation.AaOpacity(ShadowSkin, 0.2 - ShadowSkin.Opacity, 200), "Skin Shadow");
		isSkinMouseDown = false;
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(this, 1.0 - ((ScaleTransform)RenderTransform).ScaleX, 60, 0, new ModAnimation.AniEaseOutFluent()), "Skin Scale");
	}

	private void PanSkin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		isSkinMouseDown = true;
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(this, 0.9 - ((ScaleTransform)RenderTransform).ScaleX, 60, 0, new ModAnimation.AniEaseOutFluent()), "Skin Scale");
	}

	private void PanSkin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(this, 1.0 - ((ScaleTransform)RenderTransform).ScaleX, 60, 0, new ModAnimation.AniEaseOutFluent()), "Skin Scale");
		if (isSkinMouseDown)
		{
			isSkinMouseDown = false;
			Click?.Invoke(sender, e);
		}
	}

	public void BtnSkinSave_Click(object sender, RoutedEventArgs e)
	{
		Save(loader);
	}

	public static void Save(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> loader)
	{
		string output = loader.output;
		if (loader.State != ModBase.LoadState.Finished)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Fetching"), HintType.Error);
			if (loader.State != ModBase.LoadState.Loading)
			{
				loader.Start();
			}
			return;
		}
		try
		{
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Launch.Skin.SaveDialog.Title"), ModBase.GetFileNameFromPath(output), Lang.Text("Launch.Skin.SaveDialog.Filter"), (string)null);
			if (text.Contains("\\"))
			{
				File.Delete(text);
				if (output.StartsWith(ModBase.pathImage))
				{
					new MyBitmap(output).Save(text);
				}
				else
				{
					ModBase.CopyFile(output, text);
				}
				HintService.Hint(Lang.Text("Launch.Skin.SaveSuccess"), HintType.Success);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Skin.Save.Error"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Save.Error"));
		}
	}

	private void BtnSkinSave_Checked(object sender, RoutedEventArgs e)
	{
		((MyMenuItem)sender).IsEnabled = string.IsNullOrEmpty(Address);
	}

	public void Load()
	{
		try
		{
			Address = loader.output;
			if (string.IsNullOrEmpty(Address))
			{
				throw new Exception("皮肤加载器 " + loader.name + " 没有输出");
			}
			if (!Address.StartsWith(ModBase.pathImage) && !File.Exists(Address))
			{
				throw new FileNotFoundException("皮肤文件未找到", Address);
			}
			MyBitmap myBitmap;
			try
			{
				myBitmap = new MyBitmap(Address);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Launch.Skin.Load.Error.Corrupted", new object[1] { Address }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Load.Error.Corrupted", new object[1] { Address }));
				File.Delete(Address);
				return;
			}
			ImgBack.Tag = Address;
			int num = (int)Math.Round((double)myBitmap.pic.Width / 64.0);
			if (myBitmap.pic.Width < 32 || myBitmap.pic.Height < 32)
			{
				ImgFore.Source = null;
				ImgBack.Source = null;
				throw new Exception("图片大小不足，长为 " + myBitmap.pic.Height + "，宽为 " + myBitmap.pic.Width);
			}
			if (myBitmap.pic.Width >= 64 && myBitmap.pic.Height >= 32)
			{
				if (myBitmap.pic.GetPixel(1, 1).A == 0 || myBitmap.pic.GetPixel(myBitmap.pic.Width - 1, myBitmap.pic.Height - 1).A == 0 || myBitmap.pic.GetPixel(myBitmap.pic.Width - 2, (int)Math.Round((double)myBitmap.pic.Height / 2.0 - 2.0)).A == 0 || (myBitmap.pic.GetPixel(1, 1) != myBitmap.pic.GetPixel(num * 41, num * 9) && myBitmap.pic.GetPixel(myBitmap.pic.Width - 1, myBitmap.pic.Height - 1) != myBitmap.pic.GetPixel(num * 41, num * 9) && myBitmap.pic.GetPixel(myBitmap.pic.Width - 2, (int)Math.Round((double)myBitmap.pic.Height / 2.0 - 2.0)) != myBitmap.pic.GetPixel(num * 41, num * 9)))
				{
					ImgFore.Source = myBitmap.Clip(num * 40, num * 8, num * 8, num * 8);
					myBitmap.Clip(num * 40, num * 8, num * 8, num * 8);
				}
				else
				{
					ImgFore.Source = null;
				}
			}
			else
			{
				ImgFore.Source = null;
			}
			ImgBack.Source = myBitmap.Clip(num * 8, num * 8, num * 8, num * 8);
			string text = Address.Between((new string[1] { Address.Contains("Images/Skins/") ? "Skins/" : "Skin\\" })[0], ".png");
			string filename = ModBase.pathTemp + "Cache\\Skin\\Head\\" + text + ".png";
			McProfile current = ProfileService.Current;
			if (current != null)
			{
				McProfile val = current.Clone();
				val.SkinHeadId = text;
				ProfileService.Update(current, val);
				ProfileService.Select(val);
			}
			Bitmap bitmap = new Bitmap(56, 56);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				using (Bitmap image = myBitmap.Clip(num * 8, num * 8, num * 8, num * 8))
				{
					graphics.DrawImage(image, new Rectangle(4, 4, 48, 48));
				}
				if (ImgFore.Source != null)
				{
					using Bitmap image2 = myBitmap.Clip(num * 40, num * 8, num * 8, num * 8);
					graphics.DrawImage(image2, new Rectangle(0, 0, 56, 56));
				}
			}
			if (!Directory.Exists(ModBase.pathTemp + "Cache\\Skin\\Head"))
			{
				Directory.CreateDirectory(ModBase.pathTemp + "Cache\\Skin\\Head");
			}
			bitmap.Save(filename, ImageFormat.Png);
			ModBase.Log("[Skin] 载入头像成功：" + loader.name);
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Launch.Skin.Load.Error.Avatar", new object[1] { (Address ?? "null") + "," + loader.name }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Load.Error.Avatar", new object[1] { (Address ?? "null") + "," + loader.name }));
		}
	}

	private object ScaleToSize(Bitmap bitmap, int width, int height)
	{
		Bitmap bitmap2 = new Bitmap(width, height);
		using Graphics graphics = Graphics.FromImage(bitmap2);
		graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		graphics.DrawImage(bitmap, 0, 0, width, height);
		return bitmap2;
	}

	public void Clear()
	{
		Address = "";
		ImgFore.Source = null;
		ImgBack.Source = null;
	}

	public void RefreshClick(object sender, RoutedEventArgs e)
	{
		RefreshCache(loader);
	}

	public static void RefreshCache(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> sender = null)
	{
		bool flag = PageLaunchLeft.skinLoaders.Any((ModLoader.LoaderTask<ModBase.EqualableList<string>, string> skinLoader) => skinLoader.State == ModBase.LoadState.Loading);
		if ((ModMain.frmLaunchLeft != null) & flag)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Refresh.Busy"));
			return;
		}
		ModBase.RunInThread(() =>
		{
			try
			{
				HintService.Hint(Lang.Text("Launch.Skin.Refreshing"));
				ModBase.Log("[Skin] 正在清空皮肤缓存");
				if (Directory.Exists(ModBase.pathTemp + "Cache\\Skin"))
				{
					ModBase.DeleteDirectory(ModBase.pathTemp + "Cache\\Skin");
				}
				if (Directory.Exists(ModBase.pathTemp + "Cache\\Uuid"))
				{
					ModBase.DeleteDirectory(ModBase.pathTemp + "Cache\\Uuid");
				}
				ModBase.IniClearCache(ModBase.pathTemp + "Cache\\Skin\\IndexMs.ini");
				ModBase.IniClearCache(ModBase.pathTemp + "Cache\\Skin\\IndexAuth.ini");
				ModBase.IniClearCache(ModBase.pathTemp + "Cache\\Uuid\\Mojang.ini");
				ModLoader.LoaderTask<ModBase.EqualableList<string>, string>[] array = ((sender == null) ? new ModLoader.LoaderTask<ModBase.EqualableList<string>, string>[2]
				{
					PageLaunchLeft.skinLegacy,
					PageLaunchLeft.skinMs
				} : new ModLoader.LoaderTask<ModBase.EqualableList<string>, string>[1] { sender });
				for (int i = 0; i < array.Length; i++)
				{
					array[i].WaitForExit(null, null, isForceRestart: true);
				}
				HintService.Hint(Lang.Text("Launch.Skin.RefreshSuccess"), HintType.Success);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Launch.Skin.Refresh.Error"), ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Skin.Refresh.Error"));
			}
		});
	}

	public static void ReloadCache(string skinAddress, string uuid)
	{
		ModBase.RunInThread(() =>
		{
			try
			{
				ModBase.WriteIni(ModBase.pathTemp + "Cache\\Skin\\IndexMs.ini", uuid, skinAddress);
				ModBase.Log("[Skin] 已写入皮肤地址缓存 " + uuid + " -> " + skinAddress);
				PageLaunchLeft.skinMs.WaitForExit(null, null, isForceRestart: true);
				HintService.Hint(Lang.Text("Launch.Skin.ChangeSuccess"), HintType.Success);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Launch.Skin.Change.Error.MsRefresh"), ModBase.LogLevel.Feedback, null, Lang.Text("Launch.Skin.Change.Error.MsRefresh"));
			}
		});
	}

	public void BtnSkinCape_Click(object sender, RoutedEventArgs e)
	{
		if (isChanging)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Cape.Changing"));
			return;
		}
		if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Failed)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Cape.LoginFailed"), HintType.Error);
			return;
		}
		HintService.Hint(Lang.Text("Launch.Skin.Cape.FetchingList"));
		isChanging = true;
		ModBase.RunInNewThread(() =>
		{
			try
			{
				if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
				{
					ModLaunch.mcLoginMsLoader.WaitForExit(ProfileUi.GetLoginData());
				}
				if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
				{
					HintService.Hint(Lang.Text("Launch.Skin.Cape.LoginFailed"), HintType.Error);
				}
				else
				{
					string accessToken = ModLaunch.mcLoginMsLoader.output.AccessToken;
					_ = ModLaunch.mcLoginMsLoader.output;
					JsonObject skinData = (JsonObject)ModBase.GetJson(ModLaunch.mcLoginMsLoader.output.ProfileJson);
					foreach (JsonNode item in skinData["capes"].AsArray())
					{
						if (item["url"] != null)
						{
							string text = $"{ModBase.pathTemp}Cache\\Capes\\{item["alias"]}.png";
							string text2 = $"{ModBase.pathTemp}Cache\\Capes\\{item["alias"]}-front.png";
							if (File.Exists(text) && File.Exists(text2))
							{
								item["url"] = text2;
							}
							else
							{
								FileDownloader.DownloadByLoader(item["url"].ToString(), text);
								Rectangle rectangle = new Rectangle(1, 0, 11, 17);
								Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height);
								System.Drawing.Image image = System.Drawing.Image.FromFile(text);
								Graphics.FromImage(bitmap).DrawImage(image, rectangle, rectangle, GraphicsUnit.Pixel);
								bitmap.Save(text2);
								item["url"] = text2;
							}
						}
					}
					int? selId = null;
					ModBase.RunInUiWait(() =>
					{
						try
						{
							List<IMyRadio> list = new List<IMyRadio>
							{
								new MyListItem
								{
									Title = Lang.Text("Launch.Skin.Cape.None"),
									Info = "Null"
								}
							};
							list.AddRange(from Cape in skinData["capes"].AsArray()
								let CapeAlias = Cape["alias"].ToString()
								let CapeName = _GetCapeDisplayName(CapeAlias)
								let state = Cape["state"]
								let active = state != null && state.ToString().ToUpper().Equals("ACTIVE")
								select new MyListItem
								{
									Title = CapeName,
									Info = Cape["alias"].ToString(),
									Checked = active,
									Type = MyListItem.CheckType.RadioBox,
									Logo = (string?)Cape["url"],
									LogoScale = 0.8
								});
							selId = ModMain.MyMsgBoxSelect(list, Lang.Text("Launch.Skin.Cape.SelectTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"));
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, Lang.Text("Launch.Skin.Cape.Error.List"), ModBase.LogLevel.Feedback, null, Lang.Text("Launch.Skin.Cape.Error.List"));
						}
					});
					if (selId.HasValue)
					{
						string text3 = Requester.Fetch("https://api.minecraftservices.com/minecraft/profile/capes/active", new FetchParam
						{
							Method = ((selId.HasValue && selId.GetValueOrDefault() == 0) ? "DELETE" : "PUT"),
							Content = ((selId.HasValue && selId.GetValueOrDefault() == 0) ? "" : new JsonObject { ["capeId"] = skinData["capes"][(selId - 1).Value]["id"]?.ToString() }.ToJsonString()),
							ContentType = "application/json",
							Headers = new Dictionary<string, string> { 
							{
								"Authorization",
								"Bearer " + accessToken
							} }
						});
						if (text3.Contains("\"errorMessage\""))
						{
							HintService.Hint(Lang.Text("Launch.Skin.Cape.ChangeFailedWithReason", new object[1] { ((JsonObject)ModBase.GetJson(text3))["errorMessage"] }), HintType.Error);
						}
						else
						{
							HintService.Hint(Lang.Text("Launch.Skin.Cape.ChangeSuccess"), HintType.Success);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Launch.Skin.Cape.ChangeFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Cape.ChangeFailed"));
			}
			finally
			{
				isChanging = false;
			}
		}, "Cape Change");
	}

	private static string _GetCapeDisplayName(string capeAlias)
	{
		string text = capeAlias.Replace("-", "").Replace(" ", "").Replace("'", "");
		string text2 = "Launch.Skin.Cape.Name." + text;
		string text3 = Lang.Text(text2);
		if (text3 == "!" + text2 + "!" || text3 == text2)
		{
			return capeAlias;
		}
		return text3;
	}

}

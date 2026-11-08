using System;

namespace PCL;

public static class ModVideoBack
{
	public class BooleanEventArgs : EventArgs
	{
		public bool Value { get; set; }

		public BooleanEventArgs(bool value)
		{
			Value = value;
		}
	}

	public static bool isMinimized;

	public static bool IsGaming
	{
		get
		{
			return field;
		}
		set
		{
			if (field != value)
			{
				field = value;
				GamingStateChanged?.Invoke(null, new BooleanEventArgs(value));
			}
		}
	}

	public static bool ForcePlay
	{
		get
		{
			return field;
		}
		set
		{
			if (field != value)
			{
				field = value;
				ForcePlayChanged?.Invoke(null, new BooleanEventArgs(value));
			}
		}
	}

	public static event EventHandler<BooleanEventArgs>? GamingStateChanged;

	public static event EventHandler<BooleanEventArgs>? ForcePlayChanged;

	public static void OnGamingStateChanged(object sender, BooleanEventArgs e)
	{
		ModBase.RunInUi(() =>
		{
			if (IsGaming)
			{
				if (ForcePlay)
				{
					if (ModMain.frmSetupUI != null)
					{
						ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = true;
					}
				}
				else if (ModMain.frmSetupUI != null)
				{
					ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = false;
				}
			}
			else if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = true;
			}
		});
	}

	public static void OnForcePlayChanged(object sender, BooleanEventArgs e)
	{
		ModBase.RunInUi(() =>
		{
			if (IsGaming)
			{
				if (ForcePlay)
				{
					if (ModMain.frmSetupUI != null)
					{
						ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = true;
					}
				}
				else if (ModMain.frmSetupUI != null)
				{
					ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = false;
				}
			}
			else if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = true;
			}
		});
	}

	public static void VideoPlay()
	{
		ModBase.RunInUi(() =>
		{
			if ((object)ModMain.frmMain.VideoBack.Source != null && !isMinimized && (!IsGaming || ForcePlay))
			{
				try
				{
					ModMain.frmMain.VideoBack.Play();
					ModBase.Log("[UI] 已开始视频背景播放");
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "[UI] 开始视频背景播放失败");
				}
			}
		});
	}

	public static void VideoStop()
	{
		ModBase.RunInUi(() =>
		{
			try
			{
				ModMain.frmMain.VideoBack.Source = null;
				ModMain.frmMain.VideoBack.Stop();
				ModMain.frmMain.VideoBack.Position = TimeSpan.Zero;
				ModBase.Log("[UI] 已停止视频背景播放");
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[UI] 停止视频背景播放失败");
			}
		});
	}

	public static void VideoPause()
	{
		ModBase.RunInUi(() =>
		{
			if (isMinimized)
			{
				if ((object)ModMain.frmMain.VideoBack.Source != null)
				{
					try
					{
						ModMain.frmMain.VideoBack.Pause();
						ModBase.Log("[UI] 已暂停视频背景播放");
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[UI] 暂停视频背景播放失败");
					}
				}
			}
			else if (!ForcePlay && (object)ModMain.frmMain.VideoBack.Source != null)
			{
				try
				{
					if (ModMain.frmSetupUI != null)
					{
						ModMain.frmSetupUI.BtnBackgroundRefresh.IsEnabled = false;
					}
					ModMain.frmMain.VideoBack.Pause();
					ModBase.Log("[UI] 已暂停视频背景播放");
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "[UI] 暂停视频背景播放失败");
				}
			}
		});
	}
}

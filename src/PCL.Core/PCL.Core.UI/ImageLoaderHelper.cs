using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PCL.Core.UI;

public static class ImageLoaderHelper
{
	public static async Task SetServerLogoAsync(string base64String, Image imageElement, string? defaultImageUri = null)
	{
		await SetImageFromBase64Async(base64String, imageElement, defaultImageUri ?? "pack://application:,,,/Plain Craft Launcher 2;component/Images/Icons/DefaultServer.png");
	}

	public static async Task SetImageFromBase64Async(string base64String, Image imageElement, string? fallbackImageUri = null)
	{
		ArgumentNullException.ThrowIfNull("imageElement", "nameof(imageElement)");
		if (string.IsNullOrWhiteSpace(base64String))
		{
			SetFallbackImage(imageElement, fallbackImageUri);
			return;
		}
		try
		{
			string base64Data = (base64String.Contains(',') ? base64String.Split(',')[1] : base64String);
			if (string.IsNullOrWhiteSpace(base64Data))
			{
				SetFallbackImage(imageElement, fallbackImageUri);
				return;
			}
			BitmapImage bitmapImage = await Task.Run(() => _CreateBitmapFromBase64(base64Data));
			if (((DispatcherObject)imageElement).Dispatcher.CheckAccess())
			{
				imageElement.Source = bitmapImage;
				return;
			}
			await ((DispatcherObject)imageElement).Dispatcher.InvokeAsync<ImageSource>((Func<ImageSource>)(() => imageElement.Source = bitmapImage));
		}
		catch
		{
			SetFallbackImage(imageElement, fallbackImageUri);
		}
	}

	private static BitmapImage _CreateBitmapFromBase64(string base64Data)
	{
		using MemoryStream streamSource = new MemoryStream(Convert.FromBase64String(base64Data));
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = streamSource;
		bitmapImage.EndInit();
		((Freezable)bitmapImage).Freeze();
		return bitmapImage;
	}

	public static void SetFallbackImage(Image imageElement, string? fallbackImageUri)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(fallbackImageUri))
			{
				BitmapImage defaultBitmap = new BitmapImage(new Uri(fallbackImageUri));
				if (((DispatcherObject)imageElement).Dispatcher.CheckAccess())
				{
					imageElement.Source = defaultBitmap;
					return;
				}
				((DispatcherObject)imageElement).Dispatcher.Invoke<ImageSource>((Func<ImageSource>)(() => imageElement.Source = defaultBitmap));
			}
			else if (((DispatcherObject)imageElement).Dispatcher.CheckAccess())
			{
				imageElement.Source = null;
			}
			else
			{
				((DispatcherObject)imageElement).Dispatcher.Invoke<ImageSource>((Func<ImageSource>)(() => imageElement.Source = null));
			}
		}
		catch
		{
			if (((DispatcherObject)imageElement).Dispatcher.CheckAccess())
			{
				imageElement.Source = null;
				return;
			}
			((DispatcherObject)imageElement).Dispatcher.Invoke<ImageSource>((Func<ImageSource>)(() => imageElement.Source = null));
		}
	}

	public static async Task SetImageFromFileAsync(string imagePath, Image imageElement, string fallbackImageUri)
	{
		ArgumentNullException.ThrowIfNull("imageElement", "nameof(imageElement)");
		if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
		{
			SetFallbackImage(imageElement, fallbackImageUri);
			return;
		}
		try
		{
			BitmapImage bitmapImage = await Task.Run(() =>
			{
				BitmapImage bitmapImage2 = new BitmapImage();
				bitmapImage2.BeginInit();
				bitmapImage2.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage2.UriSource = new Uri(imagePath);
				bitmapImage2.EndInit();
				((Freezable)bitmapImage2).Freeze();
				return bitmapImage2;
			});
			if (((DispatcherObject)imageElement).Dispatcher.CheckAccess())
			{
				imageElement.Source = bitmapImage;
				return;
			}
			await ((DispatcherObject)imageElement).Dispatcher.InvokeAsync<ImageSource>((Func<ImageSource>)(() => imageElement.Source = bitmapImage));
		}
		catch
		{
			SetFallbackImage(imageElement, fallbackImageUri);
		}
	}
}

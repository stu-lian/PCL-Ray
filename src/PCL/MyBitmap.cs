using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL.Core.UI.Media;

namespace PCL;

public class MyBitmap
{
	private readonly ConcurrentDictionary<string, MyBitmap> _Cache = new ConcurrentDictionary<string, MyBitmap>();

	public Bitmap pic;

	public MyBitmap()
	{
	}

	public MyBitmap(string filePathOrResourceName)
	{
		try
		{
			filePathOrResourceName = filePathOrResourceName.Replace("pack://application:,,,/images/", ModBase.pathImage);
			if (filePathOrResourceName.StartsWithF(ModBase.pathImage))
			{
				if (_Cache.ContainsKey(filePathOrResourceName))
				{
					pic = _Cache[filePathOrResourceName].pic;
					return;
				}
				pic = new MyBitmap((ImageSource)new ImageSourceConverter().ConvertFromString(filePathOrResourceName));
				_Cache.TryAdd(filePathOrResourceName, pic);
				return;
			}
			using FileStream fileStream = new FileStream(filePathOrResourceName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			if (fileStream.Length > 2 && fileStream.ReadByte() == 82 && fileStream.ReadByte() == 73)
			{
				fileStream.Seek(0L, SeekOrigin.Begin);
				using MemoryStream stream = PCL.Core.UI.Media.ImageConverter.FromWebpToPng((Stream)fileStream, 0);
				pic = new Bitmap(stream);
				return;
			}
			pic = new Bitmap(fileStream);
		}
		catch (Exception ex)
		{
			pic = (Bitmap)System.Windows.Application.Current.TryFindResource(filePathOrResourceName);
			if (pic == null)
			{
				pic = new Bitmap(1, 1);
				if (ex is ArgumentException)
				{
					throw new Exception("图片格式不支持，或图片文件损坏（" + filePathOrResourceName + "）", ex);
				}
				throw new Exception("加载 MyBitmap 意外失败（" + filePathOrResourceName + "）", ex);
			}
			ModBase.Log(ex, "指定类型有误的 MyBitmap 加载（" + filePathOrResourceName + "）", ModBase.LogLevel.Developer);
		}
	}

	public MyBitmap(ImageSource image)
	{
		using MemoryStream stream = new MemoryStream();
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create((BitmapSource)image));
		pngBitmapEncoder.Save(stream);
		pic = new Bitmap(stream);
	}

	public MyBitmap(Image image)
	{
		pic = (Bitmap)image;
	}

	public MyBitmap(Bitmap image)
	{
		pic = image;
	}

	public MyBitmap(ImageBrush image)
	{
		using MemoryStream stream = new MemoryStream();
		BmpBitmapEncoder bmpBitmapEncoder = new BmpBitmapEncoder();
		bmpBitmapEncoder.Frames.Add(BitmapFrame.Create((BitmapSource)image.ImageSource));
		bmpBitmapEncoder.Save(stream);
		pic = new Bitmap(stream);
	}

	public static implicit operator MyBitmap(Image image)
	{
		if (image == null)
		{
			return null;
		}
		return new MyBitmap(image);
	}

	public static implicit operator Image(MyBitmap image)
	{
		return image?.pic;
	}

	public static implicit operator MyBitmap(ImageSource image)
	{
		if (image == null)
		{
			return null;
		}
		return new MyBitmap(image);
	}

	public static implicit operator ImageSource(MyBitmap image)
	{
		if (image == null)
		{
			return null;
		}
		Bitmap bitmap = image.pic;
		Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
		BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		try
		{
			BitmapSource bitmapSource = BitmapSource.Create(bitmap.Width, bitmap.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution, PixelFormats.Bgra32, null, bitmapData.Scan0, rect.Width * rect.Height * 4, bitmapData.Stride);
			((Freezable)bitmapSource).Freeze();
			return bitmapSource;
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
	}

	public static implicit operator MyBitmap(Bitmap image)
	{
		if (image == null)
		{
			return null;
		}
		return new MyBitmap(image);
	}

	public static implicit operator Bitmap(MyBitmap image)
	{
		return image?.pic;
	}

	public static implicit operator MyBitmap(ImageBrush image)
	{
		if (image == null)
		{
			return null;
		}
		return new MyBitmap(image);
	}

	public static implicit operator ImageBrush(MyBitmap image)
	{
		if (image == null)
		{
			return null;
		}
		return new ImageBrush(new MyBitmap(image.pic));
	}

	public MyBitmap Clip(int x, int y, int width, int height)
	{
		Bitmap bitmap = new Bitmap(width, height, pic.PixelFormat);
		bitmap.SetResolution(pic.HorizontalResolution, pic.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.TranslateTransform(-x, -y);
			graphics.DrawImage(pic, new Rectangle(0, 0, pic.Width, pic.Height));
		}
		return bitmap;
	}

	public MyBitmap RotateFlip(RotateFlipType type)
	{
		Bitmap bitmap = new Bitmap(pic);
		bitmap.SetResolution(pic.HorizontalResolution, pic.VerticalResolution);
		bitmap.RotateFlip(type);
		return bitmap;
	}

	public void Save(string filePath)
	{
		BitmapEncoder bitmapEncoder = new PngBitmapEncoder();
		bitmapEncoder.Frames.Add(BitmapFrame.Create((BitmapSource)(ImageSource)this));
		using FileStream stream = new FileStream(filePath, FileMode.Create);
		bitmapEncoder.Save(stream);
	}
}

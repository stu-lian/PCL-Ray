using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace PCL.Core.UI.Media;

public static class ImageConverter
{
	public static MemoryStream FromWebpToPng(this Stream input, int frameIndex = 0)
	{
		Stream stream = input;
		MemoryStream memoryStream = null;
		if (!input.CanSeek)
		{
			memoryStream = new MemoryStream();
			input.CopyTo(memoryStream);
			memoryStream.Position = 0L;
			stream = memoryStream;
		}
		else if (stream.Position != 0L)
		{
			stream.Position = 0L;
		}
		try
		{
			BitmapDecoder bitmapDecoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			if (bitmapDecoder.Frames.Count == 0)
			{
				throw new NotSupportedException("未能从输入数据解码出任何图像帧，可能不是有效的 WebP 图像。");
			}
			if (frameIndex < 0 || frameIndex >= bitmapDecoder.Frames.Count)
			{
				throw new ArgumentOutOfRangeException("frameIndex", $"帧索引超出范围（0..{bitmapDecoder.Frames.Count - 1}）。");
			}
			BitmapFrame source = bitmapDecoder.Frames[frameIndex];
			PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
			pngBitmapEncoder.Frames.Add(BitmapFrame.Create(source));
			MemoryStream memoryStream2 = new MemoryStream();
			pngBitmapEncoder.Save(memoryStream2);
			memoryStream2.Position = 0L;
			return memoryStream2;
		}
		catch (NotSupportedException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new NotSupportedException("转换 WebP 到 PNG 失败：输入可能不是有效的 WebP，或系统未提供所需的编解码支持。", innerException);
		}
		finally
		{
			memoryStream?.Dispose();
		}
	}
}

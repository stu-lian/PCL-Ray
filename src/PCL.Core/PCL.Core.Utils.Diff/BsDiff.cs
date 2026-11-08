using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;

namespace PCL.Core.Utils.Diff;

public class BsDiff : IBinaryDiff
{
	private const int HeaderSize = 32;

	private const int HeaderVersionIndex = 0;

	private const long HeaderVersion = 3473478480300364610L;

	private const int HeaderCtrlIndex = 8;

	private const int HeaderDiffIndex = 16;

	private const int HeaderNewSizeIndex = 24;

	public async Task<byte[]> ApplyAsync(byte[] originData, byte[] diffData)
	{
		return await Task.Run(() =>
		{
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Expected Obj, but got Unknown
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Expected Obj, but got Unknown
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01db: Expected Obj, but got Unknown
			if (diffData.Length < 32)
			{
				throw new Exception("Diff file size is less than the header size");
			}
			if (BitConverter.ToInt64(diffData, 0) != 3473478480300364610L)
			{
				throw new Exception("Diff file version is wrong");
			}
			long num = BitConverter.ToInt64(diffData, 8);
			long num2 = BitConverter.ToInt64(diffData, 16);
			long num3 = BitConverter.ToInt64(diffData, 24);
			long num4 = diffData.Length - 32 - num - num2;
			if (num < 0 || num2 < 0 || num4 < 0)
			{
				throw new Exception("Block size is negative");
			}
			if (num3 < 0)
			{
				throw new Exception("Final file size info is negative");
			}
			if (32 + num + num2 + num4 > diffData.Length)
			{
				throw new Exception("Diff file size info is not correct");
			}
			Console.WriteLine($"Got diff-data-len = {diffData.Length}, ctrllen = {num}, difflen = {num2}, extralen = {num4}, totallen = {num3}");
			byte[] array = new byte[num];
			long num5 = 32L;
			Array.Copy(diffData, num5, array, 0L, num);
			BZip2InputStream val = new BZip2InputStream((Stream)new MemoryStream(array));
			try
			{
				using BinaryReader binaryReader = new BinaryReader((Stream)(object)val);
				num5 += num;
				byte[] array2 = new byte[num2];
				Array.Copy(diffData, num5, array2, 0L, num2);
				BZip2InputStream val2 = new BZip2InputStream((Stream)new MemoryStream(array2));
				try
				{
					using BinaryReader binaryReader2 = new BinaryReader((Stream)(object)val2);
					num5 += num2;
					byte[] array3 = new byte[num4];
					Array.Copy(diffData, num5, array3, 0L, num4);
					BZip2InputStream val3 = new BZip2InputStream((Stream)new MemoryStream(array3));
					try
					{
						using BinaryReader binaryReader3 = new BinaryReader((Stream)(object)val3);
						byte[] array4 = new byte[num3];
						long num6 = 0L;
						long num7 = 0L;
						while (num6 < num3)
						{
							long num8 = ReadInt64(binaryReader.ReadBytes(8));
							long num9 = ReadInt64(binaryReader.ReadBytes(8));
							long num10 = ReadInt64(binaryReader.ReadBytes(8));
							Console.WriteLine($"Round add-range = {num8}, copy-range = {num9}, seek-pos = {num10}");
							if (num6 + num8 > num3)
							{
								throw new Exception($"Add range overflows, want add {num8.ToString()}, but only have {num3 - num6} left");
							}
							for (long num11 = 0L; num11 < num8; num11++)
							{
								byte b = binaryReader2.ReadByte();
								if (num7 + num11 < originData.Length)
								{
									array4[num6 + num11] = (byte)(b + originData[num7 + num11]);
								}
								else
								{
									array4[num6 + num11] = b;
								}
							}
							num6 += num8;
							num7 += num8;
							if (num6 + num9 > num3)
							{
								throw new Exception($"Copy range overflows, want  copy {num9.ToString()}, but only have {num3 - num6} left");
							}
							for (int i = 0; i < num9; i++)
							{
								array4[num6 + i] = binaryReader3.ReadByte();
							}
							num6 += num9;
							num7 += num10;
							if (num7 > originData.Length)
							{
								throw new Exception($"Old data pos overflows, current old data length = {originData.Length}, but want {num7}");
							}
						}
						return array4;
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		});
	}

	public Task<byte[]> MakeAsync(byte[] originData, byte[] newData)
	{
		throw new NotImplementedException();
	}

	internal static long ReadInt64(byte[] buffer, int offset = 0)
	{
		long num = (long)(buffer[offset] | ((ulong)buffer[offset + 1] << 8) | ((ulong)buffer[offset + 2] << 16) | ((ulong)buffer[offset + 3] << 24) | ((ulong)buffer[offset + 4] << 32) | ((ulong)buffer[offset + 5] << 40) | ((ulong)buffer[offset + 6] << 48) | ((ulong)buffer[offset + 7] << 56));
		long num2 = num >> 63;
		return (~num2 & num) | (((num & long.MinValue) - num) & num2);
	}
}

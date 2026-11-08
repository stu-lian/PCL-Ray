using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Records;

namespace PCL.Core.IO.Net.Dns;

public class DnsSrvResource : IDnsResource, IDnsByteArrayWriter
{
	public string Target { get; set; } = "";

	public int Weight { get; set; }

	public int Priority { get; set; }

	public int Port { get; set; }

	public void WriteBytes(Memory<byte> bytes, ref int offset)
	{
		Span<byte> span = bytes.Span.Slice(offset);
		int num = offset;
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(num, offset + 2 - num), (ushort)Priority);
		offset += 2;
		num = offset;
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(num, offset + 2 - num), (ushort)Weight);
		offset += 2;
		num = offset;
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(num, offset + 2 - num), (ushort)Port);
		offset += 2;
		string[] array = Target.Split('.');
		foreach (string s in array)
		{
			byte[] bytes2 = Encoding.UTF8.GetBytes(s);
			byte b = (byte)bytes2.Length;
			span[offset] = b;
			offset++;
			bytes2.CopyTo(span.Slice(offset));
			offset += bytes2.Length;
		}
	}

	public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
	{
		ReadOnlySpan<byte> span = bytes.Slice(offset, length).Span;
		offset += length;
		ReadOnlySpan<byte> source = span.Slice(0, 2);
		Priority = BinaryPrimitives.ReadUInt16BigEndian(source);
		ReadOnlySpan<byte> source2 = span.Slice(2, 2);
		Weight = BinaryPrimitives.ReadUInt16BigEndian(source2);
		ReadOnlySpan<byte> source3 = span.Slice(4, 2);
		Port = BinaryPrimitives.ReadUInt16BigEndian(source3);
		List<string> list = new List<string>();
		int num = 6;
		while (num < length)
		{
			byte b = span[num];
			if (b == 0)
			{
				break;
			}
			num++;
			if (num + b > length)
			{
				throw new ArgumentException("Invalid DNS SRV resource record: segment length exceeds buffer size");
			}
			list.Add(Encoding.UTF8.GetString(span.Slice(num, b)));
			num += b;
		}
		Target = string.Join('.', list);
	}
}

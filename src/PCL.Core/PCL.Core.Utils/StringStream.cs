using System;
using System.IO;
using System.Text;

namespace PCL.Core.Utils;

public class StringStream : Stream
{
	private readonly MemoryStream _innerStream;

	public override bool CanRead => _innerStream.CanRead;

	public override bool CanSeek => _innerStream.CanSeek;

	public override bool CanWrite => false;

	public override long Length => _innerStream.Length;

	public override long Position
	{
		get
		{
			return _innerStream.Position;
		}
		set
		{
			_innerStream.Position = value;
		}
	}

	public StringStream(string source, Encoding? encoding = null)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		byte[] bytes = encoding.GetBytes(source);
		_innerStream = new MemoryStream(bytes);
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _innerStream.Read(buffer, offset, count);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _innerStream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException("StringStream 是只读流，不支持 SetLength。");
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException("StringStream 是只读流，不支持 Write。");
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_innerStream.Dispose();
		}
		base.Dispose(disposing);
	}
}

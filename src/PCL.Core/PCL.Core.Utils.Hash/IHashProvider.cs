using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Hash;

public interface IHashProvider
{
	int Length { get; }

	ValueTask<byte[]> ComputeHashAsync(Stream input, CancellationToken cancellationToken = default(CancellationToken));

	byte[] ComputeHash(Stream input);

	byte[] ComputeHash(ReadOnlySpan<byte> input);

	byte[] ComputeHash(string input, Encoding? en = null);
}

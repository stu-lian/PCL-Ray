using System.Threading.Tasks;

namespace PCL.Core.Utils.Diff;

public interface IBinaryDiff
{
	Task<byte[]> MakeAsync(byte[] originData, byte[] newData);

	Task<byte[]> ApplyAsync(byte[] originData, byte[] diffData);
}

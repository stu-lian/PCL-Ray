namespace PCL.Core.IO.Download;

public interface IDlResourceMapping<out TMappingValue>
{
	TMappingValue? Parse(string resId);
}

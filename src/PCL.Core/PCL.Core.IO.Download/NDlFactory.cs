namespace PCL.Core.IO.Download;

public abstract class NDlFactory
{
	public abstract IDlConnection? CreateConnection(string resId);

	public abstract IDlWriter? CreateWriter(string resId);
}
public abstract class NDlFactory<TSourceArgument, TTargetArgument> : NDlFactory
{
	protected abstract IDlResourceMapping<TSourceArgument> SourceMapping { get; }

	protected abstract IDlResourceMapping<TTargetArgument> TargetMapping { get; }

	protected abstract IDlConnection CreateConnection(TSourceArgument source);

	public override IDlConnection? CreateConnection(string resId)
	{
		TSourceArgument val = SourceMapping.Parse(resId);
		if (val != null)
		{
			return CreateConnection(val);
		}
		return null;
	}

	protected abstract IDlWriter CreateWriter(TTargetArgument target);

	public override IDlWriter? CreateWriter(string resId)
	{
		TTargetArgument val = TargetMapping.Parse(resId);
		if (val != null)
		{
			return CreateWriter(val);
		}
		return null;
	}
}

namespace PCL.Core.UI.Theme;

public record ToneProfileConfig
{
	public static readonly ToneProfile DefaultLight = new ToneProfile();

	public static readonly ToneProfile DefaultDark = new ToneProfile(0.96, 0.75, 0.6, 0.65, 0.45, 0.25, 0.225, 0.2, 0.275, 1.0, 0.3);

	public ToneProfile Light
	{
		get
		{
			return field ?? DefaultLight;
		}
		init;
	}

	public ToneProfile Dark
	{
		get
		{
			return field ?? DefaultDark;
		}
		init;
	}
}

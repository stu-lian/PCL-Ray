using System.Windows;
using System.Windows.Media;
using PCL.Core.App.IoC;

namespace PCL.Core.UI.Theme;

public class CatColorResource
{
	private const string ColorPrefix = "ColorObject";

	private const string BrushPrefix = "ColorBrush";

	public float Alpha { get; init; } = 255f;

	public required float Red { get; init; }

	public required float Green { get; init; }

	public required float Blue { get; init; }

	public required string Suffix { get; init; }

	public bool ApplyToBrush { get; init; }

	public bool ApplyToColor { get; init; }

	public void Apply()
	{
		ResourceDictionary resources = Lifecycle.CurrentApplication.Resources;
		Color color = Color.FromScRgb(Alpha, Red, Green, Blue);
		if (ApplyToColor)
		{
			resources["ColorObject" + Suffix] = color;
		}
		if (ApplyToBrush)
		{
			resources["ColorBrush" + Suffix] = new SolidColorBrush(color);
		}
	}
}

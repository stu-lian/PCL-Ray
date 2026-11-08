using System.ComponentModel;
using PCL.Core.UI.Converters;

namespace PCL.Core.UI.Animation.Easings;

[TypeConverter(typeof(EasingConverter))]
public interface IEasing
{
	double Ease(double progress);

	double Ease(int currentFrame, int totalFrames);
}

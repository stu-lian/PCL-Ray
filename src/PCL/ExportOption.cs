using System.Windows;

namespace PCL;

public class ExportOption : DependencyObject
{
	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ExportOption));

	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ExportOption));

	public string Title
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TitleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TitleProperty, (object)value);
		}
	}

	public string Description
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(DescriptionProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(DescriptionProperty, (object)value);
		}
	}

	public string Rules { get; set; }

	public string ShowRules { get; set; }

	public bool DefaultChecked { get; set; }

	public bool RequireModLoader { get; set; }

	public bool RequireOptiFine { get; set; }

	public bool RequireModLoaderOrOptiFine { get; set; }
}

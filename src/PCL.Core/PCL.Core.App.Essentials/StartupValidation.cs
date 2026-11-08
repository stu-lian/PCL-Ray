using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PCL.Core.App.Essentials;

public static class StartupValidation
{
	public static void EnsureWpfFont()
	{
		try
		{
			new FormattedText("", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First(), 96.0, Brushes.Black, 96.0);
		}
		catch (UriFormatException)
		{
			Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"), EnvironmentVariableTarget.User);
			new FormattedText("", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First(), 96.0, Brushes.Black, 96.0);
		}
	}
}

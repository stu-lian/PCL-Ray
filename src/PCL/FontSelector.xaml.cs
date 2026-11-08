using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL;

public partial class FontSelector : ContentControl, IComponentConnector
{
	public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

	public class CustomFontProperties : INotifyPropertyChanged
	{
		public string Name
		{
			get;
			init
			{
				SetField(ref field, value, "Name");
			}
		} = string.Empty;

		public FontFamily Font
		{
			get;
			set
			{
				SetField(ref field, value, "Font");
			}
		} = LocalizationFontService.BuildLaunchFontFamily((string)null, (LocalizationLanguage)null);

		public string Tag
		{
			get;
			set
			{
				SetField(ref field, value, "Tag");
			}
		} = string.Empty;

		public event PropertyChangedEventHandler? PropertyChanged;

		private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public static readonly DependencyProperty TooltipProperty = DependencyProperty.Register("Tooltip", typeof(string), typeof(FontSelector), new PropertyMetadata((object)null, (PropertyChangedCallback)OnTooltipChanged));

	private bool _isInitializing;

	private bool _isListeningLanguageChanged;

	private string? _pendingFontTag;

	public string Tooltip
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TooltipProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TooltipProperty, (object)value);
		}
	}

	public ObservableCollection<CustomFontProperties> CustomFontCollection { get; } = new ObservableCollection<CustomFontProperties>();

	public string SelectedFontTag
	{
		get
		{
			if (ComboFont.SelectedItem == null)
			{
				return "";
			}
			if (ComboFont.SelectedItem is CustomFontProperties customFontProperties)
			{
				return customFontProperties.Tag;
			}
			return "";
		}
		set
		{
			if (CustomFontCollection.Count != 0)
			{
				ObservableCollection<CustomFontProperties> customFontCollection = CustomFontCollection;
				if (customFontCollection != null && customFontCollection.Count == 1)
				{
					CustomFontProperties customFontProperties = customFontCollection[0];
					if (customFontProperties != null)
					{
						string name = customFontProperties.Name;
						if (name == Lang.Text("Common.State.Loading"))
						{
							goto IL_0054;
						}
					}
				}
				_isInitializing = true;
				CustomFontProperties customFontProperties2 = CustomFontCollection.FirstOrDefault((CustomFontProperties i) => i.Tag == value);
				if (customFontProperties2 == null)
				{
					ComboFont.SelectedIndex = 0;
				}
				else
				{
					ComboFont.SelectedItem = customFontProperties2;
				}
				_isInitializing = false;
				return;
			}
			goto IL_0054;
			IL_0054:
			_pendingFontTag = value;
		}
	}

	public int SelectedIndex
	{
		get
		{
			return ComboFont.SelectedIndex;
		}
		set
		{
			ComboFont.SelectedIndex = value;
		}
	}

	public new bool IsEnabled
	{
		get
		{
			return ComboFont.IsEnabled;
		}
		set
		{
			ComboFont.IsEnabled = value;
		}
	}

	public event SelectionChangedEventHandler? SelectionChanged;

	public FontSelector()
	{
		InitializeComponent();
		Loaded += FontSelector_Loaded;
		Unloaded += FontSelector_Unloaded;
		ComboFont.SelectionChanged += ComboFont_SelectionChanged;
	}

	private static void OnTooltipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is FontSelector fontSelector)
		{
			fontSelector.ComboFont.ToolTip = e.NewValue;
		}
	}

	private void FontSelector_Loaded(object sender, RoutedEventArgs e)
	{
		StartListeningLanguageChanged();
		if (CustomFontCollection.Count == 0)
		{
			LoadFonts();
		}
		else
		{
			RefreshDefaultFont();
		}
	}

	private void FontSelector_Unloaded(object sender, RoutedEventArgs e)
	{
		StopListeningLanguageChanged();
	}

	private void StartListeningLanguageChanged()
	{
		if (!_isListeningLanguageChanged)
		{
			LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
			_isListeningLanguageChanged = true;
		}
	}

	private void StopListeningLanguageChanged()
	{
		if (_isListeningLanguageChanged)
		{
			LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
			_isListeningLanguageChanged = false;
		}
	}

	private void LocalizationService_LanguageChanged()
	{
		RefreshDefaultFont();
	}

	private void RefreshDefaultFont()
	{
		if (!((DispatcherObject)this).Dispatcher.CheckAccess())
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)RefreshDefaultFont);
			return;
		}
		CustomFontProperties? defaultFont = GetDefaultFont();
		if (defaultFont != null)
		{
			defaultFont.Font = LocalizationFontService.BuildLaunchFontFamily((string)null, (LocalizationLanguage)null);
		}
	}

	private void LoadFonts()
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			ComboFont.IsEnabled = false;
			_isInitializing = true;
			CustomFontCollection.Add(new CustomFontProperties
			{
				Name = Lang.Text("Common.State.Loading")
			});
			ComboFont.SelectedIndex = 0;
			List<(string Name, FontFamily Font)> availableFonts = new List<(string, FontFamily)>();
			await Task.Run(() =>
			{
				foreach (FontFamily systemFontFamily in Fonts.SystemFontFamilies)
				{
					try
					{
						if (!systemFontFamily.Source.StartsWith("Global "))
						{
							foreach (Typeface typeface in systemFontFamily.GetTypefaces())
							{
								if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
								{
									throw new NullReferenceException("字形 " + LanguageSpecificStringDictionaryExtensions.GetForCurrentUiCulture(typeface.FaceNames, "(unknown)") + " 无法加载");
								}
								new GlyphTypeface(glyphTypeface.FontUri);
							}
							availableFonts.Add((LanguageSpecificStringDictionaryExtensions.GetForCurrentUiCulture(systemFontFamily.FamilyNames, (string)null), systemFontFamily));
						}
					}
					catch (Exception ex)
					{
						LogWrapper.Error(ex, "发现了一个无法加载的异常的字体：" + systemFontFamily.Source);
					}
				}
				availableFonts.Sort(((string Name, FontFamily Font) l, (string Name, FontFamily Font) r) => string.Compare(l.Name, r.Name, StringComparison.Ordinal));
			});
			CustomFontCollection.Clear();
			CustomFontCollection.Add(new CustomFontProperties
			{
				Name = Lang.Text("Common.Option.Default"),
				Font = LocalizationFontService.BuildLaunchFontFamily((string)null, (LocalizationLanguage)null),
				Tag = ""
			});
			foreach (var item in availableFonts)
			{
				CustomFontCollection.Add(new CustomFontProperties
				{
					Name = item.Name,
					Font = item.Font,
					Tag = item.Font.Source
				});
			}
			ComboFont.IsEnabled = true;
			if (_pendingFontTag != null)
			{
				string pendingFontTag = _pendingFontTag;
				_pendingFontTag = null;
				SelectedFontTag = pendingFontTag;
			}
			_isInitializing = false;
		}), Array.Empty<object>());
	}

	private void ComboFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isInitializing)
		{
			SelectionChanged?.Invoke(sender, e);
		}
	}

	private CustomFontProperties? GetDefaultFont()
	{
		return CustomFontCollection.FirstOrDefault((CustomFontProperties i) => string.IsNullOrEmpty(i.Tag));
	}


	static FontSelector()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected Obj, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected Obj, but got Unknown
	}
}

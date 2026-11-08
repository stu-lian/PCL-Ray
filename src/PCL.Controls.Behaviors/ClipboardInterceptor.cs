using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;

namespace PCL.Controls.Behaviors;

public sealed class ClipboardInterceptor
{
	public static readonly DependencyProperty EnableSafeClipboardProperty = DependencyProperty.RegisterAttached("EnableSafeClipboard", typeof(bool), typeof(ClipboardInterceptor), new PropertyMetadata((object)false, (PropertyChangedCallback)OnEnableSafeClipboardChanged));

	private ClipboardInterceptor()
	{
	}

	public static void SetEnableSafeClipboard(DependencyObject element, bool value)
	{
		element.SetValue(EnableSafeClipboardProperty, (object)value);
	}

	public static bool GetEnableSafeClipboard(DependencyObject element)
	{
		return (bool)element.GetValue(EnableSafeClipboardProperty);
	}

	private static void OnEnableSafeClipboardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (!(bool)e.NewValue)
		{
			return;
		}
		if (!(d is System.Windows.Controls.TextBox tb))
		{
			if (!(d is System.Windows.Controls.RichTextBox rtb))
			{
				if (d is System.Windows.Controls.DataGrid dg)
				{
					AddCommandBindingsToDataGrid(dg);
				}
			}
			else
			{
				AddCommandBindingsToRichTextBox(rtb);
			}
		}
		else
		{
			AddCommandBindingsToTextBox(tb);
		}
	}

	private static void AddCommandBindingsToTextBox(System.Windows.Controls.TextBox tb)
	{
		tb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyTextBox));
		tb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCutTextBox));
		tb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteTextBox));
	}

	private static void AddCommandBindingsToRichTextBox(System.Windows.Controls.RichTextBox rtb)
	{
		rtb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyRichTextBox));
		rtb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCutRichTextBox));
		rtb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteRichTextBox));
	}

	private static void AddCommandBindingsToDataGrid(System.Windows.Controls.DataGrid dg)
	{
		dg.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyDataGrid));
	}

	private static void OnCopyTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.TextBox { SelectionLength: >0 } textBox)
		{
			TrySetClipboardText(textBox.SelectedText);
			e.Handled = true;
		}
	}

	private static void OnCutTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.TextBox { SelectionLength: >0 } textBox)
		{
			TrySetClipboardText(textBox.SelectedText);
			textBox.SelectedText = string.Empty;
			e.Handled = true;
		}
	}

	private static void OnPasteTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (!(sender is System.Windows.Controls.TextBox textBox))
		{
			return;
		}
		if (System.Windows.Forms.Clipboard.ContainsText())
		{
			string text = System.Windows.Forms.Clipboard.GetText();
			if (!textBox.AcceptsReturn)
			{
				text = text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
			}
			int selectionStart = textBox.SelectionStart;
			textBox.SelectedText = text;
			textBox.CaretIndex = selectionStart + text.Length;
			textBox.SelectionLength = 0;
		}
		e.Handled = true;
	}

	private static void OnCopyRichTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.RichTextBox richTextBox)
		{
			TextRange textRange = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End);
			if (!string.IsNullOrEmpty(textRange.Text))
			{
				TrySetClipboardText(textRange.Text);
				e.Handled = true;
			}
		}
	}

	private static void OnCutRichTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.RichTextBox richTextBox)
		{
			TextRange textRange = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End);
			if (!string.IsNullOrEmpty(textRange.Text))
			{
				TrySetClipboardText(textRange.Text);
				textRange.Text = string.Empty;
				e.Handled = true;
			}
		}
	}

	private static void OnPasteRichTextBox(object sender, ExecutedRoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.RichTextBox richTextBox && System.Windows.Forms.Clipboard.ContainsText())
		{
			string text = System.Windows.Forms.Clipboard.GetText();
			TextSelection selection = richTextBox.Selection;
			selection.Text = text;
			TextPointer textPointer = (richTextBox.CaretPosition = selection.End);
			richTextBox.Selection.Select(textPointer, textPointer);
			e.Handled = true;
		}
	}

	private static void OnCopyDataGrid(object sender, ExecutedRoutedEventArgs e)
	{
		if (!(sender is System.Windows.Controls.DataGrid { SelectedCells: not null } dataGrid) || dataGrid.SelectedCells.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (IGrouping<object, DataGridCellInfo> item in from c in dataGrid.SelectedCells
			group c by c.Item)
		{
			string value = string.Join("\t", item.Select((DataGridCellInfo cell) => (!(cell.Column.GetCellContent(cell.Item) is TextBlock textBlock)) ? "" : textBlock.Text));
			stringBuilder.AppendLine(value);
		}
		TrySetClipboardText(stringBuilder.ToString().TrimEnd(new char[2] { '\r', '\n' }));
		e.Handled = true;
	}

	private static bool TrySetClipboardText(string text)
	{
		try
		{
			System.Windows.Forms.Clipboard.Clear();
			System.Windows.Forms.Clipboard.SetDataObject(text, copy: true);
			return true;
		}
		catch
		{
			return false;
		}
	}

	static ClipboardInterceptor()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected Obj, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected Obj, but got Unknown
	}
}

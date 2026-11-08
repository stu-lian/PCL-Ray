using System.IO;
using System.Linq;
using FluentValidation;

namespace PCL.Core.Utils.Validate;

public abstract class FileSystemValidator : AbstractValidator<string>
{
	protected static string? CheckInvalidStrings(string input, string[] extraInvalidStrings)
	{
		if (string.IsNullOrEmpty(input))
		{
			return null;
		}
		string[] array = (from c in Path.GetInvalidFileNameChars()
			select c.ToString()).Concat(extraInvalidStrings).Where(input.Contains).Distinct()
			.ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		return string.Join("  ", array);
	}

	protected static string? CheckReservedWord(string input, string[] extraReservedWords)
	{
		if (string.IsNullOrEmpty(input))
		{
			return null;
		}
		string nameWithoutExtension = Path.GetFileNameWithoutExtension(input);
		return new _003C_003Ez__ReadOnlyArray<string>(new string[31]
		{
			"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
			"COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³", "LPT0", "LPT1",
			"LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²",
			"LPT³"
		}).Concat(extraReservedWords).FirstOrDefault((string r) => r.Equals(nameWithoutExtension)) ?? null;
	}
}

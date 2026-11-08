using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class FolderNameValidator(string? parentFolder = null, bool useMinecraftCharCheck = true, bool ignoreCase = true, bool ignoreSameNameInParentFolder = false) : FileSystemValidator
{
	public bool UseMinecraftCharCheck { get; set; } = useMinecraftCharCheck;

	public bool IgnoreCase { get; set; } = ignoreCase;

	public bool IgnoreSameNameInParentFolder { get; set; } = ignoreSameNameInParentFolder;

	public string? ParentFolder { get; set; } = parentFolder;

	public FolderNameValidator()
		: this(null, true, true, false)
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorExtensions.Custom<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorExtensions.Custom<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)((AbstractValidator<string>)(object)this).RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => !string.IsNullOrWhiteSpace(x))), "输入内容不能为空！"), (Func<string, bool>)((string x) => !x.StartsWith(' '))), "文件名不能以空格开头！"), (Func<string, bool>)((string x) => !x.EndsWith(' '))), "文件名不能以空格结尾！"), (Func<string, bool>)((string x) => !x.EndsWith('.'))), "文件名不能以小数点结尾！"), (Action<string, ValidationContext<string>>)((string fileName, ValidationContext<string> context) =>
		{
			string text = FileSystemValidator.CheckInvalidStrings(fileName, (!UseMinecraftCharCheck) ? Array.Empty<string>() : new string[1] { "!;" });
			if (text != null)
			{
				context.AddFailure("文件名不可包含 " + text + " 字符！");
			}
		})), (Action<string, ValidationContext<string>>)((string fileName, ValidationContext<string> context) =>
		{
			string text = FileSystemValidator.CheckReservedWord(fileName, Array.Empty<string>());
			if (text != null)
			{
				context.AddFailure("文件名不可为 " + text + "！");
			}
		})), (Func<string, bool>)((string x) => !x.IsMatch(RegexPatterns.Ntfs83FileName))), "文件名不能包含这一特殊格式！"), (Func<string, bool>)((string x) =>
		{
			if (ParentFolder == null)
			{
				return true;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(ParentFolder);
			if (!directoryInfo.Exists)
			{
				return true;
			}
			return IgnoreSameNameInParentFolder || !(from f in directoryInfo.EnumerateDirectories()
				select f.Name).Contains(x, IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		})), "不可与现有文件夹重名！");
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

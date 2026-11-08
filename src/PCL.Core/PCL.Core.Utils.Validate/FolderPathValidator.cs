using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class FolderPathValidator(bool useMinecraftCharCheck) : FileSystemValidator
{
	public bool UseMinecraftCharCheck { get; set; } = useMinecraftCharCheck;

	public FolderPathValidator()
		: this(useMinecraftCharCheck: true)
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.NotEmpty<string, string>((IRuleBuilder<string, string>)(object)((AbstractValidator<string>)(object)this).RuleFor<string>((Expression<Func<string, string>>)((string x) => x))), "输入内容不能为空！"), (Func<string, bool>)((string x) => !x.EndsWith(' '))), "文件夹名不能以空格结尾！"), (Func<string, bool>)((string x) => !x.EndsWith('.'))), "文件夹名不能以小数点结尾！");
		DefaultValidatorOptions.OverridePropertyName<string, string>(DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorExtensions.Custom<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorExtensions.Custom<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)((AbstractValidator<string>)(object)this).RuleForEach<string>((Expression<Func<string, IEnumerable<string>>>)((string x) => _GetSubPaths(x))), (Func<string, bool>)((string x) => !string.IsNullOrWhiteSpace(x))), "文件夹路径存在错误！"), (Func<string, bool>)((string x) => !x.StartsWith(' '))), "文件夹名不能以空格开头！"), (Func<string, bool>)((string x) => !x.EndsWith(' '))), "文件夹名不能以空格结尾！"), (Func<string, bool>)((string x) => !x.EndsWith('.'))), "文件夹名不能以小数点结尾！"), (Action<string, ValidationContext<string>>)((string fileName, ValidationContext<string> context) =>
		{
			string text = FileSystemValidator.CheckInvalidStrings(fileName, (!UseMinecraftCharCheck) ? Array.Empty<string>() : new string[1] { "!;" });
			if (text != null)
			{
				context.AddFailure("文件夹名不可包含 " + text + " 字符！");
			}
		})), (Action<string, ValidationContext<string>>)((string fileName, ValidationContext<string> context) =>
		{
			string text = FileSystemValidator.CheckReservedWord(fileName, Array.Empty<string>());
			if (text != null)
			{
				context.AddFailure("文件夹名不可为 " + text + "！");
			}
		})), (Func<string, bool>)((string x) => !x.IsMatch(RegexPatterns.Ntfs83FileName))), "文件夹名不能包含这一特殊格式！"), "PathSegments");
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}

	private static string[] _GetSubPaths(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return Array.Empty<string>();
		}
		string fullName = new DirectoryInfo(path).FullName;
		return fullName.Substring(Path.GetPathRoot(fullName).Length).TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
	}
}

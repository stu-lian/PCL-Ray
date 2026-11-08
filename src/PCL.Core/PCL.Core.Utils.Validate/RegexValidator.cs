using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.App.Localization;

namespace PCL.Core.Utils.Validate;

public class RegexValidator(string pattern = "", string errorMessage = "正则检查失败！") : AbstractValidator<string>
{
	public string Pattern { get; set; } = pattern;

	public string ErrorMessage { get; set; } = errorMessage;

	public string ErrorKey { get; set; } = "";

	public RegexValidator()
		: this(string.Empty)
	{
	}

	private void _BuildRules()
	{
		string text = (string.IsNullOrEmpty(ErrorKey) ? ErrorMessage : Lang.Text(ErrorKey));
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => Regex.IsMatch(x, Pattern))), text);
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

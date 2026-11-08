using System;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class HttpValidator(bool allowNullOrEmpty) : AbstractValidator<string>
{
	public bool AllowsNullOrEmpty { get; set; } = allowNullOrEmpty;

	public HttpValidator()
		: this(allowNullOrEmpty: false)
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => (AllowsNullOrEmpty && string.IsNullOrEmpty(x)) || x.IsMatch(RegexPatterns.HttpUri))), "输入的网址无效！");
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

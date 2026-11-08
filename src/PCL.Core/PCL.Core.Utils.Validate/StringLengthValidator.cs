using System;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;

namespace PCL.Core.Utils.Validate;

public class StringLengthValidator(int min = 0, int max = int.MaxValue) : AbstractValidator<string>
{
	public int Min { get; set; } = min;

	public int Max { get; set; } = max;

	public StringLengthValidator()
		: this(0, int.MaxValue)
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => x.Length != Max || Max == Min)), $"长度必须为 {Max} 个字符！"), (Func<string, bool>)((string x) => x.Length >= Min)), $"长度至少为 {Min} 个字符！"), (Func<string, bool>)((string x) => x.Length <= Max)), $"长度最长为 {Max} 个字符！");
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

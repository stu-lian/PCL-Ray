using System;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;

namespace PCL.Core.Utils.Validate;

public class IntValidator(int max = int.MaxValue, int min = int.MinValue) : AbstractValidator<string>
{
	public int Max { get; set; } = max;

	public int Min { get; set; } = min;

	public IntValidator()
		: this(int.MaxValue, int.MinValue)
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => x.Length < 9)), "请输入一个大小合理的数字！"), (Func<string, bool>)((string x) => int.TryParse(x, out var _))), "请输入一个整数！"), (Func<string, bool>)((string x) => int.TryParse(x, out var result) && result <= Max)), $"不可超过 {Max}！"), (Func<string, bool>)((string x) => int.TryParse(x, out var result) && result >= Min)), $"不可低于 {Min}！");
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

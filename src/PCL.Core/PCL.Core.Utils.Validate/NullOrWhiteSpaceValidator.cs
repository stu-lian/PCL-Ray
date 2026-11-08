using System;
using System.Linq.Expressions;
using FluentValidation;

namespace PCL.Core.Utils.Validate;

public class NullOrWhiteSpaceValidator : AbstractValidator<string>
{
	public NullOrWhiteSpaceValidator()
	{
		DefaultValidatorOptions.WithMessage<string, string>(DefaultValidatorExtensions.Must<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Func<string, bool>)((string x) => !string.IsNullOrWhiteSpace(x))), "输入内容不能为空！");
	}
}

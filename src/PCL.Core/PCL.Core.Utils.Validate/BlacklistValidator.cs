using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;

namespace PCL.Core.Utils.Validate;

public class BlacklistValidator(List<string> contains) : AbstractValidator<string>
{
	public List<string> Blacklist { get; set; } = contains;

	public BlacklistValidator()
		: this(new List<string>())
	{
	}

	private void _BuildRules()
	{
		DefaultValidatorExtensions.Custom<string, string>((IRuleBuilder<string, string>)(object)base.RuleFor<string>((Expression<Func<string, string>>)((string x) => x)), (Action<string, ValidationContext<string>>)((string input, ValidationContext<string> context) =>
		{
			foreach (string item in Blacklist)
			{
				if (input.Contains(item))
				{
					context.AddFailure("输入内容不能包含 " + item + "！");
				}
			}
		}));
	}

	protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
	{
		_BuildRules();
		return base.PreValidate(context, result);
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using PCL.Core.App.Essentials;

namespace PCL.Core.App.IoC;

public static class DependencyGroups
{
	private static readonly AttributeTargets[] _TargetsToTry = new AttributeTargets[3]
	{
		AttributeTargets.Method,
		AttributeTargets.Property,
		AttributeTargets.Class
	};

	private static readonly Dictionary<string, Dictionary<AttributeTargets, DependencyGroup>> _GroupMap;

	public static bool InvokeInjection(Delegate injection, string identifier, AttributeTargets targets)
	{
		if (!_GroupMap.TryGetValue(identifier, out Dictionary<AttributeTargets, DependencyGroup> value))
		{
			return false;
		}
		List<DependencyGroup> list = new List<DependencyGroup>();
		foreach (AttributeTargets item in _TargetsToTry.Where((AttributeTargets t) => targets.HasFlag(t)))
		{
			DependencyGroup valueOrDefault = value.GetValueOrDefault(item);
			if (valueOrDefault == null)
			{
				return false;
			}
			list.Add(valueOrDefault);
		}
		foreach (DependencyGroup item2 in list)
		{
			item2.InvokeInjection(injection);
		}
		return true;
	}

	static DependencyGroups()
	{
		Dictionary<string, Dictionary<AttributeTargets, DependencyGroup>> dictionary = new Dictionary<string, Dictionary<AttributeTargets, DependencyGroup>>();
		Dictionary<AttributeTargets, DependencyGroup> dictionary2 = new Dictionary<AttributeTargets, DependencyGroup>();
		DependencyGroup<PromoteOperationFunction, string> dependencyGroup = new DependencyGroup<PromoteOperationFunction, string>();
		InlineArray2<(PromoteOperationFunction, string)> buffer = default;
		buffer[0] = (PromoteService.KillProcess, "kill");
		buffer[1] = (PromoteService.StartProcess, "start");
		dependencyGroup.Items = ImmutableList.Create<(PromoteOperationFunction, string)>(buffer);
		dictionary2[AttributeTargets.Method] = dependencyGroup;
		dictionary["promote"] = dictionary2;
		dictionary["rpc-function"] = new Dictionary<AttributeTargets, DependencyGroup> { [AttributeTargets.Method] = new DependencyGroup<RpcFunction, string>
		{
			Items = ImmutableList.Create(new ReadOnlySpan<(RpcFunction, string)>(((RpcFunction)StartupService.OnRpcCommand, "cli")))
		} };
		_GroupMap = dictionary;
	}
}

using System;

namespace PCL.Core.App.Essentials;

public class RpcException(string reason) : Exception
{
	public string Reason => reason;
}

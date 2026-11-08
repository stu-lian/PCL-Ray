using System;

namespace PCL.Core.Minecraft.IdentityModel;

public class IdentityModelException : Exception
{
	public IdentityModelException(string message, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
